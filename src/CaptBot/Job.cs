using System;

namespace CaptBot;

internal class Job
{
	public string Square;

	public int Col;

	public int Row;

	public int CaptH;

	public int CaptM;

	public int People;

	public DateTime FireUtc;

	public int PrepSec;

	public bool DryRun;

	public bool AutoPing = true;

	public int FineMs;

	public int HoldMs = 40;

	public string Family;

	public bool Watch;

	public int AfterMin = 15;

	public string WatchSquare;

	public string Server;

	public bool Prepare;

	public RosterInfo Roster;

	public bool ListOnly;

	public long RosterFillId;

	public bool SquareFromBot;

	public Action<Job> BeforePrep;

	public Action<string, string> Confirm;

	public bool Cancel;

	public bool TurnWaited;
}
