using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CaptBot;

internal static class Family
{
	private const string CyrLook = "АВЕКМНОРСТУХаеорсухкмтвнзпиПгь";

	private const string LatLook = "ABEKMHOPCTYXaeopcyxkmmbh3nuNrb";

	public static readonly PointF AppIcon = new PointF(342f, 165f);

	private static string Dir => Path.Combine(AppPaths.Data, "calib");

	public static string MatchKey(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in s)
		{
			int num = "АВЕКМНОРСТУХаеорсухкмтвнзпиПгь".IndexOf(c);
			stringBuilder.Append((num >= 0) ? "ABEKMHOPCTYXaeopcyxkmmbh3nuNrb"[num] : c);
		}
		return Zones.Key(stringBuilder.ToString());
	}

	public static int Distance(string a, string b)
	{
		int[] array = new int[b.Length + 1];
		int[] array2 = new int[b.Length + 1];
		for (int i = 0; i <= b.Length; i++)
		{
			array[i] = i;
		}
		for (int j = 1; j <= a.Length; j++)
		{
			array2[0] = j;
			for (int k = 1; k <= b.Length; k++)
			{
				array2[k] = Math.Min(Math.Min(array2[k - 1] + 1, array[k] + 1), array[k - 1] + ((a[j - 1] != b[k - 1]) ? 1 : 0));
			}
			int[] array3 = array;
			array = array2;
			array2 = array3;
		}
		return array[b.Length];
	}

	private static int Allowed(string ka, string kb)
	{
		return Math.Max(1, (int)Math.Round(0.3 * (double)Math.Max(ka.Length, kb.Length)));
	}

	public static bool SameFamily(string a, string b)
	{
		string text = MatchKey(a);
		string text2 = MatchKey(b);
		if (text.Length == 0 || text2.Length == 0)
		{
			return false;
		}
		if (!(text == text2))
		{
			return Distance(text, text2) <= Allowed(text, text2);
		}
		return true;
	}

	public static string Snap(string read, IEnumerable<string> known)
	{
		string text = MatchKey(read);
		if (text.Length == 0 || known == null)
		{
			return null;
		}
		string text2 = null;
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		foreach (string item in known.Distinct())
		{
			string text3 = MatchKey(item);
			if (text3.Length == 0)
			{
				continue;
			}
			int num3 = ((!(text3 == text)) ? Distance(text, text3) : 0);
			if (num3 <= Allowed(text, text3))
			{
				if (num3 < num)
				{
					num2 = num;
					num = num3;
					text2 = item;
				}
				else if (num3 < num2 && MatchKey(text2) != text3)
				{
					num2 = num3;
				}
			}
		}
		if (text2 == null || num2 == num)
		{
			return null;
		}
		return text2;
	}

	public static FamilyInfo Read(Frame f, Layout l, Scr s)
	{
		FamilyInfo familyInfo = new FamilyInfo();
		bool flag = s == Scr.Home;
		familyInfo.Where = (flag ? "главный экран" : "меню семьи");
		if (s != Scr.Home && s != Scr.SkyeMenu)
		{
			familyInfo.Error = "планшет не на главном экране";
			return familyInfo;
		}
		double bx = (flag ? 252 : 90);
		double by = (flag ? 100 : 70);
		double num = (flag ? 180 : 680);
		double num2 = (flag ? 170 : 95);
		Rectangle r = new Rectangle((int)Math.Round(l.X(bx)), (int)Math.Round(l.Y(by)), (int)Math.Round(num * l.Sx), (int)Math.Round(num2 * l.Sy));
		double num3 = Math.Max(2.0, Math.Min(4.0, Math.Round(2.2 / Math.Min(l.Sx, l.Sy))));
		using Bitmap bitmap = f.ToBitmap(r);
		using Bitmap bmp = Scale(bitmap, num3);
		try
		{
			Directory.CreateDirectory(Dir);
			familyInfo.ImagePath = Path.Combine(Dir, flag ? "family_home.png" : "family_menu.png");
			bitmap.Save(familyInfo.ImagePath, ImageFormat.Png);
		}
		catch
		{
			familyInfo.ImagePath = null;
		}
		if (!WinOcr.Available)
		{
			familyInfo.Error = WinOcr.Error;
			return familyInfo;
		}
		List<OcrLine> list = WinOcr.Recognize(bmp);
		familyInfo.Seen = string.Join(" | ", list.Select((OcrLine x) => x.Text).Distinct().ToArray());
		if (list.Count == 0)
		{
			familyInfo.Error = WinOcr.Error ?? "на снимке не нашлось текста";
			return familyInfo;
		}
		OcrLine ocrLine = null;
		double num4 = double.MaxValue;
		string name = null;
		foreach (OcrLine item in list)
		{
			string text = Clean(item.Text);
			if (text != null)
			{
				double num5 = l.BX((double)r.X + (double)(item.Box.X + item.Box.Width / 2f) / num3);
				double num6 = l.BY((double)r.Y + (double)(item.Box.Y + item.Box.Height / 2f) / num3);
				double num7 = ((!flag) ? ((double)((0f - item.Box.Height) * 10f) + num6 * 0.1) : (Math.Abs(num5 - (double)AppIcon.X) + 0.6 * Math.Abs(num6 - (double)(AppIcon.Y + 45f)) + (double)((num6 < (double)AppIcon.Y) ? 60 : 0)));
				if (num7 < num4)
				{
					num4 = num7;
					ocrLine = item;
					name = text;
				}
			}
		}
		if (ocrLine == null)
		{
			familyInfo.Error = "среди надписей нет похожей на название семьи";
			return familyInfo;
		}
		familyInfo.Name = name;
		familyInfo.Label = new PointF((float)l.BX((double)r.X + (double)(ocrLine.Box.X + ocrLine.Box.Width / 2f) / num3), (float)l.BY((double)r.Y + (double)(ocrLine.Box.Y + ocrLine.Box.Height / 2f) / num3));
		return familyInfo;
	}

	private static Bitmap Scale(Bitmap b, double k)
	{
		Bitmap bitmap = new Bitmap(Math.Max(1, (int)((double)b.Width * k)), Math.Max(1, (int)((double)b.Height * k)), PixelFormat.Format24bppRgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		graphics.DrawImage(b, 0, 0, bitmap.Width, bitmap.Height);
		return bitmap;
	}

	public static string Clean(string s)
	{
		if (s == null)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = s.Trim();
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' || c == '.' || c == '\'' || c == '&')
			{
				stringBuilder.Append(c);
			}
		}
		string text2 = Regex.Replace(stringBuilder.ToString(), "\\s+", " ").Trim(' ', '-', '_', '.', '\'');
		if (text2.Length < 2 || text2.Length > 32)
		{
			return null;
		}
		if (text2.Count(char.IsLetter) < 2)
		{
			return null;
		}
		if (Regex.IsMatch(text2, "^\\d{1,2}[:.]\\d{2}"))
		{
			return null;
		}
		return text2;
	}
}
