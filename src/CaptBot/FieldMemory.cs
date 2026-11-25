using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;

namespace CaptBot;

internal class FieldMemory
{
	private readonly string dir;

	private readonly Dictionary<int, Gray> hrs = new Dictionary<int, Gray>();

	private readonly Dictionary<int, Gray> mins = new Dictionary<int, Gray>();

	public readonly int W;

	public readonly int H;

	private string MarkFile => Path.Combine(dir, "fields.v2");

	public FieldMemory(int w, int h)
	{
		W = w;
		H = h;
		dir = Calib.FolderFor(w, h);
		try
		{
			if (!Directory.Exists(dir))
			{
				return;
			}
			DropOld();
			string[] files = Directory.GetFiles(dir, "*.png");
			foreach (string path in files)
			{
				Match match = Regex.Match(Path.GetFileNameWithoutExtension(path), "^([hm])(\\d\\d)$");
				if (!match.Success)
				{
					continue;
				}
				bool flag = match.Groups[1].Value == "h";
				int num = int.Parse(match.Groups[2].Value);
				bool num2;
				if (!flag)
				{
					if (num > 55)
					{
						continue;
					}
					num2 = num % 5 != 0;
				}
				else
				{
					num2 = num > 23;
				}
				if (!num2)
				{
					using Bitmap b = new Bitmap(new MemoryStream(File.ReadAllBytes(path)));
					(flag ? hrs : mins)[Ocr.Index(flag, num)] = Gray.From(b);
				}
			}
		}
		catch
		{
		}
	}

	private void DropOld()
	{
		if (File.Exists(MarkFile))
		{
			return;
		}
		string[] files = Directory.GetFiles(dir, "m??.png");
		foreach (string path in files)
		{
			File.Delete(path);
		}
		List<Gray> list = new List<Gray>();
		string[] files2 = Directory.GetFiles(dir, "h??.png");
		foreach (string path2 in files2)
		{
			using Bitmap b = new Bitmap(new MemoryStream(File.ReadAllBytes(path2)));
			list.Add(Gray.From(b));
		}
		bool flag = false;
		for (int k = 0; k < list.Count; k++)
		{
			if (flag)
			{
				break;
			}
			for (int l = 0; l < k; l++)
			{
				if (flag)
				{
					break;
				}
				flag = list[k].W == list[l].W && list[k].H == list[l].H && Ocr.FieldDiff(list[k], list[l]) < 0.5;
			}
		}
		if (flag)
		{
			string[] files3 = Directory.GetFiles(dir, "h??.png");
			foreach (string path3 in files3)
			{
				File.Delete(path3);
			}
		}
		Mark();
	}

	private void Mark()
	{
		File.WriteAllText(MarkFile, "2");
	}

	public int Count(bool hours)
	{
		return (hours ? hrs : mins).Count;
	}

	public bool Full(bool hours)
	{
		return Count(hours) == Ocr.Count(hours);
	}

	public int Match(Gray field, bool hours, out double cost)
	{
		cost = double.MaxValue;
		double num = double.MaxValue;
		int num2 = -1;
		int x = (int)Math.Round((double)(5 * field.W) / 56.0);
		int x2 = (int)Math.Round((double)(51 * field.W) / 56.0);
		int y = (int)Math.Round((double)(3 * field.H) / 28.0);
		int y2 = (int)Math.Round((double)(25 * field.H) / 28.0);
		foreach (KeyValuePair<int, Gray> item in hours ? hrs : mins)
		{
			if (item.Value.W == field.W && item.Value.H == field.H)
			{
				double num3 = Gray.Diff(field, item.Value, x, x2, y, y2);
				if (num3 < cost)
				{
					num = cost;
					cost = num3;
					num2 = item.Key;
				}
				else if (num3 < num)
				{
					num = num3;
				}
			}
		}
		if (num2 < 0 || cost > (hours ? 3.0 : 0.5) || (num < double.MaxValue && cost >= num * 0.6))
		{
			return -1;
		}
		return Ocr.Value(hours, num2);
	}

	public void Learn(Gray field, bool hours, int idx)
	{
		if (!(hours ? hrs : mins).ContainsKey(idx))
		{
			Store(field, hours, idx);
		}
	}

	public void LearnAll(List<Gray> crops, bool hours, int startIdx)
	{
		int num = Ocr.Count(hours);
		for (int i = 0; i < crops.Count; i++)
		{
			Store(crops[i], hours, (startIdx + i) % num);
		}
	}

	private void Store(Gray field, bool hours, int idx)
	{
		(hours ? hrs : mins)[idx] = field;
		try
		{
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
				Mark();
			}
			using Bitmap bitmap = field.ToBitmap();
			bitmap.Save(Path.Combine(dir, (hours ? "h" : "m") + Ocr.Value(hours, idx).ToString("00") + ".png"), ImageFormat.Png);
		}
		catch
		{
		}
	}

	public static void Forget(int w, int h)
	{
		try
		{
			string path = Calib.FolderFor(w, h);
			if (!Directory.Exists(path))
			{
				return;
			}
			string[] files = Directory.GetFiles(path, "*.png");
			foreach (string path2 in files)
			{
				if (Regex.IsMatch(Path.GetFileName(path2), "^[hm]\\d\\d\\.png$"))
				{
					File.Delete(path2);
				}
			}
		}
		catch
		{
		}
	}
}
