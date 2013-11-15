using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MetFragNET.Results;
using MetFragNET.Tools;
using org.openscience.cdk.formula;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.tools.manipulator;

namespace MetFragNET.Spectra
{
	public class FragmentPeakAssigner
	{
		private static readonly double hydrogenMass = MolecularFormulaTools.GetMonoisotopicMass("H1");
		private IList<PeakMolPair> hits;
		private IList<PeakMolPair> hitsAll;

		public IEnumerable<PeakMolPair> Hits
		{
			get { return hits; }
		}

		public IEnumerable<PeakMolPair> AllHits
		{
			get { return hitsAll; }
		}

		public void AssignFragmentPeak(IList<IAtomContainer> fragments, IEnumerable<Peak> peakList, double mzabs, double mzppm)
		{
			hits = new List<PeakMolPair>();
			hitsAll = new List<PeakMolPair>();			

			foreach (var peak in peakList)
			{
				var haveFoundAMatch = false;
				foreach (var fragment in fragments)
				{
					//matched peak
					int hydrogensAdded;
					double matchedMass;
					double hydrogenPenalty;
					if (MatchByMass(fragment, peak.Mass, mzabs, mzppm, out matchedMass, out hydrogenPenalty, out hydrogensAdded))
					{
                        var match = new PeakMolPair(fragment, peak, matchedMass, GetMolecularFormulaAsString(fragment), hydrogenPenalty, double.Parse((string)fragment.getProperty("BondEnergy"), CultureInfo.InvariantCulture), GetNeutralChange(fragment, hydrogensAdded));
						hitsAll.Add(match);

						// If we don't yet have a match, add it
						if (!haveFoundAMatch)
						{
							hits.Add(match);
							haveFoundAMatch = true;
						}
						// If we do have a match, replace it if this new match has a lower hydrogen penalty
						else if (hydrogenPenalty < hits.Last().HydrogenPenalty)
						{
							hits.RemoveAt(hits.Count - 1);
							hits.Add(match);
						}						
					}
				}
			}
		}

		private bool MatchByMass(IAtomContainer ac, double peak, double mzabs, double mzppm, out double matchedMass, out double hydrogenPenalty, out int hydrogensAdded)
		{
			matchedMass = 0;
			hydrogenPenalty = 0;
			hydrogensAdded = 0;
			
			double mass;
			//speed up and neutral loss matching!
			if (ac.getProperty("FragmentMass") != null && (string)ac.getProperty("FragmentMass") != "")
			{
                mass = double.Parse(ac.getProperty("FragmentMass").ToString(), CultureInfo.InvariantCulture);
			}
			else
			{
				mass = MolecularFormulaTools.GetMonoisotopicMass(GetMolecularFormula(ac));
			}

			var peakLow = peak - mzabs - PpmTool.GetPPMDeviation(peak, mzppm);
			var peakHigh = peak + mzabs + PpmTool.GetPPMDeviation(peak, mzppm);			

			//now try to add/remove neutral hydrogens ...at most the treedepth
			var treeDepth = int.Parse((String)ac.getProperty("TreeDepth"));
			for (var i = 0; i <= treeDepth; i++)
			{
				var hMass = i * hydrogenMass;

				if ((mass + hMass) >= peakLow && (mass + hMass) <= peakHigh)
				{
					matchedMass = Math.Round(mass + hMass, 4);

					//now add a bond energy equivalent to a H-C bond
					hydrogenPenalty = (i * 1000);

					hydrogensAdded = i;

					return true;
				}

				if ((mass - hMass) >= peakLow && (mass - hMass) <= peakHigh)
				{
					matchedMass = Math.Round(mass - hMass, 4);

					//now add a bond energy equivalent to a H-C bond
					hydrogenPenalty = (i * 1000);

					hydrogensAdded = -i;

					return true;
				}
			}
			return false;
		}

		private static string GetMolecularFormulaAsString(IAtomContainer ac)
		{
			return MolecularFormulaTools.GetString(GetMolecularFormula(ac));
		}

		private static IMolecularFormula GetMolecularFormula(IAtomContainer ac)
		{
			return MolecularFormulaTools.GetMolecularFormula(ac);			
		}

		private static string GetNeutralChange(IChemObject ac, int hydrogensAdded)
		{
			var neutralLoss = "";
			if (ac.getProperty("NlElementalComposition") != null && (string)ac.getProperty("NlElementalComposition") != "")
			{
				neutralLoss = "-" + ac.getProperty("NlElementalComposition") + " ";
			}

			var signString = hydrogensAdded < 0 ? "-" : "+";

			switch (Math.Abs(hydrogensAdded))
			{
				case 0:
					return neutralLoss;
				case 1:
					return string.Format("{0}{1}H", neutralLoss, signString);
				default:
					return string.Format("{0}{1}{2}H", neutralLoss, signString, Math.Abs(hydrogensAdded));
			}
		}		
	}
}