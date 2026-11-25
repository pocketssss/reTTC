using System;

namespace CaptBot;

internal class CdpError : Exception
{
	public CdpError(string m)
		: base(m)
	{
	}
}
