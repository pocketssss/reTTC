using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace CaptBot;

internal class PlanEntry
{
	[ScriptIgnore]
	public string State;

	[ScriptIgnore]
	public string Status;

	[ScriptIgnore]
	public DateTime FireUtc;

	[ScriptIgnore]
	public string CaptNow;

	[ScriptIgnore]
	public string Target;

	[ScriptIgnore]
	public int Attempt;

	[ScriptIgnore]
	public List<string> Tried = new List<string>();

	[ScriptIgnore]
	public bool Retry;

	[ScriptIgnore]
	public bool Blocked;

	[ScriptIgnore]
	public string Confirm;

	[ScriptIgnore]
	public string ConfirmDetail;

	[ScriptIgnore]
	public DateTime EndedUtc;

	public string Square { get; set; }

	public string Capt { get; set; }

	public string Fire { get; set; }
}
