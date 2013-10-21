using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using org.openscience.cdk;
using org.openscience.cdk.config;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.tools.manipulator;

namespace MetFragNET.Tools
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

		// This is ported from the CDK and modified to not do so much uneccessary looping
		public static string GetString(IMolecularFormula formula)
		{
			var carbon = new Element("C");
			var poossibleElements = MolecularFormulaManipulator.containsElement(formula, carbon) ? HillSystem.ElementsWithCarbons : HillSystem.ElementsWithoutCarbons;

			var elemCounts = new OrderedDictionary();
			foreach (var possibleElement in poossibleElements)
			{
				elemCounts.Add(possibleElement, 0);
			}

			foreach (var isotope in formula.isotopes().ToWindowsEnumerable<IIsotope>())
			{
				var isotopeSymbol = isotope.getSymbol();
				var currentCount = (int)elemCounts[isotopeSymbol];
				elemCounts[isotopeSymbol] = currentCount + formula.getIsotopeCount(isotope);
			}

			var parts = new List<string>();
			foreach (DictionaryEntry elemCount in elemCounts)
			{
				var count = (int)elemCount.Value;
				var elem = (string)elemCount.Key;
				if (count == 1)
				{
					parts.Add(elem);
				}
				else if (count > 1)
				{
					parts.Add(string.Format("{0}{1}", elem, count));
				}
			}

			return string.Join("", parts);
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