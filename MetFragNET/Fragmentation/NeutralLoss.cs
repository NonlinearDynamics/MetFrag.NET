using org.openscience.cdk.interfaces;

namespace MetFragNET.Fragmentation
{
	public class NeutralLoss
	{
		private readonly string atomToStart;
		private readonly int distance;
		private readonly IMolecularFormula elementalComposition;
		private readonly int hydrogenDifference;
		private readonly int hydrogenOnStartAtom;
		private readonly int mode;
		private readonly IMolecularFormula topoFragment;

		public NeutralLoss(IMolecularFormula elementalComposition, IMolecularFormula topoFragment, int mode, int hydrogenDifference, int distance, string atomToStart, int hydrogenOnStartAtom)
		{
			this.elementalComposition = elementalComposition;
			this.mode = mode;

			this.topoFragment = topoFragment;
			this.hydrogenDifference = hydrogenDifference;
			this.distance = distance;
			this.atomToStart = atomToStart;
			this.hydrogenOnStartAtom = hydrogenOnStartAtom;
		}

		public IMolecularFormula ElementalComposition
		{
			get { return elementalComposition; }
		}

		public int Mode
		{
			get { return mode; }
		}

		public int HydrogenDifference
		{
			get { return hydrogenDifference; }
		}

		public IMolecularFormula TopoFragment
		{
			get { return topoFragment; }
		}

		public int Distance
		{
			get { return distance; }
		}

		public string AtomToStart
		{
			get { return atomToStart; }
		}

		public int HydrogenOnStartAtom
		{
			get { return hydrogenOnStartAtom; }
		}
	}
}