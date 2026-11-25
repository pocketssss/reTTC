using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CaptBot;

internal static class WinOcr
{
	private static readonly object Sync = new object();

	private static bool tried;

	private static bool ok;

	private static readonly List<object> engines = new List<object>();

	private static Type tEngine;

	private static Type tResult;

	private static Type tLine;

	private static Type tWord;

	private static Type tFile;

	private static Type tStream;

	private static Type tDecoder;

	private static Type tBitmap;

	private static Type tAccess;

	private static MethodInfo asTask;

	public static string Error;

	private static object enEngine;

	private static bool memFailed;

	private static MethodInfo asBuffer;

	private static MethodInfo copyFromBuffer;

	private static object bgra8;

	public static bool Available
	{
		get
		{
			lock (Sync)
			{
				return Init();
			}
		}
	}

	public static bool HasLatin
	{
		get
		{
			lock (Sync)
			{
				return Init() && enEngine != null;
			}
		}
	}

	private static Type T(string name)
	{
		string[] array = new string[5] { "Windows.Foundation", "Windows.Media", "Windows.Storage", "Windows.Graphics", "Windows" };
		foreach (string text in array)
		{
			try
			{
				Type type = Type.GetType(name + ", " + text + ", ContentType=WindowsRuntime", throwOnError: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}
		throw new Exception("нет типа " + name);
	}

	private static bool Init()
	{
		if (tried)
		{
			return ok;
		}
		tried = true;
		try
		{
			Assembly assembly = Assembly.Load("System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type = assembly.GetType("System.WindowsRuntimeSystemExtensions", throwOnError: true);
			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!(methodInfo.Name != "AsTask") && methodInfo.IsGenericMethodDefinition)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (parameters.Length == 1 && parameters[0].ParameterType.Name == "IAsyncOperation`1")
					{
						asTask = methodInfo;
						break;
					}
				}
			}
			if (asTask == null)
			{
				throw new Exception("нет AsTask");
			}
			tEngine = T("Windows.Media.Ocr.OcrEngine");
			tResult = T("Windows.Media.Ocr.OcrResult");
			tLine = T("Windows.Media.Ocr.OcrLine");
			tWord = T("Windows.Media.Ocr.OcrWord");
			tFile = T("Windows.Storage.StorageFile");
			tAccess = T("Windows.Storage.FileAccessMode");
			tStream = T("Windows.Storage.Streams.IRandomAccessStream");
			tDecoder = T("Windows.Graphics.Imaging.BitmapDecoder");
			tBitmap = T("Windows.Graphics.Imaging.SoftwareBitmap");
			object obj = tEngine.GetMethod("TryCreateFromUserProfileLanguages").Invoke(null, null);
			if (obj != null)
			{
				engines.Add(obj);
			}
			try
			{
				Type type2 = T("Windows.Globalization.Language");
				object value = tEngine.GetProperty("AvailableRecognizerLanguages").GetValue(null, null);
				foreach (object item in (IEnumerable)value)
				{
					string text = Convert.ToString(type2.GetProperty("LanguageTag").GetValue(item, null));
					if (text.StartsWith("en", StringComparison.OrdinalIgnoreCase))
					{
						object obj2 = tEngine.GetMethod("TryCreateFromLanguage").Invoke(null, new object[1] { item });
						if (obj2 != null)
						{
							engines.Add(obj2);
							enEngine = obj2;
							break;
						}
					}
				}
			}
			catch
			{
			}
			if (engines.Count == 0)
			{
				throw new Exception("в Windows не установлен язык для распознавания текста");
			}
			ok = true;
		}
		catch (Exception ex)
		{
			Exception ex2 = ((ex is TargetInvocationException && ex.InnerException != null) ? ex.InnerException : ex);
			Error = "распознавание текста Windows недоступно: " + ex2.Message;
			ok = false;
		}
		return ok;
	}

	private static object Await(object op, Type result)
	{
		Task task = (Task)asTask.MakeGenericMethod(result).Invoke(null, new object[1] { op });
		if (!task.Wait(10000))
		{
			throw new TimeoutException("распознавание не ответило");
		}
		return task.GetType().GetProperty("Result").GetValue(task, null);
	}

	private static object Prop(object o, Type declared, string name)
	{
		PropertyInfo propertyInfo = ((declared != null) ? declared.GetProperty(name) : null);
		if (propertyInfo == null)
		{
			propertyInfo = o.GetType().GetProperty(name);
		}
		return propertyInfo.GetValue(o, null);
	}

	private static double Num(object o, string name)
	{
		return Convert.ToDouble(o.GetType().GetProperty(name).GetValue(o, null), CultureInfo.InvariantCulture);
	}

	public static List<OcrLine> Recognize(Bitmap bmp)
	{
		return Recognize(bmp, words: false, latin: false);
	}

	public static List<OcrLine> Recognize(Bitmap bmp, bool words)
	{
		return Recognize(bmp, words, latin: false);
	}

	private static object FromMemory(Bitmap bmp)
	{
		if (memFailed)
		{
			return null;
		}
		try
		{
			if (copyFromBuffer == null)
			{
				Assembly assembly = Assembly.Load("System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				Type type = assembly.GetType("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions", throwOnError: true);
				asBuffer = type.GetMethods(BindingFlags.Static | BindingFlags.Public).First((MethodInfo m) => m.Name == "AsBuffer" && m.GetParameters().Length == 1);
				Type type2 = T("Windows.Storage.Streams.IBuffer");
				Type type3 = T("Windows.Graphics.Imaging.BitmapPixelFormat");
				copyFromBuffer = tBitmap.GetMethod("CreateCopyFromBuffer", new Type[4]
				{
					type2,
					type3,
					typeof(int),
					typeof(int)
				});
				bgra8 = Enum.Parse(type3, "Bgra8");
				if (copyFromBuffer == null)
				{
					throw new Exception("нет CreateCopyFromBuffer");
				}
			}
			int width = bmp.Width;
			int height = bmp.Height;
			byte[] array = new byte[width * height * 4];
			BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			try
			{
				for (int i = 0; i < height; i++)
				{
					Marshal.Copy(bitmapData.Scan0 + i * bitmapData.Stride, array, i * width * 4, width * 4);
				}
			}
			finally
			{
				bmp.UnlockBits(bitmapData);
			}
			object obj = asBuffer.Invoke(null, new object[1] { array });
			return copyFromBuffer.Invoke(null, new object[4] { obj, bgra8, width, height });
		}
		catch
		{
			memFailed = true;
			return null;
		}
	}

	public static List<OcrLine> Recognize(Bitmap bmp, bool words, bool latin)
	{
		List<OcrLine> list = new List<OcrLine>();
		lock (Sync)
		{
			if (!Init())
			{
				return list;
			}
			string text = null;
			object obj = null;
			object obj2 = null;
			try
			{
				obj2 = FromMemory(bmp);
				if (obj2 == null)
				{
					text = Path.Combine(Path.GetTempPath(), "captbot_ocr_" + Guid.NewGuid().ToString("N") + ".bmp");
					bmp.Save(text, ImageFormat.Bmp);
					object obj3 = Await(tFile.GetMethod("GetFileFromPathAsync").Invoke(null, new object[1] { text }), tFile);
					obj = Await(tFile.GetMethod("OpenAsync", new Type[1] { tAccess }).Invoke(obj3, new object[1] { Enum.ToObject(tAccess, 0) }), tStream);
					object obj4 = Await(tDecoder.GetMethod("CreateAsync", new Type[1] { tStream }).Invoke(null, new object[1] { obj }), tDecoder);
					obj2 = Await(tDecoder.GetMethod("GetSoftwareBitmapAsync", Type.EmptyTypes).Invoke(obj4, null), tBitmap);
				}
				List<object> list2;
				if (!latin || enEngine == null)
				{
					list2 = engines;
				}
				else
				{
					List<object> list3 = new List<object>();
					list3.Add(enEngine);
					list2 = list3;
				}
				foreach (object item in list2)
				{
					object o = Await(tEngine.GetMethod("RecognizeAsync").Invoke(item, new object[1] { obj2 }), tResult);
					foreach (object item2 in (IEnumerable)Prop(o, tResult, "Lines"))
					{
						OcrLine ocrLine = new OcrLine();
						ocrLine.Text = Convert.ToString(Prop(item2, tLine, "Text"));
						RectangleF rectangleF = RectangleF.Empty;
						foreach (object item3 in (IEnumerable)Prop(item2, tLine, "Words"))
						{
							object o2 = Prop(item3, tWord, "BoundingRect");
							RectangleF rectangleF2 = new RectangleF((float)Num(o2, "X"), (float)Num(o2, "Y"), (float)Num(o2, "Width"), (float)Num(o2, "Height"));
							rectangleF = (rectangleF.IsEmpty ? rectangleF2 : RectangleF.Union(rectangleF, rectangleF2));
							if (words)
							{
								list.Add(new OcrLine
								{
									Text = Convert.ToString(Prop(item3, tWord, "Text")),
									Box = rectangleF2
								});
							}
						}
						if (!words)
						{
							ocrLine.Box = rectangleF;
							if (!string.IsNullOrWhiteSpace(ocrLine.Text))
							{
								list.Add(ocrLine);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ((ex is TargetInvocationException && ex.InnerException != null) ? ex.InnerException : ex);
				if (ex2 is AggregateException && ex2.InnerException != null)
				{
					ex2 = ex2.InnerException;
				}
				Error = "ошибка распознавания: " + ex2.Message;
			}
			finally
			{
				if (obj2 is IDisposable disposable)
				{
					try
					{
						disposable.Dispose();
					}
					catch
					{
					}
				}
				if (obj is IDisposable disposable2)
				{
					try
					{
						disposable2.Dispose();
					}
					catch
					{
					}
				}
				if (text != null)
				{
					try
					{
						File.Delete(text);
					}
					catch
					{
					}
				}
			}
		}
		return list;
	}
}
