using System.Collections.Generic;
using System.Linq;

namespace MetFragNET.Spectra
{
	public class CleanUpPeakList
	{
		private readonly IEnumerable<Peak> peakList;

		public CleanUpPeakList(IEnumerable<Peak> peakList)
		{
			this.peakList = peakList;
		}

		public IEnumerable<Peak> GetCleanedPeakList(double mass)
		{
			return peakList.Where(p => p.Mass < mass);
		}
	}
}