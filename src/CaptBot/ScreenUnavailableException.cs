using System;

namespace CaptBot;

internal class ScreenUnavailableException : Exception
{
	public ScreenUnavailableException()
		: base("Windows не дала снять экран (окно игры меняет режим, открыт запрос администратора или экран заблокирован)")
	{
	}
}
