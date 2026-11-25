using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace CaptBot;

internal class VzzWatcher
{
	private class Job
	{
		public string Text;

		public bool Check;
	}

	private class Check
	{
		public string Text;

		public string Code;

		public string Picture;

		public string PictureRow;

		public double Since;
	}

	private class Read
	{
		public bool Answerable;

		public string Code = "";

		public string Hold = "";

		public string Message;
	}

	private const string FIND = "([channel, hosts]) => {\r\n const out = {href: location.href, signed: !!document.querySelector('[data-list-id=\"guildsnav\"]'), ready: false};\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n const rows = (list || document).querySelectorAll('[id^=\"chat-messages-' + channel + '-\"]');\r\n if (!list && !rows.length) {\r\n   for (const el of document.querySelectorAll('button, a, [role=\"button\"]')) {\r\n     const text = (el.textContent || '').trim().toLowerCase();\r\n     if (['продолжить в браузере', 'continue in browser'].includes(text)) { el.click(); break; }\r\n   }\r\n   return out;\r\n }\r\n out.ready = true;\r\n out.open = !!document.querySelector('form [role=\"textbox\"][contenteditable=\"true\"]');\r\n out.item = null;\r\n for (const row of Array.from(rows).reverse()) {\r\n   for (const img of Array.from(row.querySelectorAll('img')).reverse()) {\r\n     const url = new URL(img.currentSrc || img.src, location.href);\r\n     if (hosts.includes(url.hostname) && url.pathname.startsWith('/attachments/' + channel + '/')) {\r\n       out.item = [row.id, url.pathname, url.href];\r\n       return out;\r\n     }\r\n   }\r\n }\r\n return out;\r\n}";

	private const string ROWS = "([channel, hosts]) => {\r\n const prefix = 'chat-messages-' + channel + '-';\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n if (!list) return [];\r\n const out = [];\r\n let author = '';\r\n for (const row of list.querySelectorAll('[id^=\"' + prefix + '\"]')) {\r\n   const id = row.id.slice(prefix.length);\r\n   const names = row.querySelectorAll('[id^=\"message-username-\"]');\r\n   if (names.length) author = names[names.length - 1].textContent.trim();\r\n   const picture = Array.from(row.querySelectorAll('img')).some(img => {\r\n     const url = new URL(img.currentSrc || img.src, location.href);\r\n     return hosts.includes(url.hostname) && url.pathname.startsWith('/attachments/' + channel + '/');\r\n   });\r\n   if (picture) out.length = 0;\r\n   const parts = row.querySelectorAll('[id=\"message-content-' + id + '\"]');\r\n   out.push({row: row.id, author, text: parts.length ? parts[parts.length - 1].innerText : ''});\r\n }\r\n return out;\r\n}";

	private const string BOX = "form [role=\"textbox\"][contenteditable=\"true\"]";

	private const string PASTE = "([sel, text]) => {\r\n const box = document.querySelector(sel);\r\n if (!box) return false;\r\n const data = new DataTransfer();\r\n data.setData('text/plain', text);\r\n box.focus();\r\n box.dispatchEvent(new ClipboardEvent('paste', {clipboardData: data, bubbles: true, cancelable: true}));\r\n return true;\r\n}";

	private const string BOX_TEXT = "sel => { const b = document.querySelector(sel); return b ? b.innerText : null; }";

	private const string RECT = "sel => {\r\n const el = document.querySelector(sel);\r\n if (!el) return null;\r\n el.scrollIntoView({block: 'nearest', inline: 'nearest'});\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}";

	private const string CONTENT_RECT = "([rowId, mid]) => {\r\n const row = document.getElementById(rowId);\r\n if (!row) return null;\r\n const parts = row.querySelectorAll('[id=\"message-content-' + mid + '\"]');\r\n const el = parts[parts.length - 1];\r\n if (!el) return null;\r\n el.scrollIntoView({block: 'center'});\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + Math.min(r.width / 2, 40), r.top + r.height / 2] : null;\r\n}";

	private const string MENU_DELETE = "() => {\r\n let el = document.querySelector('#message-delete, #message-actions-delete');\r\n if (!el) el = Array.from(document.querySelectorAll('[role=\"menuitem\"]')).find(e => /удалить сообщение|delete message/i.test(e.textContent || ''));\r\n if (!el) return null;\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}";

	private const string CONFIRM = "() => {\r\n const el = Array.from(document.querySelectorAll('[role=\"dialog\"] button, [role=\"alertdialog\"] button')).find(e => /удалить|delete/i.test(e.textContent || ''));\r\n if (!el) return null;\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}";

	private const string HAS_ROW = "id => !!document.getElementById(id)";

	private const string IDS = "channel => Array.from(document.querySelectorAll('[id^=\"chat-messages-' + channel + '-\"]'), row => row.id)";

	private const string DELIVERY = "([channel, known]) => {\r\n const prefix = 'chat-messages-' + channel + '-';\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n if (!list) return [];\r\n const cls = el => (el && el.getAttribute('class')) || '';\r\n const out = [];\r\n for (const row of list.querySelectorAll('[id^=\"' + prefix + '\"]')) {\r\n   if (known.includes(row.id)) continue;\r\n   const id = row.id.slice(prefix.length);\r\n   const parts = row.querySelectorAll('[id=\"message-content-' + id + '\"]');\r\n   const content = parts.length ? parts[parts.length - 1] : null;\r\n   const text = content ? content.innerText : '';\r\n   let opacity = 1;\r\n   for (let el = content; el; el = el.parentElement) {\r\n     const o = parseFloat(getComputedStyle(el).opacity);\r\n     if (!isNaN(o)) opacity *= o;\r\n     if (el === row) break;\r\n   }\r\n   const rest = text ? row.innerText.replace(text, '') : row.innerText;\r\n   const notice = /ephemeral/i.test(cls(row)) || !!row.querySelector('[class*=\"ephemeral\"]')\r\n     || /only you can see this|видите только вы|только вы видите|видно только вам/i.test(rest);\r\n   out.push({\r\n     row: row.id, text,\r\n     sending: !!content && (/isSending/.test(cls(content)) || opacity < 0.9),\r\n     failed: !!content && /isFailed/.test(cls(content)),\r\n     notice: notice ? (text || row.innerText).trim() : '',\r\n   });\r\n }\r\n return out;\r\n}";

	private const string FETCH = "async url => {\r\n try {\r\n   const r = await fetch(url, {cache: 'force-cache', credentials: 'omit'});\r\n   if (!r.ok) return {status: r.status};\r\n   const b = new Uint8Array(await r.arrayBuffer());\r\n   let s = '';\r\n   for (let i = 0; i < b.length; i += 0x8000) s += String.fromCharCode.apply(null, b.subarray(i, i + 0x8000));\r\n   return {data: btoa(s)};\r\n } catch (e) { return {error: String(e)}; }\r\n}";

	private const string DATE = "async () => { try { const r = await fetch('/api/v9/gateway', {cache: 'no-store'}); return r.headers.get('date'); } catch (e) { return null; } }";

	private const double DISCORD_EPOCH = 1420070400.0;

	private const double EARLIER = 10.0;

	public const int LATE = 60;

	public const int DELIVER_WAIT = 10;

	private const double SHUT_MIN = 3.0;

	private const double CHECK_WAIT = 60.0;

	private const double FIND_WAIT = 15.0;

	private const double STUCK = 25.0;

	private const double REOPEN = 15.0;

	private const double AWAY = 2.0;

	public const string WATCHING = "Новые картинки из канала распознаются сами. Окно браузера можно свернуть, только не закрывайте его.";

	private const string LOGIN = "Войдите в Discord в открывшемся окне браузера. Канал откроется сам, как только вход пройдёт.";

	private const string SIGNED_IN = "Вход выполнен, открываю канал…";

	private const string OPENING = "Жду, пока Discord покажет сообщения канала.";

	private const string RELOADING = "Discord долго не показывает канал, загружаю страницу заново…";

	public const string CLOSED = "Окно браузера закрыто. Нажмите «Включить», чтобы открыть его снова.";

	private const string SHUT = "Чат закрыт для записи.";

	private const string SHUT_WAIT = "Чат закрыт для записи. Как только откроют, сразу отправлю 2 строки, а если до этого придёт картинка — 3 строки с кодом.";

	private const string SHUT_CODE = "Чат закрыт для записи. Сообщение с кодом готово: отправлю, как только откроют.";

	private const string SHUT_NOCODE = "Чат закрыт для записи. Картинка пришла, но код не распознан: при открытии сам ничего не отправлю.";

	private static readonly int[] ANSWERS = new int[2] { 2, 5 };

	private static readonly Regex CODE = new Regex("[A-Za-z0-9]+");

	private static readonly string[] APP_PATHS = new string[8] { "/channels/", "/app", "/quest-home", "/store", "/shop", "/discovery", "/library", "/message-requests" };

	public static string DiscordHost = "discord.com";

	public static string[] CdnHosts = new string[2] { "cdn.discordapp.com", "media.discordapp.net" };

	public volatile VzzSettings Settings = new VzzSettings();

	public Action<VzzEvent> Emit;

	public string Root;

	public bool Headless;

	private volatile bool stop;

	private readonly AutoResetEvent wake = new AutoResetEvent(initialState: false);

	private double started;

	private double clockAt;

	private readonly VzzClock clock = new VzzClock();

	private string randomColor = "";

	private string standbyColor;

	private string lastState;

	private CdpBrowser browser;

	public bool Stopping => stop;

	public void Stop()
	{
		stop = true;
		wake.Set();
	}

	private void Fire(VzzEvent e)
	{
		Action<VzzEvent> emit = Emit;
		if (emit != null)
		{
			try
			{
				emit(e);
			}
			catch
			{
			}
		}
	}

	private void State(string tone, string text)
	{
		string text2 = tone + "|" + text;
		if (!(text2 == lastState))
		{
			lastState = text2;
			Fire(new VzzEvent
			{
				Kind = "state",
				Tone = tone,
				Text = text
			});
		}
	}

	private void Note(string kind, string text)
	{
		Fire(new VzzEvent
		{
			Kind = kind,
			Text = text
		});
	}

	private static double Now()
	{
		return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	public void Run(string url)
	{
		CdpPage page = null;
		started = Now();
		try
		{
			State("start", "Проверяю ключ и распознавание на сайте…");
			if (!VzzOcr.Hello(out var ms, out var error))
			{
				Fire(new VzzEvent
				{
					Kind = "ocr",
					Ok = false,
					Text = error
				});
				Note("error", "Распознавание на сайте TTC: " + error + ".");
				return;
			}
			Fire(new VzzEvent
			{
				Kind = "ocr",
				Ok = true,
				OcrMs = ms
			});
			if (!stop)
			{
				State("start", "Открываю отдельное окно браузера…");
				browser = CdpBrowser.Open(Root, url, Headless, out error);
				if (browser == null)
				{
					Fire(new VzzEvent
					{
						Kind = "browser",
						Ok = false,
						Text = error
					});
					Note("error", error);
				}
				else
				{
					Fire(new VzzEvent
					{
						Kind = "browser",
						Ok = true,
						Text = browser.Name
					});
					Loop(url, ref page);
				}
			}
		}
		catch (Exception ex)
		{
			if (!stop)
			{
				if (ex is CdpClosed)
				{
					Note("note", "Окно браузера закрыто. Нажмите «Включить», чтобы открыть его снова.");
				}
				else
				{
					Note("error", Short(ex));
				}
			}
		}
		finally
		{
			page?.Dispose();
			if (browser != null)
			{
				try
				{
					browser.Close();
				}
				catch
				{
				}
			}
			Fire(new VzzEvent
			{
				Kind = "done"
			});
		}
	}

	private void Loop(string url, ref CdpPage page)
	{
		string text = url.Substring(url.LastIndexOf('/') + 1);
		string text2 = PathOf(url);
		string[] array = null;
		Check check = null;
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = Now();
		bool? flag = null;
		double num5 = 0.0;
		double num6 = 0.0;
		Job job = null;
		bool look = browser.Reused;
		bool flag2 = false;
		if (browser.Reused)
		{
			look = true;
		}
		bool flag3 = !browser.Reused;
		while (!stop)
		{
			page = Pick(page, text2, look);
			if (page == null)
			{
				Note("note", "Окно браузера закрыто. Нажмите «Включить», чтобы открыть его снова.");
				break;
			}
			if (!flag3)
			{
				flag3 = true;
				num = Now();
				Go(page, url);
			}
			Dictionary<string, object> dictionary = null;
			try
			{
				dictionary = page.Eval("([channel, hosts]) => {\r\n const out = {href: location.href, signed: !!document.querySelector('[data-list-id=\"guildsnav\"]'), ready: false};\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n const rows = (list || document).querySelectorAll('[id^=\"chat-messages-' + channel + '-\"]');\r\n if (!list && !rows.length) {\r\n   for (const el of document.querySelectorAll('button, a, [role=\"button\"]')) {\r\n     const text = (el.textContent || '').trim().toLowerCase();\r\n     if (['продолжить в браузере', 'continue in browser'].includes(text)) { el.click(); break; }\r\n   }\r\n   return out;\r\n }\r\n out.ready = true;\r\n out.open = !!document.querySelector('form [role=\"textbox\"][contenteditable=\"true\"]');\r\n out.item = null;\r\n for (const row of Array.from(rows).reverse()) {\r\n   for (const img of Array.from(row.querySelectorAll('img')).reverse()) {\r\n     const url = new URL(img.currentSrc || img.src, location.href);\r\n     if (hosts.includes(url.hostname) && url.pathname.startsWith('/attachments/' + channel + '/')) {\r\n       out.item = [row.id, url.pathname, url.href];\r\n       return out;\r\n     }\r\n   }\r\n }\r\n return out;\r\n}", new object[2] { text, CdnHosts }, 5000) as Dictionary<string, object>;
			}
			catch (CdpClosed)
			{
				page.Dispose();
				page = null;
				look = true;
				continue;
			}
			catch (CdpError)
			{
				dictionary = null;
			}
			if (dictionary == null)
			{
				wake.WaitOne(200);
				continue;
			}
			string text3 = Str(dictionary, "href");
			bool flag4 = PathOf(text3) == text2;
			look = !flag4;
			flag2 = false;
			if (!flag4)
			{
				num3 = 0.0;
				if (SignedIn(text3, Bool(dictionary, "signed")))
				{
					State("opening", "Вход выполнен, открываю канал…");
					if (num2 == 0.0)
					{
						num2 = Now();
					}
					if (Now() - num2 > 2.0 && Now() - num > 15.0)
					{
						num = Now();
						num2 = 0.0;
						Go(page, url);
					}
				}
				else
				{
					num2 = 0.0;
					State("login", "Войдите в Discord в открывшемся окне браузера. Канал откроется сам, как только вход пройдёт.");
				}
			}
			else
			{
				num2 = 0.0;
				if (!Bool(dictionary, "ready"))
				{
					if (num3 == 0.0)
					{
						num3 = Now();
					}
					if (Now() - num3 > 25.0 && Now() - num > 15.0)
					{
						State("opening", "Discord долго не показывает канал, загружаю страницу заново…");
						num = Now();
						num3 = 0.0;
						Go(page, url);
					}
					else
					{
						State("opening", "Жду, пока Discord покажет сообщения канала.");
					}
				}
				else
				{
					flag2 = true;
					num3 = 0.0;
					if (Now() - clockAt > 600.0)
					{
						LearnClock(page);
					}
					double num7 = Now();
					flag = Bool(dictionary, "open");
					object[] array2 = (dictionary.ContainsKey("item") ? (dictionary["item"] as object[]) : null);
					string[] array3 = ((array2 != null && array2.Length == 3) ? new string[3]
					{
						Convert.ToString(array2[0]),
						Convert.ToString(array2[1]),
						Convert.ToString(array2[2])
					} : null);
					if (array3 != null && (array == null || array[0] != array3[0] || array[1] != array3[1]))
					{
						array = array3;
						check = null;
						Read read = ReadPicture(page, array3, flag == true);
						if (read.Answerable)
						{
							num6 = num7;
							job = null;
						}
						VzzSettings settings = Settings;
						if (read.Code.Length > 0 && read.Hold.Length == 0 && settings.AutoSend && !stop)
						{
							Job job2 = new Job();
							job2.Text = read.Message;
							job2.Check = settings.Check;
							Job job3 = job2;
							if (flag == true)
							{
								check = Dispatch(page, text, job3, array);
							}
							else
							{
								job = job3;
								check = null;
								Fire(new VzzEvent
								{
									Kind = "waiting"
								});
							}
						}
					}
					else if (check != null)
					{
						check = Verify(page, text, check);
					}
					if (flag != true)
					{
						if (num5 == 0.0)
						{
							num5 = num7;
						}
						string text4 = Standby();
						State("live", (job != null) ? "Чат закрыт для записи. Сообщение с кодом готово: отправлю, как только откроют." : ((text4 == null) ? "Чат закрыт для записи." : ((num6 >= num5) ? "Чат закрыт для записи. Картинка пришла, но код не распознан: при открытии сам ничего не отправлю." : "Чат закрыт для записи. Как только откроют, сразу отправлю 2 строки, а если до этого придёт картинка — 3 строки с кодом.")));
					}
					else
					{
						string text5 = Standby();
						if (num5 > 0.0 && num7 - num5 >= 3.0 && job == null && num6 < num5 && text5 != null)
						{
							Fire(new VzzEvent
							{
								Kind = "opened",
								Message = text5,
								Color = standbyColor
							});
							Job job4 = new Job();
							job4.Text = text5;
							job4.Check = false;
							job = job4;
							standbyColor = null;
						}
						num5 = 0.0;
						if (job != null)
						{
							check = Dispatch(page, text, job, array);
							job = null;
						}
						State("live", "Новые картинки из канала распознаются сами. Окно браузера можно свернуть, только не закрывайте его.");
					}
				}
			}
			if (Now() - num4 > 50.0 && check == null && job == null)
			{
				num4 = Now();
				Warm();
			}
			wake.WaitOne((!flag2) ? 400 : ((num5 > 0.0) ? 40 : 40));
		}
	}

	private void Warm()
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			int ms;
			string error;
			bool ok = VzzOcr.Hello(out ms, out error);
			if (!stop)
			{
				Fire(new VzzEvent
				{
					Kind = "ocr",
					Ok = ok,
					OcrMs = ms,
					Text = error
				});
			}
		});
	}

	private void LearnClock(CdpPage page)
	{
		clockAt = Now();
		try
		{
			clock.Learn(page.Eval("async () => { try { const r = await fetch('/api/v9/gateway', {cache: 'no-store'}); return r.headers.get('date'); } catch (e) { return null; } }", null, 5000) as string);
		}
		catch (CdpClosed)
		{
			throw;
		}
		catch
		{
		}
	}

	private string Standby()
	{
		VzzSettings settings = Settings;
		string text = (settings.Line1 ?? "").Trim();
		if (!settings.AutoSend || text.Length == 0)
		{
			standbyColor = null;
			return null;
		}
		if (settings.Color != "Случайный")
		{
			standbyColor = settings.Color;
		}
		else if (standbyColor == null || VzzColors.Hex(standbyColor) == null)
		{
			standbyColor = VzzColors.Pick(standbyColor);
		}
		return text + "\n" + standbyColor;
	}

	private CdpPage Pick(CdpPage page, string want, bool look)
	{
		if (page != null && !page.Closed && !look)
		{
			return page;
		}
		List<CdpTarget> list;
		try
		{
			list = browser.Pages();
		}
		catch
		{
			list = null;
		}
		if (list == null || list.Count == 0)
		{
			if (list == null && page != null && !page.Closed)
			{
				return page;
			}
			page?.Dispose();
			return null;
		}
		CdpTarget cdpTarget = list.FirstOrDefault((CdpTarget p) => PathOf(p.Url) == want);
		if (cdpTarget == null)
		{
			if (page != null && !page.Closed)
			{
				return page;
			}
			cdpTarget = list[0];
		}
		if (page != null && !page.Closed && page.Id == cdpTarget.Id)
		{
			return page;
		}
		page?.Dispose();
		return CdpPage.Connect(cdpTarget);
	}

	private bool SignedIn(string href, bool guilds)
	{
		if (!Uri.TryCreate(href, UriKind.Absolute, out var u) || (!(u.Host == DiscordHost) && !u.Host.EndsWith("." + DiscordHost)))
		{
			return false;
		}
		if (APP_PATHS.Any((string p) => u.AbsolutePath.StartsWith(p)))
		{
			return true;
		}
		return guilds;
	}

	private static void Go(CdpPage page, string url)
	{
		try
		{
			page.Navigate(url);
		}
		catch (CdpClosed)
		{
			throw;
		}
		catch (CdpError)
		{
		}
	}

	private string Hold(double at, bool writable)
	{
		if (at < started + clock.Offset - 10.0)
		{
			return "Картинка была в канале до включения, её сам не отправляю.";
		}
		if (writable && clock.Now() - at > 60.0)
		{
			return "Картинке больше " + 60 + " секунд, её сам не отправляю.";
		}
		return "";
	}

	private Read ReadPicture(CdpPage page, string[] item, bool writable)
	{
		State("busy", "Распознаю новую картинку…");
		double num = Posted(item[0]);
		double seen = clock.Now() - num;
		string code;
		double sure;
		int ms;
		try
		{
			byte[] image = Fetch(page, item[2]);
			if (!VzzOcr.Read(image, MessageId(item[0]), out code, out sure, out ms, out var error))
			{
				throw new Exception("сайт TTC: " + error);
			}
		}
		catch (Exception ex)
		{
			if (ex is CdpClosed || page.Closed)
			{
				throw new CdpClosed();
			}
			Fire(new VzzEvent
			{
				Kind = "warn",
				Text = Short(ex)
			});
			Read read = new Read();
			read.Answerable = Hold(num, writable).Length == 0;
			return read;
		}
		string text = Hold(num, writable);
		VzzSettings settings = Settings;
		string text2 = ((settings.Color == "Случайный") ? (randomColor = VzzColors.Pick(randomColor)) : settings.Color);
		string message = (settings.Line1 ?? "").Trim() + "\n" + text2 + "\n" + code;
		if (!stop)
		{
			Fire(new VzzEvent
			{
				Kind = "result",
				Code = code,
				Hold = text,
				Sure = sure,
				Message = message,
				Color = text2,
				Seen = seen,
				OcrMs = ms,
				Sending = (code.Length > 0 && text.Length == 0 && settings.AutoSend)
			});
		}
		Read read2 = new Read();
		read2.Answerable = text.Length == 0;
		read2.Code = code;
		read2.Hold = text;
		read2.Message = message;
		return read2;
	}

	private byte[] Fetch(CdpPage page, string url)
	{
		string text = null;
		try
		{
			Dictionary<string, object> dictionary = page.Eval("async url => {\r\n try {\r\n   const r = await fetch(url, {cache: 'force-cache', credentials: 'omit'});\r\n   if (!r.ok) return {status: r.status};\r\n   const b = new Uint8Array(await r.arrayBuffer());\r\n   let s = '';\r\n   for (let i = 0; i < b.length; i += 0x8000) s += String.fromCharCode.apply(null, b.subarray(i, i + 0x8000));\r\n   return {data: btoa(s)};\r\n } catch (e) { return {error: String(e)}; }\r\n}", url, 8000) as Dictionary<string, object>;
			if (dictionary != null && dictionary.ContainsKey("data"))
			{
				return Convert.FromBase64String(Convert.ToString(dictionary["data"]));
			}
			if (dictionary != null && dictionary.ContainsKey("status"))
			{
				text = "Discord ответил HTTP " + dictionary["status"];
			}
		}
		catch (CdpClosed)
		{
			throw;
		}
		catch (CdpError)
		{
		}
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.AllowAutoRedirect = false;
			httpWebRequest.Timeout = 15000;
			httpWebRequest.ReadWriteTimeout = 15000;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TTC/2.9.13";
			using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			if (httpWebResponse.StatusCode != HttpStatusCode.OK)
			{
				throw new Exception("Discord ответил HTTP " + (int)httpWebResponse.StatusCode);
			}
			clock.Learn(httpWebResponse.Headers["Date"]);
			using Stream stream = httpWebResponse.GetResponseStream();
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			return memoryStream.ToArray();
		}
		catch (WebException ex)
		{
			throw new Exception((ex.Response is HttpWebResponse httpWebResponse2) ? ("Discord ответил HTTP " + (int)httpWebResponse2.StatusCode) : (text ?? ("картинка не скачалась: " + ex.Message)));
		}
	}

	private Check Dispatch(CdpPage page, string channel, Job job, string[] last)
	{
		if (Send(page, channel, job.Text, last) && job.Check)
		{
			Check check = new Check();
			check.Text = job.Text;
			check.Code = Squash(LastLine(job.Text));
			check.Since = Now();
			check.Picture = ((last != null) ? MessageId(last[0]) : "");
			check.PictureRow = ((last != null) ? last[0] : null);
			return check;
		}
		return null;
	}

	private bool Send(CdpPage page, string channel, string text, string[] last)
	{
		State("sending", "Отправляю сообщение в канал…");
		bool flag;
		string row;
		try
		{
			flag = Post(page, channel, text, out row);
		}
		catch (Exception ex)
		{
			if (ex is CdpClosed || page.Closed)
			{
				throw new CdpClosed();
			}
			Fire(new VzzEvent
			{
				Kind = "unsent",
				Text = Short(ex)
			});
			return false;
		}
		if (!flag)
		{
			Fire(new VzzEvent
			{
				Kind = "unconfirmed",
				Text = ""
			});
			return true;
		}
		VzzEvent vzzEvent = new VzzEvent();
		vzzEvent.Kind = "sent";
		VzzEvent vzzEvent2 = vzzEvent;
		if (row != null && last != null && text.Split('\n').Length >= 3)
		{
			vzzEvent2.After = Posted(row) - Posted(last[0]);
			vzzEvent2.Place = Place(page, channel, text);
		}
		Fire(vzzEvent2);
		return true;
	}

	private int Place(CdpPage page, string channel, string text)
	{
		try
		{
			List<Dictionary<string, object>> list = Rows(page, channel);
			int num = Mine(list, text);
			if (num < 0)
			{
				return 0;
			}
			string text2 = Str(list[num], "author");
			string ours = Squash(LastLine(text));
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < num; i++)
			{
				string text3 = Str(list[i], "author");
				if (!string.IsNullOrEmpty(text3) && text3 != text2 && Answer(Str(list[i], "text"), ours) != null)
				{
					hashSet.Add(text3);
				}
			}
			return hashSet.Count + 1;
		}
		catch (CdpClosed)
		{
			throw;
		}
		catch
		{
			return 0;
		}
	}

	private bool Post(CdpPage page, string channel, string text, out string row)
	{
		row = null;
		string text2 = Squash(LastLine(text));
		double[] array = Point(page.Eval("sel => {\r\n const el = document.querySelector(sel);\r\n if (!el) return null;\r\n el.scrollIntoView({block: 'nearest', inline: 'nearest'});\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}", "form [role=\"textbox\"][contenteditable=\"true\"]", 5000));
		if (array == null)
		{
			throw new Exception("не нашёл поле для сообщения в канале");
		}
		page.Click(array[0], array[1], "left", 0);
		if (!Pasted(page, text2, delegate
		{
			page.Eval("([sel, text]) => {\r\n const box = document.querySelector(sel);\r\n if (!box) return false;\r\n const data = new DataTransfer();\r\n data.setData('text/plain', text);\r\n box.focus();\r\n box.dispatchEvent(new ClipboardEvent('paste', {clipboardData: data, bubbles: true, cancelable: true}));\r\n return true;\r\n}", new object[2] { "form [role=\"textbox\"][contenteditable=\"true\"]", text }, 5000);
		}) && !Pasted(page, text2, delegate
		{
			TypeLines(page, text);
		}))
		{
			throw new Exception("сообщение не вставилось в поле чата");
		}
		Thread.Sleep(60);
		object[] known = (page.Eval("channel => Array.from(document.querySelectorAll('[id^=\"chat-messages-' + channel + '-\"]'), row => row.id)", channel, 5000) as object[]) ?? new object[0];
		page.Enter(shift: false);
		for (int i = 0; i < 40; i++)
		{
			Thread.Sleep(50);
			if (!(page.Eval("sel => { const b = document.querySelector(sel); return b ? b.innerText : null; }", "form [role=\"textbox\"][contenteditable=\"true\"]", 5000) is string text3) || !Squash(text3).Contains(text2))
			{
				return Delivered(page, channel, text, known, out row);
			}
		}
		throw new Exception("Discord не отправил сообщение, оно осталось в поле чата");
	}

	private static bool Pasted(CdpPage page, string code, Action action)
	{
		page.SelectAll();
		page.Delete();
		action();
		for (int i = 0; i < 20; i++)
		{
			if (page.Eval("sel => { const b = document.querySelector(sel); return b ? b.innerText : null; }", "form [role=\"textbox\"][contenteditable=\"true\"]", 5000) is string text && Squash(text).EndsWith(code))
			{
				return true;
			}
			Thread.Sleep(25);
		}
		return false;
	}

	private static void TypeLines(CdpPage page, string text)
	{
		string[] array = text.Replace("\r", "").Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			if (i > 0)
			{
				page.Enter(shift: true);
			}
			if (array[i].Length > 0)
			{
				page.InsertText(array[i]);
			}
		}
	}

	private bool Delivered(CdpPage page, string channel, string text, object[] known, out string row)
	{
		row = null;
		string text2 = Norm(text);
		DateTime dateTime = DateTime.UtcNow.AddSeconds(10.0);
		while (true)
		{
			List<Dictionary<string, object>> list;
			try
			{
				list = Dicts(page.Eval("([channel, known]) => {\r\n const prefix = 'chat-messages-' + channel + '-';\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n if (!list) return [];\r\n const cls = el => (el && el.getAttribute('class')) || '';\r\n const out = [];\r\n for (const row of list.querySelectorAll('[id^=\"' + prefix + '\"]')) {\r\n   if (known.includes(row.id)) continue;\r\n   const id = row.id.slice(prefix.length);\r\n   const parts = row.querySelectorAll('[id=\"message-content-' + id + '\"]');\r\n   const content = parts.length ? parts[parts.length - 1] : null;\r\n   const text = content ? content.innerText : '';\r\n   let opacity = 1;\r\n   for (let el = content; el; el = el.parentElement) {\r\n     const o = parseFloat(getComputedStyle(el).opacity);\r\n     if (!isNaN(o)) opacity *= o;\r\n     if (el === row) break;\r\n   }\r\n   const rest = text ? row.innerText.replace(text, '') : row.innerText;\r\n   const notice = /ephemeral/i.test(cls(row)) || !!row.querySelector('[class*=\"ephemeral\"]')\r\n     || /only you can see this|видите только вы|только вы видите|видно только вам/i.test(rest);\r\n   out.push({\r\n     row: row.id, text,\r\n     sending: !!content && (/isSending/.test(cls(content)) || opacity < 0.9),\r\n     failed: !!content && /isFailed/.test(cls(content)),\r\n     notice: notice ? (text || row.innerText).trim() : '',\r\n   });\r\n }\r\n return out;\r\n}", new object[2] { channel, known }, 5000));
			}
			catch (CdpClosed)
			{
				throw;
			}
			catch (CdpError)
			{
				list = new List<Dictionary<string, object>>();
			}
			int num = -1;
			for (int num2 = list.Count - 1; num2 >= 0; num2--)
			{
				if (Norm(Str(list[num2], "text")) == text2)
				{
					num = num2;
					break;
				}
			}
			string text3 = "";
			for (int i = ((num >= 0) ? (num + 1) : 0); i < list.Count; i++)
			{
				if (!string.IsNullOrEmpty(Str(list[i], "notice")))
				{
					text3 = Str(list[i], "notice");
					break;
				}
			}
			if (text3.Length > 0)
			{
				string text4 = Regex.Replace(text3, "\\s+", " ");
				throw new Exception("Discord не доставил сообщение и написал: " + ((text4.Length > 200) ? text4.Substring(0, 200) : text4));
			}
			if (num >= 0)
			{
				if (Bool(list[num], "failed"))
				{
					throw new Exception("Discord не доставил сообщение, в чате оно красное");
				}
				if (!Bool(list[num], "sending"))
				{
					row = Str(list[num], "row");
					return true;
				}
			}
			if (DateTime.UtcNow > dateTime)
			{
				break;
			}
			Thread.Sleep(100);
		}
		return false;
	}

	private List<Dictionary<string, object>> Rows(CdpPage page, string channel)
	{
		return Dicts(page.Eval("([channel, hosts]) => {\r\n const prefix = 'chat-messages-' + channel + '-';\r\n const list = document.querySelector('[data-list-id=\"chat-messages\"]');\r\n if (!list) return [];\r\n const out = [];\r\n let author = '';\r\n for (const row of list.querySelectorAll('[id^=\"' + prefix + '\"]')) {\r\n   const id = row.id.slice(prefix.length);\r\n   const names = row.querySelectorAll('[id^=\"message-username-\"]');\r\n   if (names.length) author = names[names.length - 1].textContent.trim();\r\n   const picture = Array.from(row.querySelectorAll('img')).some(img => {\r\n     const url = new URL(img.currentSrc || img.src, location.href);\r\n     return hosts.includes(url.hostname) && url.pathname.startsWith('/attachments/' + channel + '/');\r\n   });\r\n   if (picture) out.length = 0;\r\n   const parts = row.querySelectorAll('[id=\"message-content-' + id + '\"]');\r\n   out.push({row: row.id, author, text: parts.length ? parts[parts.length - 1].innerText : ''});\r\n }\r\n return out;\r\n}", new object[2] { channel, CdnHosts }, 5000));
	}

	private Check Verify(CdpPage page, string channel, Check check)
	{
		List<Dictionary<string, object>> list;
		try
		{
			list = Rows(page, channel);
		}
		catch (CdpClosed)
		{
			throw;
		}
		catch (CdpError)
		{
			return check;
		}
		double num = Now() - check.Since;
		int num2 = Mine(list, check.Text);
		if (num2 < 0)
		{
			if (num < 15.0)
			{
				return check;
			}
			Fire(new VzzEvent
			{
				Kind = "unchecked",
				Text = "не нашёл наше сообщение в канале"
			});
			return null;
		}
		List<string> list2 = Answers(list, num2, check.Code);
		string code = null;
		int votes = 0;
		int num3 = 0;
		for (num3 = ANSWERS[0]; num3 <= list2.Count && !Decide(list2.Take(num3).ToList(), out code, out votes); num3++)
		{
		}
		if (code == null)
		{
			if (list2.Count < ANSWERS[1] && num < 60.0)
			{
				return check;
			}
			Fire(new VzzEvent
			{
				Kind = "unchecked",
				Text = ((list2.Count < ANSWERS[0]) ? "после нашего сообщения меньше двух ответов с кодом" : ("ответы после нашего разошлись: " + string.Join(", ", list2.ToArray())))
			});
			return null;
		}
		VzzOcr.Verdict(check.Picture, code, votes, num3);
		if (code == check.Code)
		{
			Fire(new VzzEvent
			{
				Kind = "checked",
				Votes = votes,
				Total = num3,
				Code = code
			});
		}
		else
		{
			Fix(page, channel, Str(list[num2], "row"), check.Text, code, votes, num3);
		}
		return null;
	}

	private void Fix(CdpPage page, string channel, string rowId, string text, string fresh, int votes, int total)
	{
		string[] array = text.Replace("\r", "").Split('\n');
		string text2 = Squash(array[array.Length - 1]);
		State("sending", "Исправляю код " + text2 + " на " + fresh + ": удаляю наше сообщение…");
		try
		{
			Delete(page, rowId);
		}
		catch (Exception ex)
		{
			if (ex is CdpClosed || page.Closed)
			{
				throw new CdpClosed();
			}
			Fire(new VzzEvent
			{
				Kind = "unfixed",
				Old = text2,
				New = fresh,
				Votes = votes,
				Total = total,
				Text = "наше сообщение не удалилось: " + Short(ex)
			});
			return;
		}
		State("sending", "Отправляю сообщение с кодом " + fresh + "…");
		array[array.Length - 1] = fresh;
		string text3 = string.Join("\n", array);
		bool flag;
		try
		{
			flag = Post(page, channel, text3, out var _);
		}
		catch (Exception ex2)
		{
			if (ex2 is CdpClosed || page.Closed)
			{
				throw new CdpClosed();
			}
			Fire(new VzzEvent
			{
				Kind = "unfixed",
				Old = text2,
				New = fresh,
				Votes = votes,
				Total = total,
				Text = "старое удалено, а новое не отправилось: " + Short(ex2)
			});
			return;
		}
		Fire(new VzzEvent
		{
			Kind = "fixed",
			Old = text2,
			New = fresh,
			Votes = votes,
			Total = total,
			Message = text3
		});
		if (!flag)
		{
			Fire(new VzzEvent
			{
				Kind = "unconfirmed",
				Text = "Исправляю код " + text2 + " на " + fresh + ". "
			});
		}
	}

	private static void Delete(CdpPage page, string rowId)
	{
		string text = MessageId(rowId);
		try
		{
			double[] array = Point(page.Eval("([rowId, mid]) => {\r\n const row = document.getElementById(rowId);\r\n if (!row) return null;\r\n const parts = row.querySelectorAll('[id=\"message-content-' + mid + '\"]');\r\n const el = parts[parts.length - 1];\r\n if (!el) return null;\r\n el.scrollIntoView({block: 'center'});\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + Math.min(r.width / 2, 40), r.top + r.height / 2] : null;\r\n}", new object[2] { rowId, text }, 5000));
			if (array == null)
			{
				throw new Exception("не нашёл наше сообщение в канале");
			}
			page.Click(array[0], array[1], "right", 0);
			double[] array2 = null;
			for (int i = 0; i < 30; i++)
			{
				if (array2 != null)
				{
					break;
				}
				Thread.Sleep(100);
				array2 = Point(page.Eval("() => {\r\n let el = document.querySelector('#message-delete, #message-actions-delete');\r\n if (!el) el = Array.from(document.querySelectorAll('[role=\"menuitem\"]')).find(e => /удалить сообщение|delete message/i.test(e.textContent || ''));\r\n if (!el) return null;\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}", null, 5000));
			}
			if (array2 == null)
			{
				throw new Exception("не открылось меню сообщения с пунктом «Удалить сообщение»");
			}
			page.Click(array2[0], array2[1], "left", 8);
			bool flag = false;
			for (int j = 0; j < 50; j++)
			{
				Thread.Sleep(100);
				object obj = page.Eval("id => !!document.getElementById(id)", rowId, 5000);
				if (!(obj is bool) || !(bool)obj)
				{
					return;
				}
				if (!flag)
				{
					double[] array3 = Point(page.Eval("() => {\r\n const el = Array.from(document.querySelectorAll('[role=\"dialog\"] button, [role=\"alertdialog\"] button')).find(e => /удалить|delete/i.test(e.textContent || ''));\r\n if (!el) return null;\r\n const r = el.getBoundingClientRect();\r\n return r.width && r.height ? [r.left + r.width / 2, r.top + r.height / 2] : null;\r\n}", null, 5000));
					if (array3 != null)
					{
						page.Click(array3[0], array3[1], "left", 0);
						flag = true;
					}
				}
			}
			throw new Exception("Discord не удалил сообщение");
		}
		catch
		{
			if (!page.Closed)
			{
				try
				{
					page.Escape();
				}
				catch
				{
				}
			}
			throw;
		}
	}

	private static string Squash(string text)
	{
		return Regex.Replace(text ?? "", "\\s+", "");
	}

	private static string Norm(string text)
	{
		return Regex.Replace(text ?? "", "[\\W_]+", "");
	}

	private static string LastLine(string text)
	{
		string[] array = (text ?? "").Replace("\r", "").Split('\n');
		return array[array.Length - 1];
	}

	private static int Distance(string a, string b)
	{
		int[] array = Enumerable.Range(0, b.Length + 1).ToArray();
		for (int i = 1; i <= a.Length; i++)
		{
			int num = array[0];
			array[0] = i;
			for (int j = 1; j <= b.Length; j++)
			{
				int num2 = array[j];
				array[j] = Math.Min(Math.Min(array[j] + 1, array[j - 1] + 1), num + ((a[i - 1] != b[j - 1]) ? 1 : 0));
				num = num2;
			}
		}
		return array[b.Length];
	}

	private static string Answer(string text, string ours)
	{
		int low = Math.Max(2, ours.Length - 2);
		int high = ours.Length + 2;
		List<string> list = (from l in (text ?? "").Replace("\r", "").Split('\n')
			where l.Trim().Length > 0
			select Squash(l).ToUpperInvariant()).ToList();
		if (list.Count > 0)
		{
			string text2 = list[list.Count - 1];
			Match match = CODE.Match(text2);
			if (match.Success && match.Length == text2.Length && text2.Length >= low && text2.Length <= high)
			{
				return text2;
			}
		}
		string result = null;
		int num = int.MaxValue;
		List<string> list2 = (from Match x in CODE.Matches(text ?? "")
			select x.Value.ToUpperInvariant() into w
			where w.Length >= low && w.Length <= high
			select w).ToList();
		for (int num2 = list2.Count - 1; num2 >= 0; num2--)
		{
			int num3 = Distance(list2[num2], ours);
			if (num3 < num)
			{
				num = num3;
				result = list2[num2];
			}
		}
		return result;
	}

	private static bool Decide(List<string> codes, out string code, out int votes)
	{
		code = null;
		votes = 0;
		List<KeyValuePair<string, int>> list = (from c in codes
			group c by c into g
			select new KeyValuePair<string, int>(g.Key, g.Count()) into p
			orderby p.Value descending
			select p).ToList();
		if (list.Count > 0 && list[0].Value >= 2 && (list.Count == 1 || list[1].Value < list[0].Value))
		{
			code = list[0].Key;
			votes = list[0].Value;
			return true;
		}
		return false;
	}

	private static int Mine(List<Dictionary<string, object>> rows, string text)
	{
		string text2 = Norm(text);
		for (int num = rows.Count - 1; num >= 0; num--)
		{
			if (Norm(Str(rows[num], "text")) == text2)
			{
				return num;
			}
		}
		return -1;
	}

	private static List<string> Answers(List<Dictionary<string, object>> rows, int at, string ours)
	{
		string text = Str(rows[at], "author");
		HashSet<string> hashSet = new HashSet<string>();
		List<string> list = new List<string>();
		for (int i = at + 1; i < rows.Count; i++)
		{
			string text2 = Str(rows[i], "author");
			if (string.IsNullOrEmpty(text2) || text2 == text || hashSet.Contains(text2))
			{
				continue;
			}
			string text3 = Answer(Str(rows[i], "text"), ours);
			if (text3 != null)
			{
				hashSet.Add(text2);
				list.Add(text3);
				if (list.Count == ANSWERS[1])
				{
					break;
				}
			}
		}
		return list;
	}

	public static double Posted(string rowId)
	{
		if (rowId == null || !long.TryParse(MessageId(rowId), out var result))
		{
			return 0.0;
		}
		return (double)(result >> 22) / 1000.0 + 1420070400.0;
	}

	private static string MessageId(string rowId)
	{
		return rowId.Substring(rowId.LastIndexOf('-') + 1);
	}

	public static string PathOf(string url)
	{
		if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var result))
		{
			return "";
		}
		return result.AbsolutePath.TrimEnd('/');
	}

	private static double[] Point(object v)
	{
		if (!(v is object[] array) || array.Length < 2)
		{
			return null;
		}
		return new double[2]
		{
			Convert.ToDouble(array[0], CultureInfo.InvariantCulture),
			Convert.ToDouble(array[1], CultureInfo.InvariantCulture)
		};
	}

	private static List<Dictionary<string, object>> Dicts(object v)
	{
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		if (v is object[] array)
		{
			object[] array2 = array;
			foreach (object obj in array2)
			{
				if (obj is Dictionary<string, object> item)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	private static bool Bool(Dictionary<string, object> d, string k)
	{
		if (d != null && d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	public static string Short(Exception ex)
	{
		string text = ex.Message ?? "";
		int num = text.IndexOf("Call log:");
		if (num >= 0)
		{
			text = text.Substring(0, num);
		}
		text = text.Trim();
		if (text.Length <= 280)
		{
			return text;
		}
		return text.Substring(0, 280);
	}
}
