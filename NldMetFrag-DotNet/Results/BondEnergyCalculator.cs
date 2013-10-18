using System.Linq;
using NldMetFrag_DotNet.Fragmentation;
using org.openscience.cdk.interfaces;

namespace NldMetFrag_DotNet.Results
{
	public class BondEnergyCalculator
	{
		private readonly IAtomContainer molecule;

		public BondEnergyCalculator(IAtomContainer molecule)
		{
			this.molecule = molecule;			
		}

		public double TotalBondEnergy()
		{
			return molecule.bonds().ToWindowsEnumerable<IBond>().Sum(bond => BondEnergies.Lookup(bond));
		}
	}
}