using java.lang;
using org.openscience.cdk.atomtype;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.tools;
using org.openscience.cdk.tools.manipulator;

namespace NldMetFrag_DotNet.Algorithm
{
	public class ImplicitHydrogenAdder
	{
		public bool AddImplicitHydrogens(IAtomContainer molecule)
		{
			try
			{
				var builder = molecule.getBuilder();
				var matcher = CDKAtomTypeMatcher.getInstance(builder);

				foreach (var atom in molecule.atoms().ToWindowsEnumerable<IAtom>())
				{
					var type = matcher.findMatchingAtomType(molecule, atom);
					AtomTypeManipulator.configure(atom, type);
				}
				var hAdder = CDKHydrogenAdder.getInstance(builder);
				hAdder.addImplicitHydrogens(molecule);

				AtomContainerManipulator.convertImplicitToExplicitHydrogens(molecule);
			}
				//there is a bug in cdk?? error happens when there is a S or Ti in the molecule
			catch (IllegalArgumentException)
			{
				return false;
			}

			return true;
		}
	}
}