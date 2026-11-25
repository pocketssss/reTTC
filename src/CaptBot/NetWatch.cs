using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CaptBot;

internal static class NetWatch
{
	private class ZoneRec
	{
		public long Owner;

		public long SinceMs;

		public NetCapt Cap;

		public bool CapEnded;

		public DateTime CapSeen;
	}

	private class Frag
	{
		public byte[] Buf;

		public HashSet<uint> Got = new HashSet<uint>();

		public uint Count;

		public DateTime At;
	}

	public static class V8
	{
		private class Reader
		{
			private readonly byte[] b;

			private int p;

			private readonly List<object> objs = new List<object>();

			private int depth;

			public Reader(byte[] b, int p)
			{
				this.b = b;
				this.p = p;
			}

			private byte Byte()
			{
				if (p >= b.Length)
				{
					throw new EndOfStreamException();
				}
				return b[p++];
			}

			private byte Peek()
			{
				if (p >= b.Length)
				{
					throw new EndOfStreamException();
				}
				return b[p];
			}

			private long Varint()
			{
				long num = 0L;
				int num2 = 0;
				do
				{
					byte b = Byte();
					num |= (long)(b & 0x7F) << num2;
					num2 += 7;
					if ((b & 0x80) == 0)
					{
						return num;
					}
				}
				while (num2 <= 63);
				throw new InvalidDataException();
			}

			private long Zigzag()
			{
				long num = Varint();
				return (num >>> 1) ^ -(num & 1);
			}

			private byte[] Take(long n)
			{
				if (n < 0 || p + n > b.Length)
				{
					throw new EndOfStreamException();
				}
				byte[] array = new byte[n];
				Buffer.BlockCopy(b, p, array, 0, (int)n);
				p += (int)n;
				return array;
			}

			public object Value()
			{
				if (++depth > 64)
				{
					throw new InvalidDataException("слишком глубоко");
				}
				try
				{
					return ValueInner();
				}
				finally
				{
					depth--;
				}
			}

			private object ValueInner()
			{
				char c = (char)Byte();
				while (true)
				{
					switch (c)
					{
					case '\0':
						break;
					case 'ÿ':
						Varint();
						return Value();
					default:
						switch (c)
						{
						case '0':
						case '_':
							return null;
						case 'T':
							return true;
						case 'F':
							return false;
						case 'I':
							return Zigzag();
						case 'U':
							return Varint();
						case 'D':
						case 'N':
							return BitConverter.ToDouble(Take(8L), 0);
						case '"':
							return Encoding.GetEncoding(28591).GetString(Take(Varint()));
						case 'c':
							return Encoding.Unicode.GetString(Take(Varint()));
						case 'S':
							return Encoding.UTF8.GetString(Take(Varint()));
						case '-':
							return Hole;
						case '^':
						{
							long num2 = Varint();
							if (num2 >= objs.Count)
							{
								return null;
							}
							return objs[(int)num2];
						}
						case 'o':
						{
							Dictionary<object, object> dictionary = new Dictionary<object, object>();
							objs.Add(dictionary);
							while (Peek() != 123)
							{
								object obj = Value();
								object value = Value();
								if (obj != null)
								{
									dictionary[obj] = value;
								}
							}
							p++;
							Varint();
							return dictionary;
						}
						case 'A':
						{
							long num3 = Varint();
							if (num3 > 100000)
							{
								throw new InvalidDataException();
							}
							List<object> list3 = new List<object>((int)num3);
							objs.Add(list3);
							for (long num4 = 0L; num4 < num3; num4++)
							{
								list3.Add(Value());
							}
							while (Peek() != 36)
							{
								Value();
								Value();
							}
							p++;
							Varint();
							Varint();
							return list3;
						}
						case 'a':
						{
							long num = Varint();
							if (num > 100000)
							{
								throw new InvalidDataException();
							}
							List<object> list2 = new List<object>(new object[num]);
							objs.Add(list2);
							while (Peek() != 64)
							{
								object obj3 = Value();
								object value3 = Value();
								if (obj3 is long && (long)obj3 >= 0 && (long)obj3 < num)
								{
									list2[(int)(long)obj3] = value3;
								}
							}
							p++;
							Varint();
							Varint();
							return list2;
						}
						case ';':
						{
							Dictionary<object, object> dictionary2 = new Dictionary<object, object>();
							objs.Add(dictionary2);
							while (Peek() != 58)
							{
								object obj2 = Value();
								object value2 = Value();
								if (obj2 != null)
								{
									dictionary2[obj2] = value2;
								}
							}
							p++;
							Varint();
							return dictionary2;
						}
						case '\'':
						{
							List<object> list = new List<object>();
							objs.Add(list);
							while (Peek() != 44)
							{
								list.Add(Value());
							}
							p++;
							Varint();
							return list;
						}
						case 'y':
							return true;
						case 'x':
							return false;
						case 's':
							return Value();
						default:
							throw new InvalidDataException("неизвестный тег " + (int)c);
						}
					}
					c = (char)Byte();
				}
			}
		}

		public static readonly object Hole = new object();

		private static readonly byte[] Marker = Encoding.ASCII.GetBytes("JSVal");

		public static List<object> All(byte[] b)
		{
			List<object> list = new List<object>();
			for (int num = IndexOf(b, Marker, 0); num >= 0; num = IndexOf(b, Marker, num + 6))
			{
				if (num + 5 < b.Length && b[num + 5] == byte.MaxValue)
				{
					try
					{
						list.Add(new Reader(b, num + 5).Value());
					}
					catch
					{
					}
				}
			}
			return list;
		}
	}

	public const int ServerPort = 7778;

	private const string EvCaptureData = "ipad.organization.captureMap.getCaptureDataResult";

	private const string EvSync = "client.captureMap.syncZones";

	private const string EvReset = "client.captureMap.syncZonesChunksReset";

	private const string EvFamilies = "ipad.organization.captureMap.updateFamilyCache";

	private const string EvData = "ipad.organization.captureMap.getDataResult";

	private static readonly List<KeyValuePair<DateTime, NetCapt>> appeared = new List<KeyValuePair<DateTime, NetCapt>>();

	private static readonly List<NetOwner> ownersOut = new List<NetOwner>();

	private static bool ownersFull;

	private static NetLimits limits;

	private static bool limitsDirty;

	private static readonly object Sync = new object();

	private static readonly Dictionary<int, ZoneRec> zones = new Dictionary<int, ZoneRec>();

	private static readonly Dictionary<long, string> families = new Dictionary<long, string>();

	private static readonly List<NetCapt> outbox = new List<NetCapt>();

	private static readonly List<NetCapt> ended = new List<NetCapt>();

	public static long Packets;

	public static long GamePackets;

	public static long Messages;

	public static long RawGamePackets;

	public static string GameServer;

	public static string OwnFamily;

	public static string Error;

	public static Func<DateTime> Now = () => DateTime.UtcNow;

	private static readonly List<Socket> socks = new List<Socket>();

	private static readonly HashSet<string> boundAddrs = new HashSet<string>();

	private static Thread scanThread;

	private static volatile bool stop;

	private static HashSet<int> gamePorts = new HashSet<int>();

	private static long rawSeen = -1L;

	private static DateTime rawQuietSince = DateTime.UtcNow;

	private static DateTime pktFailAt = DateTime.MinValue;

	public static long OutPackets;

	private static readonly HashSet<string> enetPeers = new HashSet<string>();

	private static readonly DateTime[] sentAt = new DateTime[65536];

	private static readonly object rttLock = new object();

	private static readonly List<KeyValuePair<DateTime, double>> rtts = new List<KeyValuePair<DateTime, double>>();

	private static DateTime clickAt = DateTime.MinValue;

	private static string clickWhat;

	private static readonly List<double> wireDelays = new List<double>();

	private static string storePath;

	private static DateTime squareAt = DateTime.MinValue;

	private static string squareWhat;

	private static int squareLogged;

	private static readonly int[] Fixed = new int[13]
	{
		0, 8, 48, 44, 8, 4, 6, 8, 24, 8,
		12, 16, 24
	};

	private static readonly Dictionary<string, Frag> frags = new Dictionary<string, Frag>();

	private static readonly byte[] Logos = Encoding.ASCII.GetBytes("/familyLogos/");

	private static readonly Regex LogoRx = new Regex("/familyLogos/([A-Za-z0-9]{2,8})/");

	private static bool inChunks;

	public static NetLimits Limits
	{
		get
		{
			lock (Sync)
			{
				return limits;
			}
		}
	}

	public static int ZonesKnown
	{
		get
		{
			lock (Sync)
			{
				return zones.Count;
			}
		}
	}

	public static int FamiliesKnown
	{
		get
		{
			lock (Sync)
			{
				return families.Count;
			}
		}
	}

	public static DateTime LastFullUtc { get; private set; }

	public static DateTime LastUpdateUtc { get; private set; }

	public static DateTime LastEventUtc { get; private set; }

	public static DateTime LastOutEventUtc { get; private set; }

	public static DateTime LastCaptureDataUtc { get; private set; }

	public static int LastRequestZone { get; private set; }

	public static DateTime LastRequestUtc { get; private set; }

	public static bool Running { get; private set; }

	public static bool IsAdmin
	{
		get
		{
			try
			{
				return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
			}
			catch
			{
				return false;
			}
		}
	}

	public static bool GameRunning { get; private set; }

	public static double WireDelayMs
	{
		get
		{
			lock (rttLock)
			{
				if (wireDelays.Count == 0)
				{
					return 0.0;
				}
				List<double> list = wireDelays.OrderBy((double x) => x).ToList();
				return (list.Count % 2 == 1) ? list[list.Count / 2] : ((list[list.Count / 2 - 1] + list[list.Count / 2]) / 2.0);
			}
		}
	}

	public static int WireDelayCount
	{
		get
		{
			lock (rttLock)
			{
				return wireDelays.Count;
			}
		}
	}

	public static string StorePath
	{
		get
		{
			return storePath;
		}
		set
		{
			storePath = value;
			try
			{
				if (string.IsNullOrEmpty(value) || !File.Exists(value))
				{
					return;
				}
				lock (rttLock)
				{
					wireDelays.Clear();
					string[] array = File.ReadAllText(value).Split(new char[5] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string s in array)
					{
						if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && result >= 2.0 && result <= 600.0)
						{
							wireDelays.Add(result);
						}
					}
					while (wireDelays.Count > 15)
					{
						wireDelays.RemoveAt(0);
					}
				}
			}
			catch
			{
			}
		}
	}

	public static event Action<string> Note;

	public static event Action<string> OwnFamilyChanged;

	public static KeyValuePair<DateTime, NetCapt>? CaptAppeared(int zone, DateTime since)
	{
		lock (Sync)
		{
			using IEnumerator<KeyValuePair<DateTime, NetCapt>> enumerator = (from a in appeared
				where a.Value.Zone == zone && a.Key >= since
				orderby a.Key
				select a).GetEnumerator();
			if (enumerator.MoveNext())
			{
				KeyValuePair<DateTime, NetCapt> current = enumerator.Current;
				return new KeyValuePair<DateTime, NetCapt>(current.Key, current.Value.Copy());
			}
		}
		return null;
	}

	public static NetCapt CaptOn(int zone)
	{
		lock (Sync)
		{
			ZoneRec value;
			return (zones.TryGetValue(zone, out value) && value.Cap != null && !value.CapEnded) ? value.Cap.Copy() : null;
		}
	}

	public static List<NetOwner> TakeOwners(out bool full)
	{
		lock (Sync)
		{
			full = ownersFull;
			ownersFull = false;
			List<NetOwner> result = new List<NetOwner>(ownersOut);
			ownersOut.Clear();
			return result;
		}
	}

	public static void RequeueOwners(List<NetOwner> l, bool full)
	{
		lock (Sync)
		{
			if (full)
			{
				ownersFull = true;
				ownersOut.Clear();
				ownersOut.AddRange(l);
				return;
			}
			foreach (NetOwner o in l)
			{
				if (!ownersOut.Any((NetOwner x) => x.Zone == o.Zone))
				{
					ownersOut.Add(o);
				}
			}
		}
	}

	public static NetLimits TakeLimits()
	{
		lock (Sync)
		{
			if (!limitsDirty)
			{
				return null;
			}
			limitsDirty = false;
			return limits;
		}
	}

	public static void RequeueLimits()
	{
		lock (Sync)
		{
			limitsDirty = limits != null;
		}
	}

	public static NetCapt LastEnded(int zone, long since)
	{
		lock (Sync)
		{
			return (from c in ended
				where c.Zone == zone && c.End >= since
				orderby c.End descending
				select c.Copy()).FirstOrDefault();
		}
	}

	public static List<NetCapt> EndedSince(long since)
	{
		lock (Sync)
		{
			return (from c in ended
				where c.End >= since
				select c.Copy()).ToList();
		}
	}

	public static bool OwnerOf(int zone, out long id, out string name)
	{
		id = 0L;
		name = null;
		lock (Sync)
		{
			if (!zones.TryGetValue(zone, out var value))
			{
				return false;
			}
			id = value.Owner;
			if (id != 0)
			{
				families.TryGetValue(id, out name);
			}
			return true;
		}
	}

	public static int RequestZone(byte[] body)
	{
		if (body.Length < 20 || body.Length > 64 || EventName(body) == null)
		{
			return 0;
		}
		int num = 1;
		int num2 = 0;
		int num3 = 0;
		while (num < body.Length && num3 < 21)
		{
			byte b = body[num++];
			num2 |= (b & 0x7F) << num3;
			num3 += 7;
			if ((b & 0x80) == 0)
			{
				break;
			}
		}
		int num4 = num + num2 / 2;
		byte[] n = new byte[5] { 13, 0, 2, 0, 3 };
		int num5 = IndexOf(body, n, num4);
		if (num5 < 0 || num5 > num4 + 4 || num5 + 9 > body.Length)
		{
			return 0;
		}
		int num6 = BitConverter.ToInt32(body, num5 + 5);
		if (num6 <= 0 || ((uint)num6 & (true ? 1u : 0u)) != 0)
		{
			return 0;
		}
		int num7 = num6 >> 1;
		if (Zones.ById(num7) == null)
		{
			return 0;
		}
		return num7;
	}

	public static List<NetCapt> Active()
	{
		lock (Sync)
		{
			return (from z in zones.Values
				where z.Cap != null && !z.CapEnded
				select z.Cap.Copy() into c
				orderby c.Start
				select c).ToList();
		}
	}

	public static List<NetCapt> TakeOutbox()
	{
		lock (Sync)
		{
			List<NetCapt> result = new List<NetCapt>(outbox);
			outbox.Clear();
			return result;
		}
	}

	public static void Requeue(IEnumerable<NetCapt> l)
	{
		lock (Sync)
		{
			foreach (NetCapt c in l)
			{
				if (!outbox.Any((NetCapt o) => o.Zone == c.Zone && o.Start == c.Start && o.AId == c.AId && (o.End != 0 || c.End == 0)))
				{
					outbox.Add(c);
				}
			}
		}
	}

	public static string FamilyName(long id)
	{
		lock (Sync)
		{
			string value;
			return families.TryGetValue(id, out value) ? value : null;
		}
	}

	public static List<string> FamilyNames()
	{
		lock (Sync)
		{
			return families.Values.Distinct().ToList();
		}
	}

	private static void Say(string s)
	{
		Action<string> note = NetWatch.Note;
		if (note != null)
		{
			try
			{
				note(s);
			}
			catch
			{
			}
		}
	}

	public static bool Start()
	{
		if (Running)
		{
			return true;
		}
		if (!IsAdmin)
		{
			Error = "нужны права администратора";
			return false;
		}
		stop = false;
		Error = null;
		RefreshSockets();
		lock (socks)
		{
			if (socks.Count == 0)
			{
				Error = Error ?? "не удалось открыть ни один сетевой адрес";
				return false;
			}
		}
		Running = true;
		PktMonCapture.OnPacket = FeedTunnel;
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				PktMonCapture.CleanupStale();
			}
			catch
			{
			}
		});
		Thread thread = new Thread(ScanLoop);
		thread.IsBackground = true;
		thread.Name = "NetWatch.scan";
		scanThread = thread;
		scanThread.Start();
		return true;
	}

	public static void Stop()
	{
		stop = true;
		Running = false;
		try
		{
			PktMonCapture.Stop();
		}
		catch
		{
		}
		lock (socks)
		{
			foreach (Socket sock in socks)
			{
				try
				{
					sock.Close();
				}
				catch
				{
				}
			}
			socks.Clear();
			boundAddrs.Clear();
		}
	}

	private static void ScanLoop()
	{
		int num = 0;
		DateTime utcNow = DateTime.UtcNow;
		while (!stop)
		{
			try
			{
				gamePorts = GamePorts();
				GameRunning = gamePorts.Count > 0;
				if (GameRunning)
				{
					utcNow = DateTime.UtcNow;
				}
				else if ((DateTime.UtcNow - utcNow).TotalSeconds > 60.0 && LastUpdateUtc != DateTime.MinValue)
				{
					Reset();
					Say("Из игры: игра закрыта — карта каптов сброшена до следующего входа.");
				}
			}
			catch
			{
			}
			try
			{
				Tunnel();
			}
			catch
			{
			}
			if (++num % 4 == 0)
			{
				try
				{
					RefreshSockets();
				}
				catch
				{
				}
			}
			for (int i = 0; i < 50; i++)
			{
				if (stop)
				{
					break;
				}
				Thread.Sleep(100);
			}
		}
	}

	private static void Tunnel()
	{
		long num = Interlocked.Read(ref RawGamePackets);
		bool flag = num != rawSeen;
		rawSeen = num;
		DateTime utcNow = DateTime.UtcNow;
		if (!GameRunning)
		{
			rawQuietSince = utcNow;
			if (PktMonCapture.Running)
			{
				PktMonCapture.Stop();
				Say("Из игры: игра закрыта — монитор пакетов Windows выключен.");
			}
			return;
		}
		if (flag)
		{
			rawQuietSince = utcNow;
			if (PktMonCapture.Running)
			{
				PktMonCapture.Stop();
				Say("Из игры: пакеты игры снова видны на сетевой карте — монитор пакетов Windows выключен.");
			}
			return;
		}
		List<int> list = gamePorts.Where((int pp) => pp != 5353 && pp != 5355 && pp != 1900 && pp != 137 && pp != 138).ToList();
		if (PktMonCapture.Running)
		{
			if (!PktMonCapture.Start(list))
			{
				pktFailAt = utcNow;
				Say("Из игры: монитор пакетов Windows не перезапустился на новые порты игры: " + PktMonCapture.Error + ".");
			}
		}
		else if (!((utcNow - rawQuietSince).TotalSeconds < 15.0) && !((utcNow - pktFailAt).TotalMinutes < 5.0))
		{
			if (PktMonCapture.Start(list))
			{
				Say("Из игры: пакетов игры на сетевой карте нет — похоже, она идёт через VPN или ускоритель (виртуальный адаптер). Читаю её трафик через монитор пакетов Windows (pktmon, только чтение, порты " + string.Join(", ", list) + ").");
				return;
			}
			pktFailAt = utcNow;
			Say("Из игры: пакетов игры на сетевой карте нет (VPN или ускоритель?), а монитор пакетов Windows не включился: " + PktMonCapture.Error + ".");
		}
	}

	private static void RefreshSockets()
	{
		NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface networkInterface in allNetworkInterfaces)
		{
			if (networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
			{
				continue;
			}
			foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
			{
				if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
				{
					continue;
				}
				string key = unicastAddress.Address.ToString();
				lock (socks)
				{
					if (boundAddrs.Contains(key))
					{
						continue;
					}
				}
				Socket socket = null;
				try
				{
					socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
					socket.Bind(new IPEndPoint(unicastAddress.Address, 0));
					socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AcceptConnection, optionValue: true);
					socket.ReceiveBufferSize = 4194304;
					socket.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes(1), new byte[4]);
				}
				catch (Exception ex)
				{
					if (socket != null)
					{
						try
						{
							socket.Close();
						}
						catch
						{
						}
					}
					Error = "адрес " + key + ": " + ex.Message;
					continue;
				}
				lock (socks)
				{
					socks.Add(socket);
					boundAddrs.Add(key);
				}
				Socket sock = socket;
				string name = networkInterface.Name;
				Thread thread = new Thread((ThreadStart)delegate
				{
					RecvLoop(sock, key);
				});
				thread.IsBackground = true;
				thread.Name = "NetWatch " + name;
				Thread thread2 = thread;
				thread2.Start();
			}
		}
	}

	private static void RecvLoop(Socket s, string addr)
	{
		byte[] array = new byte[65536];
		while (!stop)
		{
			int num;
			try
			{
				num = s.Receive(array);
			}
			catch
			{
				break;
			}
			if (num > 0)
			{
				DateTime at = Now();
				Interlocked.Increment(ref Packets);
				try
				{
					OnIp(array, num, at);
				}
				catch (Exception ex)
				{
					Error = "разбор пакета: " + ex.Message;
				}
			}
		}
		lock (socks)
		{
			socks.Remove(s);
			boundAddrs.Remove(addr);
		}
		try
		{
			s.Close();
		}
		catch
		{
		}
	}

	public static void ReplayIp(byte[] b, int n, DateTime at)
	{
		OnIp(b, n, at);
	}

	public static void FeedTunnel(byte[] b, int n, DateTime at)
	{
		OnIp(b, n, at, tunnel: true);
	}

	private static void OnIp(byte[] b, int n, DateTime at, bool tunnel = false)
	{
		if (n < 28 || b[0] >> 4 != 4 || b[9] != 17)
		{
			return;
		}
		int num = (b[0] & 0xF) * 4;
		if ((b[6] & 0x1Fu) != 0 || b[7] != 0 || n < num + 8)
		{
			return;
		}
		int num2 = (b[num] << 8) | b[num + 1];
		int num3 = (b[num + 2] << 8) | b[num + 3];
		HashSet<int> hashSet = gamePorts;
		bool flag = ((hashSet.Count > 0) ? hashSet.Contains(num3) : (num2 == 7778));
		bool flag2 = !flag && ((hashSet.Count > 0) ? hashSet.Contains(num2) : (num3 == 7778));
		if (!flag && !flag2)
		{
			return;
		}
		int num4 = (b[num + 4] << 8) | b[num + 5];
		int num5 = Math.Min(num4 - 8, n - num - 8);
		if (num5 < 4)
		{
			return;
		}
		byte[] array = new byte[num5];
		Buffer.BlockCopy(b, num + 8, array, 0, num5);
		if (flag)
		{
			string text = b[12] + "." + b[13] + "." + b[14] + "." + b[15] + ":" + num2;
			Interlocked.Increment(ref GamePackets);
			Feed(text, array, at);
			if (!tunnel && IsEnetPeer(text))
			{
				Interlocked.Increment(ref RawGamePackets);
			}
		}
		else
		{
			string p = b[16] + "." + b[17] + "." + b[18] + "." + b[19] + ":" + num3;
			if (IsEnetPeer(p))
			{
				Interlocked.Increment(ref OutPackets);
				FeedOut(array, at);
			}
		}
	}

	private static bool IsEnetPeer(string p)
	{
		lock (enetPeers)
		{
			return enetPeers.Contains(p);
		}
	}

	private static void FeedOut(byte[] pl, DateTime at)
	{
		int num = (pl[0] << 8) | pl[1];
		int i = 2;
		if (((uint)num & 0x8000u) != 0)
		{
			sentAt[(pl[2] << 8) | pl[3]] = at;
			i = 4;
		}
		bool flag = false;
		int num2;
		int num4;
		for (; i + 4 <= pl.Length; i += Fixed[num2] + num4)
		{
			num2 = pl[i] & 0xF;
			if (num2 < 1 || num2 > 12)
			{
				break;
			}
			int num3 = pl[i + 3];
			num4 = 0;
			int num5 = DataLenAt(num2);
			if (num5 >= 0)
			{
				if (i + num5 + 2 > pl.Length)
				{
					break;
				}
				num4 = (pl[i + num5] << 8) | pl[i + num5 + 1];
			}
			if ((num2 != 6 && num2 != 8) || num3 != 7)
			{
				continue;
			}
			flag = true;
			LastOutEventUtc = at;
			if (i + Fixed[num2] < pl.Length)
			{
				int num6 = i + Fixed[num2];
				int val = Math.Min(num4, pl.Length - num6);
				byte[] array = new byte[Math.Max(0, val)];
				Buffer.BlockCopy(pl, num6, array, 0, array.Length);
				int num7 = RequestZone(array);
				if (num7 > 0)
				{
					LastRequestZone = num7;
					LastRequestUtc = at;
				}
			}
		}
		if (flag)
		{
			OnOutgoingEvent(at);
		}
	}

	private static void OnAck(int sentTime, DateTime at)
	{
		DateTime dateTime = sentAt[sentTime];
		if (dateTime == DateTime.MinValue)
		{
			return;
		}
		double totalMilliseconds = (at - dateTime).TotalMilliseconds;
		if (totalMilliseconds <= 0.0 || totalMilliseconds > 2000.0)
		{
			return;
		}
		lock (rttLock)
		{
			rtts.Add(new KeyValuePair<DateTime, double>(at, totalMilliseconds));
			if (rtts.Count > 4000)
			{
				rtts.RemoveRange(0, 2000);
			}
		}
	}

	public static bool RttStats(double windowSec, out double median, out double min, out double max, out int n)
	{
		DateTime from = Now().AddSeconds(0.0 - windowSec);
		List<double> list;
		lock (rttLock)
		{
			list = (from kv in rtts
				where kv.Key >= @from
				select kv.Value).ToList();
		}
		n = list.Count;
		if (n == 0)
		{
			median = (min = (max = 0.0));
			return false;
		}
		list.Sort();
		median = ((n % 2 == 1) ? list[n / 2] : ((list[n / 2 - 1] + list[n / 2]) / 2.0));
		min = list[0];
		max = list[n - 1];
		return true;
	}

	public static void MarkClick(DateTime utc, string what)
	{
		lock (rttLock)
		{
			clickAt = utc;
			clickWhat = what;
			if (what != null && (what.StartsWith("квадрат") || what.StartsWith("приложение")))
			{
				squareAt = utc;
				squareWhat = what;
				squareLogged = 0;
			}
		}
	}

	private static void DebugEvent(string dir, byte[] m, DateTime at)
	{
	}

	private static string Hex(byte[] m, int max)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < m.Length && i < max; i++)
		{
			stringBuilder.Append(m[i].ToString("X2")).Append((i % 4 == 3) ? " " : "");
		}
		if (m.Length > max)
		{
			stringBuilder.Append("…");
		}
		return stringBuilder.ToString().Trim();
	}

	private static string FindNumber(byte[] m, int id)
	{
		List<KeyValuePair<string, byte[]>> list = new List<KeyValuePair<string, byte[]>>();
		list.Add(new KeyValuePair<string, byte[]>("строка", Encoding.ASCII.GetBytes(id.ToString())));
		list.Add(new KeyValuePair<string, byte[]>("int32LE", BitConverter.GetBytes(id)));
		list.Add(new KeyValuePair<string, byte[]>("int32BE", new byte[4]
		{
			(byte)(id >> 24),
			(byte)(id >> 16),
			(byte)(id >> 8),
			(byte)id
		}));
		list.Add(new KeyValuePair<string, byte[]>("int16LE", new byte[2]
		{
			(byte)id,
			(byte)(id >> 8)
		}));
		list.Add(new KeyValuePair<string, byte[]>("int16BE", new byte[2]
		{
			(byte)(id >> 8),
			(byte)id
		}));
		list.Add(new KeyValuePair<string, byte[]>("varint", Varint((ulong)id)));
		list.Add(new KeyValuePair<string, byte[]>("zigzag", Varint((ulong)((long)id << 1))));
		list.Add(new KeyValuePair<string, byte[]>("double", BitConverter.GetBytes((double)id)));
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, byte[]> item in list)
		{
			int num = IndexOf(m, item.Value, 0);
			while (num >= 0 && list2.Count < 12)
			{
				list2.Add(item.Key + "@" + num);
				num = IndexOf(m, item.Value, num + 1);
			}
		}
		return string.Join(", ", list2);
	}

	private static byte[] Varint(ulong v)
	{
		List<byte> list = new List<byte>();
		do
		{
			byte b = (byte)(v & 0x7F);
			v >>= 7;
			if (v != 0)
			{
				b = (byte)(b | 0x80u);
			}
			list.Add(b);
		}
		while (v != 0);
		return list.ToArray();
	}

	private static void Flatten(IEnumerable<object> vals, StringBuilder sb, ref int k, int lim)
	{
		foreach (object val in vals)
		{
			if (k >= lim)
			{
				break;
			}
			if (val is List<object> vals2)
			{
				sb.Append("[");
				Flatten(vals2, sb, ref k, lim);
				sb.Append("] ");
			}
			else if (val is Dictionary<object, object> dictionary)
			{
				sb.Append("{");
				foreach (KeyValuePair<object, object> item in dictionary)
				{
					if (k < lim)
					{
						sb.Append(Convert.ToString(item.Key, CultureInfo.InvariantCulture)).Append(':');
						Flatten(new object[1] { item.Value }, sb, ref k, lim);
						continue;
					}
					break;
				}
				sb.Append("} ");
			}
			else if (val != null && val != V8.Hole)
			{
				string text = Convert.ToString(val, CultureInfo.InvariantCulture);
				if (text.Length > 40)
				{
					text = text.Substring(0, 40) + "…";
				}
				sb.Append(text).Append(' ');
				k++;
			}
		}
	}

	private static void OnOutgoingEvent(DateTime at)
	{
		double totalMilliseconds;
		string text;
		lock (rttLock)
		{
			if (clickAt == DateTime.MinValue)
			{
				return;
			}
			totalMilliseconds = (at - clickAt).TotalMilliseconds;
			text = clickWhat;
			if (totalMilliseconds < 0.0)
			{
				return;
			}
			clickAt = DateTime.MinValue;
			if (totalMilliseconds < 2.0 || totalMilliseconds > 600.0)
			{
				return;
			}
			wireDelays.Add(totalMilliseconds);
			if (wireDelays.Count > 15)
			{
				wireDelays.RemoveAt(0);
			}
			SaveDelays();
		}
		Say($"Из игры: от нажатия «{text}» до ухода запроса {totalMilliseconds:0} мс (задержка игры; учитывается в опережении, медиана {WireDelayMs:0} мс по {WireDelayCount}).");
	}

	private static void SaveDelays()
	{
		if (string.IsNullOrEmpty(storePath))
		{
			return;
		}
		try
		{
			File.WriteAllText(storePath, string.Join(",", wireDelays.Select((double d) => d.ToString("0.0", CultureInfo.InvariantCulture)).ToArray()));
		}
		catch
		{
		}
	}

	[DllImport("iphlpapi.dll", SetLastError = true)]
	private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, uint reserved);

	private static HashSet<int> GamePorts()
	{
		HashSet<int> hashSet = new HashSet<int>();
		Process[] processesByName = Process.GetProcessesByName("GTA5");
		foreach (Process process in processesByName)
		{
			hashSet.Add(process.Id);
			process.Dispose();
		}
		HashSet<int> hashSet2 = new HashSet<int>();
		if (hashSet.Count == 0)
		{
			return hashSet2;
		}
		int pdwSize = 0;
		GetExtendedUdpTable(IntPtr.Zero, ref pdwSize, bOrder: false, 2, 1, 0u);
		IntPtr intPtr = Marshal.AllocHGlobal(pdwSize + 1024);
		try
		{
			pdwSize += 1024;
			if (GetExtendedUdpTable(intPtr, ref pdwSize, bOrder: false, 2, 1, 0u) != 0)
			{
				return hashSet2;
			}
			int num = Marshal.ReadInt32(intPtr);
			for (int j = 0; j < num; j++)
			{
				IntPtr ptr = intPtr + 4 + j * 12;
				int num2 = Marshal.ReadInt32(ptr, 4);
				int item = Marshal.ReadInt32(ptr, 8);
				if (hashSet.Contains(item))
				{
					hashSet2.Add(((num2 & 0xFF) << 8) | ((num2 >> 8) & 0xFF));
				}
			}
			return hashSet2;
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	private static int DataLenAt(int cmd)
	{
		switch (cmd)
		{
		default:
			return -1;
		case 7:
		case 8:
		case 9:
		case 12:
			return 6;
		case 6:
			return 4;
		}
	}

	public static void Feed(string peer, byte[] pl)
	{
		Feed(peer, pl, Now());
	}

	public static void Feed(string peer, byte[] pl, DateTime when)
	{
		if (pl.Length < 4)
		{
			return;
		}
		int num = (pl[0] << 8) | pl[1];
		int i = ((((uint)num & 0x8000u) != 0) ? 4 : 2);
		bool flag = false;
		int num4;
		int num5;
		for (; i + 4 <= pl.Length; i += num4 + num5)
		{
			int num2 = pl[i] & 0xF;
			if (num2 < 1 || num2 > 12)
			{
				break;
			}
			int num3 = pl[i + 3];
			num4 = Fixed[num2];
			num5 = 0;
			int num6 = DataLenAt(num2);
			if (num6 >= 0)
			{
				if (i + num6 + 2 > pl.Length)
				{
					break;
				}
				num5 = (pl[i + num6] << 8) | pl[i + num6 + 1];
			}
			if (i + num4 > pl.Length)
			{
				break;
			}
			if (num2 == 1)
			{
				OnAck((pl[i + 6] << 8) | pl[i + 7], when);
			}
			int num7 = Math.Max(0, Math.Min(num5, pl.Length - i - num4));
			switch (num2)
			{
			case 8:
			case 12:
			{
				if (i + 24 > pl.Length)
				{
					break;
				}
				int num8 = (pl[i + 4] << 8) | pl[i + 5];
				uint num9 = U32(pl, i + 8);
				uint item = U32(pl, i + 12);
				uint num10 = U32(pl, i + 16);
				uint num11 = U32(pl, i + 20);
				if (num10 == 0 || num10 > 4194304 || num9 == 0 || num9 > 4096 || num11 + num7 > num10)
				{
					continue;
				}
				string key = peer + "|" + num3 + "|" + num8 + "|" + num10;
				Frag value;
				lock (frags)
				{
					if (!frags.TryGetValue(key, out value))
					{
						if (frags.Count > 64)
						{
							foreach (string item2 in (from kv in frags
								where (DateTime.UtcNow - kv.Value.At).TotalSeconds > 30.0
								select kv.Key).ToList())
							{
								frags.Remove(item2);
							}
						}
						Frag frag2 = (frags[key] = new Frag
						{
							Buf = new byte[num10],
							Count = num9,
							At = DateTime.UtcNow
						});
						value = frag2;
					}
					Buffer.BlockCopy(pl, i + num4, value.Buf, (int)num11, num7);
					value.Got.Add(item);
					if (value.Got.Count < value.Count)
					{
						value = null;
					}
					else
					{
						frags.Remove(key);
					}
				}
				if (value != null && OnMessage(value.Buf))
				{
					flag = true;
				}
				continue;
			}
			case 6:
			case 7:
			case 9:
			{
				byte[] array = new byte[num7];
				Buffer.BlockCopy(pl, i + num4, array, 0, num7);
				if (OnMessage(array))
				{
					flag = true;
				}
				continue;
			}
			default:
				continue;
			}
			break;
		}
		if (flag)
		{
			lock (enetPeers)
			{
				enetPeers.Add(peer);
			}
		}
	}

	private static uint U32(byte[] b, int i)
	{
		return (uint)((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]);
	}

	public static string EventName(byte[] m)
	{
		if (m.Length < 3 || m[0] != 13)
		{
			return null;
		}
		int num = 1;
		int num2 = 0;
		int num3 = 0;
		while (num < m.Length && num3 < 21)
		{
			byte b = m[num++];
			num2 |= (b & 0x7F) << num3;
			num3 += 7;
			if ((b & 0x80) == 0)
			{
				break;
			}
		}
		num2 /= 2;
		if (num2 < 3 || num2 > 200 || num + num2 > m.Length)
		{
			return null;
		}
		for (int i = num; i < num + num2; i++)
		{
			if (m[i] < 32 || m[i] > 126)
			{
				return null;
			}
		}
		return Encoding.ASCII.GetString(m, num, num2);
	}

	private static bool OnMessage(byte[] m)
	{
		if (EventName(m) == null)
		{
			return false;
		}
		OnEvent(m);
		return true;
	}

	private static void OnEvent(byte[] m)
	{
		string text = EventName(m);
		if (text == null)
		{
			return;
		}
		Interlocked.Increment(ref Messages);
		LastEventUtc = Now();
		if (text == "ipad.organization.captureMap.getCaptureDataResult")
		{
			LastCaptureDataUtc = Now();
		}
		DebugEvent("пришло", m, Now());
		if (GameServer == null && IndexOf(m, Logos, 0) >= 0)
		{
			Match match = LogoRx.Match(Encoding.ASCII.GetString(m));
			if (match.Success)
			{
				GameServer = match.Groups[1].Value.ToUpperInvariant();
			}
		}
		if (text == "client.captureMap.syncZonesChunksReset")
		{
			inChunks = true;
		}
		else
		{
			if (text != "client.captureMap.syncZones" && text != "ipad.organization.captureMap.updateFamilyCache" && text != "ipad.organization.captureMap.getDataResult" && text != "ipad.organization.getInitFamilyDataResult")
			{
				return;
			}
			List<object> list = new List<object>();
			foreach (byte[] item in Inflate(m))
			{
				list.AddRange(V8.All(item));
			}
			list.AddRange(V8.All(m));
			switch (text)
			{
			case "ipad.organization.captureMap.getDataResult":
				OnLimits(list);
				return;
			case "ipad.organization.captureMap.updateFamilyCache":
				OnFamilies(list);
				return;
			case "ipad.organization.getInitFamilyDataResult":
			{
				string text2 = list.OfType<string>().FirstOrDefault((string s) => s.Length > 0 && !s.StartsWith("/"));
				if (text2 == null || !(text2 != OwnFamily))
				{
					return;
				}
				OwnFamily = text2;
				Say("Из игры: своя семья — " + text2 + ".");
				Action<string> ownFamilyChanged = NetWatch.OwnFamilyChanged;
				if (ownFamilyChanged != null)
				{
					try
					{
						ownFamilyChanged(text2);
						return;
					}
					catch
					{
						return;
					}
				}
				return;
			}
			}
			List<object> list2 = null;
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] is List<object> list3 && list3.Any((object x) => x is List<object>))
				{
					list2 = list3;
					if (i + 2 < list.Count && list[i + 2] is bool)
					{
						flag = (bool)list[i + 2];
					}
					break;
				}
			}
			if (list2 == null)
			{
				return;
			}
			bool flag2 = inChunks;
			DateTime dateTime = Now();
			int num = 0;
			lock (Sync)
			{
				foreach (object item2 in list2)
				{
					if (item2 is List<object> { Count: >=7 } list4 && list4[0] is long)
					{
						OnZone(list4, dateTime, flag2);
						num++;
					}
				}
			}
			LastUpdateUtc = dateTime;
			if (!flag2 || !flag)
			{
				return;
			}
			inChunks = false;
			LastFullUtc = dateTime;
			lock (Sync)
			{
				ownersOut.Clear();
				ownersFull = true;
				foreach (KeyValuePair<int, ZoneRec> zone in zones)
				{
					if (zone.Value.Owner != 0)
					{
						ownersOut.Add(MakeOwner(zone.Key, zone.Value));
					}
				}
			}
			Say("Из игры: получена вся карта каптов (" + ZonesKnown + " зон).");
		}
	}

	private static NetOwner MakeOwner(int zone, ZoneRec r)
	{
		families.TryGetValue(r.Owner, out var value);
		NetOwner netOwner = new NetOwner();
		netOwner.Zone = zone;
		netOwner.FamilyId = r.Owner;
		netOwner.Family = value;
		netOwner.Since = r.SinceMs / 1000;
		return netOwner;
	}

	private static void OnLimits(List<object> vals)
	{
		if (vals.Count < 5 || !(vals[1] is long) || !(vals[3] is long))
		{
			return;
		}
		int num = (int)(long)vals[1];
		int num2 = (int)(long)vals[3];
		long num3 = ((vals[2] is string) ? (B36((string)vals[2]) / 1000) : 0);
		long num4 = ((vals[4] is string) ? (B36((string)vals[4]) / 1000) : 0);
		int num5 = -1;
		int num6 = -1;
		foreach (object val in vals)
		{
			if (!(val is List<object> { Count: not 0 } list))
			{
				continue;
			}
			int num7 = 0;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = true;
			foreach (object item in list)
			{
				if (!(item is Dictionary<object, object> dictionary) || !dictionary.TryGetValue("source", out var value) || !dictionary.TryGetValue("amount", out var value2) || !(value2 is long))
				{
					flag3 = false;
					break;
				}
				num7 += (int)(long)value2;
				string text = (value as string) ?? "";
				if (text.IndexOf("Def", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					flag = true;
				}
				if (text.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					flag2 = true;
				}
			}
			if (flag3)
			{
				if (flag && num5 < 0)
				{
					num5 = num7;
				}
				else if (flag2 && num6 < 0)
				{
					num6 = num7;
				}
			}
		}
		if (num5 < 1 || num6 < 1 || num5 > 9 || num6 > 9 || num < 0 || num2 < 0 || num > num5 || num2 > num6)
		{
			Say("Из игры: лимиты семьи не разобраны (формат события не тот) — беру их, как раньше, с экрана.");
			return;
		}
		NetLimits netLimits = new NetLimits();
		netLimits.Family = OwnFamily;
		netLimits.AtkLeft = num2;
		netLimits.AtkMax = num6;
		netLimits.DefLeft = num;
		netLimits.DefMax = num5;
		netLimits.AtkCdUntil = num4;
		netLimits.DefCdUntil = num3;
		netLimits.At = Now();
		NetLimits netLimits2 = netLimits;
		bool flag4;
		lock (Sync)
		{
			flag4 = limits == null || limits.AtkLeft != netLimits2.AtkLeft || limits.AtkMax != netLimits2.AtkMax || limits.DefLeft != netLimits2.DefLeft || limits.DefMax != netLimits2.DefMax || limits.AtkCdUntil != netLimits2.AtkCdUntil || limits.DefCdUntil != netLimits2.DefCdUntil;
			limits = netLimits2;
			if (flag4)
			{
				limitsDirty = true;
			}
		}
		long num8 = Unix(netLimits2.At);
		if (flag4)
		{
			Say(string.Format("Из игры: у семьи{0} атак {1}/{2}, защит {3}/{4}{5}{6}.", string.IsNullOrEmpty(OwnFamily) ? "" : (" " + OwnFamily), num2, num6, num, num5, (num4 > num8) ? (", атаковать можно с " + Hms(num4)) : "", (num3 > num8) ? (", защищаемся снова с " + Hms(num3)) : ""));
		}
	}

	private static void OnFamilies(List<object> vals)
	{
		int num = 0;
		lock (Sync)
		{
			foreach (string item in vals.OfType<string>())
			{
				string[] array = item.Split('&');
				foreach (string text in array)
				{
					string[] array2 = text.Split('^');
					if (array2.Length >= 3 && long.TryParse(array2[0], out var result) && array2[2].Trim().Length > 0)
					{
						if (!families.ContainsKey(result))
						{
							num++;
						}
						families[result] = array2[2].Trim();
					}
				}
			}
		}
		if (num > 0)
		{
			Say("Из игры: названия семей — " + FamiliesKnown + ".");
		}
	}

	private static long Num(object v)
	{
		if (v is long)
		{
			return (long)v;
		}
		if (v is double)
		{
			return (long)(double)v;
		}
		return 0L;
	}

	private static long B36(string s)
	{
		long num = 0L;
		if (string.IsNullOrEmpty(s) || s.Length > 12)
		{
			return 0L;
		}
		string text = s.ToLowerInvariant();
		foreach (char c in text)
		{
			int num2 = ((c >= '0' && c <= '9') ? (c - 48) : ((c >= 'a' && c <= 'z') ? (c - 97 + 10) : (-1)));
			if (num2 < 0)
			{
				return 0L;
			}
			num = num * 36 + num2;
		}
		return num;
	}

	private static long Unix(DateTime utc)
	{
		return (long)Math.Floor((utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
	}

	private static void OnZone(List<object> z, DateTime now, bool snapshot)
	{
		int id = (int)(long)z[0];
		long num = Num(z[1]);
		long num2 = Num(z[6]);
		List<object> list = z[3] as List<object>;
		ZoneRec value;
		bool flag = zones.TryGetValue(id, out value);
		if (!flag)
		{
			ZoneRec zoneRec2 = (zones[id] = new ZoneRec());
			value = zoneRec2;
		}
		long owner = value.Owner;
		long end = Unix(now);
		if (list != null && list.Count >= 3)
		{
			long num3 = ((list[0] is string) ? B36((string)list[0]) : Num(list[0]));
			long num4 = num3 / 1000;
			long num5 = Num(list[1]);
			long num6 = Num(list[2]);
			if (num4 > 0)
			{
				if (value.Cap != null && !value.CapEnded && value.Cap.AId == num5 && value.Cap.DId == num6 && value.Cap.Start != num4)
				{
					long start = value.Cap.Start;
					NetCapt netCapt = value.Cap.Copy();
					netCapt.Start = num4;
					netCapt.PrevStart = start;
					value.Cap = netCapt;
					value.CapSeen = now;
					outbox.Add(netCapt.Copy());
					Say("Из игры: капт на зоне " + id + " перенесён: начало " + Hm(start) + " → " + Hm(num4) + ".");
				}
				else if (value.Cap == null || value.Cap.Start != num4 || value.Cap.AId != num5)
				{
					string text = ((list.Count > 5) ? (list[5] as string) : null);
					if (string.IsNullOrEmpty(text) && families.TryGetValue(num5, out var value2))
					{
						text = value2;
					}
					families.TryGetValue(num6, out var value3);
					value.Cap = new NetCapt
					{
						Zone = id,
						Start = num4,
						AId = num5,
						DId = num6,
						A = (text ?? ("#" + num5)),
						D = value3
					};
					value.CapEnded = false;
					value.CapSeen = now;
					outbox.Add(value.Cap.Copy());
					appeared.Add(new KeyValuePair<DateTime, NetCapt>(now, value.Cap.Copy()));
					if (appeared.Count > 300)
					{
						appeared.RemoveAt(0);
					}
					Say("Из игры: капт на зоне " + id + ": " + value.Cap.A + " → " + (value.Cap.D ?? ("#" + num6)) + ", начало " + Hm(num4) + ".");
				}
				if (!value.CapEnded && num == num5 && num5 != 0 && (!flag || owner != num) && num2 / 1000 >= num4)
				{
					EndCapt(value, num2 / 1000, "a", exact: true);
				}
			}
		}
		else if (value.Cap != null && !value.CapEnded)
		{
			if (num == value.Cap.AId && num2 / 1000 >= value.Cap.Start)
			{
				EndCapt(value, num2 / 1000, "a", exact: true);
			}
			else if (!snapshot)
			{
				EndCapt(value, end, "d", exact: false);
			}
			else
			{
				value.CapEnded = true;
			}
		}
		if (list == null && value.CapEnded)
		{
			value.Cap = null;
		}
		bool flag2 = flag && (owner != num || value.SinceMs != num2);
		value.Owner = num;
		value.SinceMs = num2;
		if (flag2 && !snapshot && num != 0)
		{
			ownersOut.RemoveAll((NetOwner o) => o.Zone == id);
			ownersOut.Add(MakeOwner(id, value));
		}
	}

	private static void EndCapt(ZoneRec r, long end, string winner, bool exact)
	{
		r.CapEnded = true;
		NetCapt netCapt = r.Cap.Copy();
		netCapt.End = end;
		netCapt.Winner = winner;
		outbox.Add(netCapt);
		ended.Add(netCapt.Copy());
		if (ended.Count > 200)
		{
			ended.RemoveAt(0);
		}
		Say("Из игры: капт на зоне " + netCapt.Zone + " кончился в " + Hms(end) + ((winner == "a") ? (" — " + netCapt.A + " взяли зону.") : (" — защита отбилась" + (exact ? "." : " (время по приходу события)."))));
	}

	private static string Hm(long unix)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unix).AddHours(3.0).ToString("HH:mm");
	}

	private static string Hms(long unix)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unix).AddHours(3.0).ToString("HH:mm:ss");
	}

	public static void Reset()
	{
		lock (Sync)
		{
			zones.Clear();
			outbox.Clear();
			ended.Clear();
			appeared.Clear();
			ownersOut.Clear();
			ownersFull = false;
			limits = null;
			limitsDirty = false;
		}
		inChunks = false;
		GameServer = null;
		OwnFamily = null;
		LastFullUtc = (LastUpdateUtc = DateTime.MinValue);
	}

	private static IEnumerable<byte[]> Inflate(byte[] m)
	{
		for (int i = 0; i + 2 < m.Length; i++)
		{
			if (m[i] != 120 || (m[i + 1] != 1 && m[i + 1] != 94 && m[i + 1] != 156 && m[i + 1] != 218))
			{
				continue;
			}
			byte[] res = null;
			try
			{
				using MemoryStream stream = new MemoryStream(m, i + 2, m.Length - i - 2);
				using DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
				using MemoryStream memoryStream = new MemoryStream();
				byte[] array = new byte[8192];
				int count;
				while ((count = deflateStream.Read(array, 0, array.Length)) > 0)
				{
					memoryStream.Write(array, 0, count);
					if (memoryStream.Length > 8388608)
					{
						break;
					}
				}
				res = memoryStream.ToArray();
			}
			catch
			{
				res = null;
			}
			if (res != null && res.Length > 0)
			{
				yield return res;
				break;
			}
		}
	}

	private static int IndexOf(byte[] h, byte[] n, int from)
	{
		for (int i = from; i <= h.Length - n.Length; i++)
		{
			int j;
			for (j = 0; j < n.Length && h[i + j] == n[j]; j++)
			{
			}
			if (j == n.Length)
			{
				return i;
			}
		}
		return -1;
	}
}
