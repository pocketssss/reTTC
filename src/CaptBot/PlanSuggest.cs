using System.Collections.Generic;

namespace CaptBot;

internal class PlanSuggest
{
	public int People;

	public int MedianN;

	public PlanOption Main;

	public List<PlanOption> Options = new List<PlanOption>();

	public List<string> Notes = new List<string>();
}
