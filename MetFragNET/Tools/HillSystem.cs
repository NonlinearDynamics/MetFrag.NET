using System.Linq;
using org.openscience.cdk;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Tools
{
	public static class HillSystem
	{
		public static readonly string[] ElementsWithCarbons = new[]
			{
				"C", "H",
				"Ac", "Ag", "Al", "Am", "Ar", "As", "At", "Au",
				"B", "Ba", "Be", "Bh", "Bi", "Bk", "Br",
				"Ca", "Cd", "Ce", "Cf", "Cl", "Cm", "Cn", "Co", "Cr", "Cs", "Cu",
				"Db", "Ds", "Dy",
				"Er", "Es", "Eu",
				"F", "Fe", "Fm", "Fr",
				"Ga", "Gd", "Ge",
				"He", "Hf", "Hg", "Ho", "Hs",
				"I", "In", "Ir",
				"K", "Kr",
				"La", "Li", "Lr", "Lu",
				"Md", "Mg", "Mn", "Mo", "Mt",
				"N", "Na", "Nb", "Nd", "Ne", "Ni", "No", "Np",
				"O", "Os",
				"P", "Pa", "Pb", "Pd", "Pm", "Po", "Pr", "Pt", "Pu",
				"Ra", "Rb", "Re", "Rf", "Rg", "Rh", "Rn", "Ru",
				"S", "Sb", "Sc", "Se", "Sg", "Si", "Sm", "Sn", "Sr",
				"Ta", "Tb", "Tc", "Te", "Th", "Ti", "Tl", "Tm",
				"U", "V", "W", "Xe", "Y", "Yb", "Zn", "Zr",
				// The "odd one out": an unspecified R-group
				"R"
			};


		public static readonly string[] ElementsWithoutCarbons = new[]
			{
				"Ac", "Ag", "Al", "Am", "Ar", "As", "At", "Au",
				"B", "Ba", "Be", "Bh", "Bi", "Bk", "Br",
				"C", "Ca", "Cd", "Ce", "Cf", "Cl", "Cm", "Cn", "Co", "Cr", "Cs", "Cu",
				"Db", "Ds", "Dy",
				"Er", "Es", "Eu",
				"F", "Fe", "Fm", "Fr",
				"Ga", "Gd", "Ge",
				"H", "He", "Hf", "Hg", "Ho", "Hs",
				"I", "In", "Ir",
				"K", "Kr",
				"La", "Li", "Lr", "Lu",
				"Md", "Mg", "Mn", "Mo", "Mt",
				"N", "Na", "Nb", "Nd", "Ne", "Ni", "No", "Np",
				"O", "Os",
				"P", "Pa", "Pb", "Pd", "Pm", "Po", "Pr", "Pt", "Pu",
				"Ra", "Rb", "Re", "Rf", "Rg", "Rh", "Rn", "Ru",
				"S", "Sb", "Sc", "Se", "Sg", "Si", "Sm", "Sn", "Sr",
				"Ta", "Tb", "Tc", "Te", "Th", "Ti", "Tl", "Tm",
				"U", "V", "W", "Xe", "Y", "Yb", "Zn", "Zr",
				// The "odd one out": an unspecified R-group
				"R"
			}; 
	}
}