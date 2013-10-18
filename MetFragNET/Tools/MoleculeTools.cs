using System.Collections.Generic;
using System.Globalization;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Tools
{
	public class MoleculeTools
	{
		public static IAtomContainer MoleculeNumbering(IAtomContainer mol)
		{
			var count = 0;
			var countBond = 0;
			var alreadyDone = new List<IAtom>();

			foreach (var bond in mol.bonds().ToWindowsEnumerable<IBond>())
			{
				bond.setID(countBond.ToString(CultureInfo.InvariantCulture));
				countBond++;

				foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>())
				{
					if (!alreadyDone.Contains(atom))
					{
						atom.setID(count.ToString(CultureInfo.InvariantCulture));
						count++;
						alreadyDone.Add(atom);
					}
				}
			}

			return mol;
		}
	}
}