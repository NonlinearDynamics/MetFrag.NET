using System.Collections.Generic;
using System.Linq;
using MetFragNET.Results;
using MetFragNET.Spectra;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Algorithm
{
	public class HitsMatcher
	{
		private readonly FragmentationConfig config;
		private readonly Spectrum spectrum;

		public HitsMatcher(Spectrum spectrum, FragmentationConfig config)
		{
			this.spectrum = spectrum;
			this.config = config;
		}

		public ResultRow GetResultRow(IAtomContainer originalCompound, IEnumerable<IAtomContainer> fragments, string compoundId)
		{
			var assignFragmentHits = AssignHits(fragments);

			var fragsPics = assignFragmentHits.AllHits.Reverse();

			return new ResultRow(compoundId, originalCompound, fragsPics);
		}

		private FragmentPeakAssigner AssignHits(IEnumerable<IAtomContainer> fragments)
		{
			var assignFragmentPeak = new FragmentPeakAssigner();
			assignFragmentPeak.AssignFragmentPeak(fragments.ToList(), CleanPeakList(), config.Mzabs, config.Mzppm);
			return assignFragmentPeak;
		}

		private IEnumerable<Peak> CleanPeakList()
		{
			var cList = new CleanUpPeakList(spectrum.Peaks);
			return cList.GetCleanedPeakList(spectrum.ExactMass);
		}
	}
}