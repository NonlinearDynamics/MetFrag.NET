using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MetFragNET.Fragmentation;
using MetFragNET.Spectra;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Algorithm
{
	public class FragmentGenerator
	{
		private readonly FragmentationConfig config;
		private readonly Spectrum spectrum;

		public FragmentGenerator(Spectrum spectrum, FragmentationConfig config)
		{
			this.spectrum = spectrum;
			this.config = config;
		}

		public IEnumerable<IAtomContainer> GenerateFragments(IAtomContainer compound, string compoundId, CancellationToken isCancelled)
		{
			var fragmenter = new Fragmenter(spectrum.Peaks.ToList(), config);

			// This might throw an OutOfMemoryException, but I want it to be thrown not silently fail
			return fragmenter.GenerateFragmentsEfficient(compound, true, config.TreeDepth, compoundId, isCancelled);
		}
	}
}