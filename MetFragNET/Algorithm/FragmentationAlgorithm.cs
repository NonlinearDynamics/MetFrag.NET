using System.Threading;
using org.openscience.cdk.interfaces;

namespace NldMetFrag_DotNet.Algorithm
{
	public class FragmentationAlgorithm
	{
		private readonly FragmentGenerator fragmentGenerator;
		private readonly HitsMatcher hitsMatcher;
		private readonly ImplicitHydrogenAdder hydrogenAdder;

		public FragmentationAlgorithm(ImplicitHydrogenAdder hydrogenAdder, FragmentGenerator fragmentGenerator, HitsMatcher hitsMatcher)
		{
			this.hydrogenAdder = hydrogenAdder;
			this.fragmentGenerator = fragmentGenerator;
			this.hitsMatcher = hitsMatcher;
		}

		public ResultRow GenerateFragments(IAtomContainer compound, string compoundId, CancellationToken isCancelled)
		{
			if (!hydrogenAdder.AddImplicitHydrogens(compound))
			{
				return null;
			}

			var fragments = fragmentGenerator.GenerateFragments(compound, compoundId, isCancelled);
			return hitsMatcher.GetResultRow(compound, fragments, compoundId);
		}
	}
}