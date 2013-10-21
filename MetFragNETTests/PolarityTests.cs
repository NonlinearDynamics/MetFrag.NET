using System.Linq;
using System.Text;
using System.Threading;
using MetFragNET;
using MetFragNETTests.Properties;
using NUnit.Framework;

namespace MetFragNETTests
{
	[TestFixture]
	public class PolarityTests
	{
		private const double mzAbs = 0.1;
		private const double mzPpm = 0.5; 

		[Test]
		public void Negative_GainsAndLosesHydrogen()
		{
			const string peaks = @"113.076583862305 3.40501737594604
117.040496826172 3.55244660377502
118.999359130859 4.39520215988159
124.740966796875 2.43928718566895
142.267822265625 2.85816287994385";
			var fragger = new MetFrag(Encoding.Default.GetString(Resources.Melibiose));
			var results = fragger.metFrag(double.MaxValue, peaks, -1, mzAbs, mzPpm, CancellationToken.None).ToList();

			Assert.That(results.Count, Is.EqualTo(1));
			var frags = results[0].FragmentPics.ToList();
			Assert.That(frags.Count, Is.EqualTo(6));
			Assert.That(frags[0], new FragConstraint("C4H7O4", "", 119.0344));
			Assert.That(frags[1], new FragConstraint("C4H6O4", "+H", 119.0344));
			Assert.That(frags[2], new FragConstraint("C4H8O4", "-H", 119.0344));
			Assert.That(frags[3], new FragConstraint("C4H6O4", "-H", 117.0188));
			Assert.That(frags[4], new FragConstraint("C5H8O3", "+H", 117.0552));
			Assert.That(frags[5], new FragConstraint("C5H6O3", "-H", 113.0239));
		}

		[Test]
		public void Positive_GainsAndLosesHydrogen()
		{
			const string peaks = @"121.065101623535 91
180.260803222656 112
133.076599121094 441
179.09489440918 765
171.044692993164 283";
			var fragger = new MetFrag(Encoding.Default.GetString(Resources.Norsalsolinol));
			var results = fragger.metFrag(double.MaxValue, peaks, 1, mzAbs, mzPpm, CancellationToken.None).ToList();

			Assert.That(results.Count, Is.EqualTo(1));
			var frags = results[0].FragmentPics.ToList();
			Assert.That(frags.Count, Is.EqualTo(7));
			Assert.That(frags[0], new FragConstraint("C10H13NO2", "", 179.0946));
			Assert.That(frags[1], new FragConstraint("C9H10N", "+H", 133.0891));
			Assert.That(frags[2], new FragConstraint("C9H8O", "+H", 133.0653));
			Assert.That(frags[3], new FragConstraint("C8H7NO", "", 133.0528));
			Assert.That(frags[4], new FragConstraint("C8H10N", "+H", 121.0891));			 
			Assert.That(frags[5], new FragConstraint("C8H11N", "", 121.0891));
			Assert.That(frags[6], new FragConstraint("C7H6O2", "-H", 121.029));
		}
	}
}