using System;
using System.Text.RegularExpressions;
using MetFragNET.Results;
using NUnit.Framework.Constraints;

namespace MetFragNETTests
{
	public class FragConstraint : Constraint
	{
		private struct FragPair : IEquatable<FragPair>
		{
			private readonly string formula;
			private readonly double mass;
			private readonly string neutralChange;
			private static readonly Regex html = new Regex("<[^>]+>");

			public FragPair(string formula, string neutralChange, double mass)
			{
				this.formula = formula;
				this.neutralChange = neutralChange;
				this.mass = mass;
			}

			public FragPair(PeakMolPair pair)
			{
				formula = html.Replace(pair.MolecularFormula, "");
				neutralChange = pair.NeutralChange;
				mass = pair.MatchedMass;
			}

			public bool Equals(FragPair other)
			{
				return formula.Equals(other.formula) && neutralChange.Equals(other.neutralChange) && Math.Abs(mass - other.mass) < 1e-4;
			}

			public override string ToString()
			{
				return string.Format("{0} {1} ({2} Da)", formula, neutralChange, mass);
			}
		}

		private readonly FragPair expected;

		public FragConstraint(string formula, string neutralChange, double mass)
		{
			expected = new FragPair(formula, neutralChange, mass);
		}

		public override bool Matches(object value)
		{
			var pair = actual = new FragPair((PeakMolPair)value);
			return expected.Equals(pair);
		}

		public override void WriteDescriptionTo(MessageWriter writer)
		{
			writer.Write(expected);
		}

		public override void WriteActualValueTo(MessageWriter writer)
		{
			writer.Write(actual);
		}
	}
}
