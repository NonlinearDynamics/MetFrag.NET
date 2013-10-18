using System.Collections.Generic;
using java.io;
using org.openscience.cdk;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.io;
using org.openscience.cdk.tools.manipulator;

namespace NldMetFrag_DotNet.Algorithm
{
	public class SdfParser
	{
		public IEnumerable<IAtomContainer> Parse(string sdfString)
		{
			var reader = new MDLV2000Reader(new StringReader(sdfString));
			var chemFile = (ChemFile)reader.read(new ChemFile());
			return ChemFileManipulator.getAllAtomContainers(chemFile).ToWindowsEnumerable<IAtomContainer>();
		}
	}
}