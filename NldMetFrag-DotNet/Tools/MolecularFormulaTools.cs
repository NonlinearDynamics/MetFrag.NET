using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using org.openscience.cdk;
using org.openscience.cdk.config;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.tools.manipulator;

namespace NldMetFrag_DotNet.Tools
{
	public class MolecularFormulaTools
	{
		public static double GetMonoisotopicMass(string formula)
		{
			var parsedFormula = new Dictionary<string, double>();
			parsedFormula = ParseFormula(formula);

			return parsedFormula.Keys.Sum(s => parsedFormula[s]);
		}

		public static double GetMonoisotopicMass(IMolecularFormula formula)
		{
			return MolecularFormulaManipulator.getTotalExactMass(formula);
		}

		private static Dictionary<string, double> ParseFormula(string formula)
		{
			var parsedFormula = new Dictionary<string, double>();
			var regexSymbols = @"\d+"; //digit at least once
			var regexNumbers = @"[A-Z]{1}"; //not a digit

			var pSymbols = new Regex(regexSymbols);
			var pNumbers = new Regex(regexNumbers);

			var symbols = pSymbols.Split(formula).Where(s => !string.IsNullOrEmpty(s)).ToArray();
			var numbers = pNumbers.Split(formula);

			var numberCount = 1;
			for (var i = 0; i < symbols.Length; i++)
			{
				//create temporary atom with symbol and "configure" it
				IAtom a = new Atom(symbols[i]);

				var isofac = IsotopeFactory.getInstance(new ChemObject().getBuilder());
				isofac.configure(a);

				//fix if the digit is not written
				if (string.IsNullOrEmpty(numbers[numberCount]) && numberCount > 0)
				{
					numbers[numberCount] = "1";
				}

				var mass = a.getExactMass().doubleValue();
				mass = mass * double.Parse(numbers[numberCount]);
				numberCount++;
				parsedFormula[symbols[i]] = mass;
			}

			return parsedFormula;
		}

		public static Dictionary<string, double> ParseFormula(IMolecularFormula formula)
		{
			var parsedFormula = new Dictionary<string, double>();

			var elements = MolecularFormulaManipulator.elements(formula);
			foreach (var element in elements.ToWindowsEnumerable<IElement>())
			{
				var elementCount = MolecularFormulaManipulator.getElementCount(formula, element);
				var symbol = element.getSymbol();
				var a = new Atom(symbol);
				var isofac = IsotopeFactory.getInstance(new ChemObject().getBuilder());
				isofac.configure(a);
				var mass = a.getExactMass().doubleValue();
				mass = mass * elementCount;

				parsedFormula[symbol] = mass;
			}
			return parsedFormula;
		}

		public static bool IsPossibleNeutralLoss(Dictionary<string, double> originalFormulaMap, IMolecularFormula neutralLossFormula)
		{
			var isPossible = false;
			var neutralLossFormulaMap = ParseFormula(neutralLossFormula);

			foreach (var element in neutralLossFormulaMap.Keys)
			{
				if (originalFormulaMap.ContainsKey(element))
				{
					var massElementOrig = originalFormulaMap[element];
					var massNeutralLoss = neutralLossFormulaMap[element];
					if ((massElementOrig - massNeutralLoss) < 0)
					{
						isPossible = false;
						break;
					}
					else
					{
						//neutral loss is possible with this formula
						isPossible = true;
					}
				}
					//element not contained in candidate fragment
				else
				{
					break;
				}
			}

			return isPossible;
		}
	}
}