using System.Collections.Generic;
using System.Linq;
using NldMetFrag_DotNet.Results;
using org.openscience.cdk.interfaces;

namespace NldMetFrag_DotNet
{
	public class ResultRow
	{
		private readonly IEnumerable<PeakMolPair> fragmentPics;
		private readonly string id;
		private readonly double bondEnergy;

		public ResultRow(
			string id,
			IAtomContainer originalCompound,
			IEnumerable<PeakMolPair> fragmentPics)
		{
			this.id = id;
			this.fragmentPics = fragmentPics;
			bondEnergy = new BondEnergyCalculator(originalCompound).TotalBondEnergy();
		}

		public string ID
		{
			get { return id; }
		}

		public IEnumerable<PeakMolPair> FragmentPics
		{
			get { return fragmentPics; }
		}

		public double BondEnergyOfUnfragmentedMolecule
		{
			get { return bondEnergy; }
		}
	}
}