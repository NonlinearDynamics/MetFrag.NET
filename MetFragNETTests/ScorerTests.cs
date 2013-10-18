using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NldMetFrag_DotNet.Scoring;

namespace NldMetFrag_DotNetTests
{
	[TestFixture]
	public class ScorerTests
	{
		private const double delta = 1e-8;

		[Test]
		public void CalculateScore_WhenSpectrumHasNoFragments_ReturnsZero()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(new List<Tuple<double, double>>(), Enumerable.Empty<Tuple<double, double, double>>(), 100.0);

			// Assert
			Assert.That(score, Is.EqualTo(0));
		}

		[Test]
		public void CalculateScore_WhenThereAreNoMatchedFragments_ReturnsZero()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0)},
				Enumerable.Empty<Tuple<double, double, double>>(),
				100.0);

			// Assert
			Assert.That(score, Is.EqualTo(0));
		}

		[Test]
		public void CalculateScore_AllFragmentsAreMatched_AndNoBondEnergyIsLost_ReturnsOneHundred()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 0.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(100));
		}

		[Test]
		public void CalculateScore_SomeFragmentsAreMatched_AndNoBondEnergyIsLost_ReturnsProportionOfWeightedIntensityMatched()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 0.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(4.05135967785514).Within(delta));
		}

		[Test]
		public void CalculateScore_SomeFragmentsAreMatched_AndSomeBondEnergyIsLost_TakesIntoAccountBondEnergyLoss()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 10.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(3.64622371006963).Within(delta));
		}

		[Test]
		public void CalculateScore_SomeFragmentsAreMatched_AndSomeBondEnergyIsLost_AndHasSomeHydrogenPenalty_TakesIntoAccountBondEnergyLossAndHydrogenPenalty()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 15.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(3.44365572617687).Within(delta));
		}

		[Test]
		public void CalculateScore_SomeFragmentsAreMatched_AndSumOfBondEnergyAndHydrogenPenaltyIsGreaterThanTotalBondEnergy_ReturnsScoreOfZero()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 115.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(0));
		}

		[Test]
		public void CalculateScore_SomeFragmentsAreMatched_AndSumOfBondEnergyAndHydrogenPenaltyIsGreaterThanTotalBondEnergyForSomePeaks_OnlyConsidersGoodPeaks()
		{
			// Arrange
			var calculator = new Scorer();

			// Act
			var score = calculator.Score(
				new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)},
				new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 115.0), Tuple.Create(50.0, 200.0, 0.0)},
				100.0
				);

			// Assert
			Assert.That(score, Is.EqualTo(95.9486403221449).Within(delta));
		}

		[Test]
		public void CalculateScore_WhenTwoCompoundsMatchSameFragments_OneWithLowerTotalBondEnergyGetsHigherScore()
		{
			// Arrange
			var calculator = new Scorer();
			var spectrum = new List<Tuple<double, double>> {Tuple.Create(20.0, 100.0), Tuple.Create(50.0, 200.0)};

			var fragments1 = new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 15.0)};
			var fragments2 = new List<Tuple<double, double, double>> {Tuple.Create(20.0, 100.0, 20.0)};

			// Act
			var score1 = calculator.Score(spectrum, fragments1, 100.0);
			var score2 = calculator.Score(spectrum, fragments2, 100.0);

			// Assert
			Assert.That(score1, Is.GreaterThan(score2));
		}
	}
}