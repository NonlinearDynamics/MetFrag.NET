using System;
using org.openscience.cdk.interfaces;

namespace NldMetFrag_DotNet.Fragmentation
{
	public class BondPair : IEquatable<BondPair>
	{
		private readonly IBond bond1;
		private readonly IBond bond2;

		public BondPair(IBond bond1, IBond bond2)
		{
			this.bond1 = bond1;
			this.bond2 = bond2;
		}

		public bool Equals(BondPair other)
		{
			return (bond1 == other.bond1 && bond2 == other.bond2) || (bond1 == other.bond2 && bond2 == other.bond1);
		}
	}
}