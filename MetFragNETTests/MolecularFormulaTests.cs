using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MetFragNET;
using MetFragNETTests.Properties;
using NUnit.Framework;

namespace MetFragNETTests
{
	[TestFixture]
	public class MolecularFormulaTests
	{
		private static string Clean(string htmlFormula)
		{
			return Regex.Replace(htmlFormula, @"<[^>]*>", String.Empty);
		}

		[Test]
		public void MolecularFormulas_AreAsExpected()
		{
			var peaksToUse = @"218.292037963867 4814.0087890625
251.08984375 4636.91015625
255.294479370117 4333.99462890625
262.05908203125 4842.00537109375
263.503967285156 17012.8515625
316.170104980469 9390.609375
330.142669677734 5896.49560546875
336.126068115234 30484.8515625
388.176727294922 98447.6875
420.146850585938 30213.833984375
422.163482666016 35773.4296875
436.082305908203 5405.27880859375
445.210327148438 7284.78369140625
454.136291503906 138159.09375
485.711242675781 5883.47705078125
492.739959716797 4392.4541015625
671.27001953125 52046.59375
673.223999023438 13191.6015625
699.439758300781 7179.86279296875
708.473022460938 5404.1103515625
709.756103515625 4595.73779296875
746.69970703125 6625.22314453125
765.3642578125 12489.841796875
765.697509765625 18657.2578125
787.299255371094 11979.267578125
925.471130371094 5533.615234375
1001.71899414063 5344.462890625
1006.49475097656 43394.28515625
1006.99371337891 32465.154296875
1070.025390625 8782.123046875
1071.01171875 109050.328125
1071.51245117188 75879.2734375
1108.54565429688 21520.17578125
1108.66394042969 7866.89990234375
1109.04162597656 20998.587890625
1136.55224609375 10964.8427734375
1137.05126953125 8957.07421875
1137.54138183594 8071.50927734375
1201.56555175781 5949.6748046875
1216.61267089844 24844.447265625
1217.12524414063 23370.1796875
1217.60144042969 929436.875
1218.10131835938 1008759.75
1218.60095214844 140482.171875
1854.02758789063 11936.041015625
1941.10046386719 6494.98876953125
1942.84094238281 8874.51953125";

			var fragger = new MetFrag(Encoding.Default.GetString(Resources.Digoxin));
			var results = fragger.metFrag(TestConfig.ExactMass, peaksToUse, TestConfig.Mode, TestConfig.MzAbs, TestConfig.MzPpm, CancellationToken.None).ToList();

			Assert.That(results.Count, Is.EqualTo(5));

			// Note (Steve): These now only allow +/- 1 hydrogen (because the tree depth is always 1).

			CollectionAssert.AreEquivalent(results[0].FragmentPics.Select(p => Clean(p.MolecularFormula)), new[] { "C18H27O9", "C18H29O9", "C15H23O8" });
			CollectionAssert.AreEquivalent(results[0].FragmentPics.Select(p => p.NeutralChange), new[] { "+H", "-H", "-H" });

			CollectionAssert.AreEquivalent(results[1].FragmentPics.Select(p => Clean(p.MolecularFormula)), new[] { "C18H27O9", "C18H29O9", "C15H23O8" });
			CollectionAssert.AreEquivalent(results[1].FragmentPics.Select(p => p.NeutralChange), new[] { "+H", "-H", "-H" });

			CollectionAssert.AreEquivalent(results[2].FragmentPics.Select(p => Clean(p.MolecularFormula)), new[] { "C24H30N1O7" });
			CollectionAssert.AreEquivalent(results[2].FragmentPics.Select(p => p.NeutralChange), new[] { "+H" });

			CollectionAssert.AreEquivalent(results[3].FragmentPics.Select(p => Clean(p.MolecularFormula)), new[] { "C21H32O10", "C15H23O8", "C10H14O8" });
			CollectionAssert.AreEquivalent(results[3].FragmentPics.Select(p => p.NeutralChange), new[] { "+H", "-H", "" });

			CollectionAssert.AreEquivalent(results[4].FragmentPics.Select(p => Clean(p.MolecularFormula)), new[] { "C35H57O18", "C35H56O18", "C28H47O18", "C22H29O6" });
			CollectionAssert.AreEquivalent(results[4].FragmentPics.Select(p => p.NeutralChange), new[] { "", "+H", "", "-H" });
		}
	}
}