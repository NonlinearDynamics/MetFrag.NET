﻿using System.Collections.Generic;
using System.Linq;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Fragmentation
{
	public class BondEnergies
	{
		private static readonly Dictionary<string, double> energies = new Dictionary<string, double>
		{
			{"H-H", 436},
			{"H-C", 412},
			{"H-Si", 318},
			{"H-N", 388},
			{"H-P", 322},
			{"H-O", 463},
			{"H-S", 338},
			{"H-F", 562},
			{"H-Cl", 431},
			{"H-Br", 366},
			{"H-I", 299},
			{"H-B", 389},
			{"H-Ge", 288},
			{"H-Sn", 251},
			{"H-As", 247},
			{"H-Se", 276},
			{"H-T", 238},
			{"C-C", 348},
			{"C=C", 612},
			{"C~C", 837},
			{"C-O", 360},
			{"C=O", 743},
			{"C-N", 305},
			{"C=N", 613},
			{"C~N", 890},
			{"C-F", 484},
			{"C-Cl", 338},
			{"C-Br", 276},
			{"C-I", 238},
			{"C-S", 272},
			{"C=S", 573},
			{"C-Si", 318},
			{"C-Ge", 238},
			{"C-Sn", 192},
			{"C-Pb", 130},
			{"C-P", 264},
			{"C-B", 356},
			{"P-P", 201},
			{"P-O", 335},
			{"P=O", 544},
			{"P=S", 335},
			{"P-F", 490},
			{"P-Cl", 326},
			{"P-Br", 264},
			{"P-I", 184},
			{"F-Cl", 313},
			{"Si-Si", 176},
			{"N-N", 163},
			{"N=N", 409},
			{"N~N", 944},
			{"O-O", 146},
			{"O=O", 496},
			{"F-F", 158},
			{"Cl-Cl", 242},
			{"Br-Br", 193},
			{"I-I", 151},
			{"At-At", 116},
			{"Se-Se", 172},
			{"I-O", 201},
			{"I-F", 273},
			{"I-Cl", 208},
			{"I-Br", 175},
			{"B-B", 293},
			{"B-O", 536},
			{"B-F", 613},
			{"B-Cl", 456},
			{"B-Br", 377},
			{"S-Cl", 255},
			{"S-F", 284},
			{"S=S", 425},
			{"S=O", 522},
			{"N=O", 607},
			{"N-O", 222},
			{"S-S", 226},
			{"F-N", 272},
			{"F-O", 184},
			{"F-S", 226}
		};

		public static double Lookup(IBond bond)
		{
			var atoms = bond.atoms().ToWindowsEnumerable<IAtom>().ToList();

			return Lookup(atoms.First().getSymbol(), atoms.Last().getSymbol(), bond.getOrder().toString());
		}

		private static double Lookup(string atom1, string atom2, string order)
		{
			var inOrderBondDesc = BondDescription(atom1, atom2, order);

			var bondEnergy = 0.0;
			if (energies.TryGetValue(inOrderBondDesc, out bondEnergy))
			{
				return bondEnergy;
			}

			var reversedBondDesc = BondDescription(atom2, atom1, order);
			if (energies.TryGetValue(reversedBondDesc, out bondEnergy))
			{
				return bondEnergy;
			}

			//not a covalent bond? just assume a C-C bond TODO!
			return 348.0;
		}

		private static string BondDescription(string atom1, string atom2, string order)
		{
			//now check the bond order up to 3
			// - --> single bond
			// = --> double bond
			// ~ --> triple bond
			string joiner;
			switch (order)
			{
				case "SINGLE" :
					joiner = "-";
					break;
				case "DOUBLE":
					joiner = "=";
					break;
				case "TRIPLE":
					joiner = "~";
					break;
				default:
					return null;
			}

			return string.Format("{0}{1}{2}", atom1, joiner, atom2);
		}
	}
}