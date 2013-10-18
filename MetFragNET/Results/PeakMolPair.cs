using MetFragNET.Spectra;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Results
{
	public class PeakMolPair
	{
		private readonly IAtomContainer ac;
		private readonly Peak peak;

		public PeakMolPair(IAtomContainer ac, Peak peak, double matchedMass, string molecularFormula, double hydrogenPenalty, double bondDissociationEnergy, string neutralChange)
		{
			this.ac = ac;
			this.peak = peak;

			MatchedMass = matchedMass;
			MolecularFormula = molecularFormula;
			HydrogenPenalty = hydrogenPenalty;
			BondDissociationEnergy = bondDissociationEnergy;
			NeutralChange = neutralChange;
		}

		public IAtomContainer Fragment
		{
			get { return ac; }
		}

		public Peak Peak
		{
			get { return peak; }
		}

		public double MatchedMass { get; private set; }
		public string MolecularFormula { get; private set; }
		public double HydrogenPenalty { get; private set; }
		public double BondDissociationEnergy { get; private set; }
		public string NeutralChange { get; private set; }
	}
}