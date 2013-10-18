using System;
using System.Collections.Generic;
using System.Linq;

namespace NldMetFrag_DotNet.Scoring
{
	public class Scorer
	{
		public double Score(IList<Tuple<double, double>> spectrumPeaks, IEnumerable<Tuple<double, double, double>> matchedFragments, double compoundTotalBondEnergy)
		{
			if (!spectrumPeaks.Any())
			{
				return 0;
			}

			var maxSpectrumIntensity = spectrumPeaks.Max(p => p.Item2);

			var matchedTotalWeightedIntensity = matchedFragments.Sum(f => WeightedIntensity(f.Item1, f.Item2, f.Item3, compoundTotalBondEnergy, maxSpectrumIntensity));
			var spectrumTotalWeightedIntensity = spectrumPeaks.Sum(t => WeightedIntensity(t.Item1, t.Item2, 0, compoundTotalBondEnergy, maxSpectrumIntensity));

			var score = spectrumTotalWeightedIntensity > 0 ? matchedTotalWeightedIntensity / spectrumTotalWeightedIntensity * 100 : 0;
			return score;
		}

		/// <summary>
		/// Returns the "weighted intensity" for a peak in the spectrum. This combines mass, intensity and bond energy to produce a weight
		/// for the peak, with higher weighted peaks contributing to the score more.
		/// </summary>
		/// 
		/// <param name="mass">
		/// The mass of the peak, in Daltons. A higher mass leads to a higher weight.
		/// </param>
		/// <param name="intensity">
		/// The intensity of the peak, in arbitrary units. A higher intensity leads to a higher weight.
		/// </param>
		/// <param name="energyRequiredToFormFragment">
		/// The sum of (a) the bond energy of all the bonds that had to be broken to form this
		/// fragment; and (b) the "hydrogen penalty" given by MetFrag, which is a penalty added to the energy dependent on how many extra
		/// hydrogens MetFrag had to add or remove to get the mass of the fragment to match the mass of the peak.
		/// A higher energy leads to a lower weight.
		/// </param>
		/// <param name="bondEnergyOfUnfragmentedMolecule">
		/// The sum of the bond energies of all bonds in the unfragmented molecule. This is used to normalise the energyRequiredToFormFragment.
		/// </param>
		/// <param name="maxSpectrumInensity">
		/// The maximum intensity of any peak in the spectrum. This is used to normalise the intensity.
		/// </param>
		/// <returns>The colour</returns>
		private static double WeightedIntensity(double mass, double intensity, double energyRequiredToFormFragment, double bondEnergyOfUnfragmentedMolecule, double maxSpectrumInensity)
		{
			var proportionOfEnergyLost = Math.Min(1, energyRequiredToFormFragment / bondEnergyOfUnfragmentedMolecule);

			var weightedIntensity = Math.Pow(intensity / maxSpectrumInensity * 999, 0.6) * Math.Pow(mass, 3);

			// If a higher proportion of the energy has been lost, return a lower score.
			return weightedIntensity - (weightedIntensity * proportionOfEnergyLost);
		}
	}
}