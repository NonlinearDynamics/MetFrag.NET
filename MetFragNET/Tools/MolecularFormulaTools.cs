using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using java.lang;
using org.openscience.cdk.config;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.silent;
using org.openscience.cdk.tools.manipulator;
using Atom = org.openscience.cdk.Atom;
using ChemObject = org.openscience.cdk.ChemObject;
using Element = org.openscience.cdk.Element;

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

				var isofac = Isotopes.getInstance();
				isofac.configure(a);

				//fix if the digit is not written
				if (string.IsNullOrEmpty(numbers[numberCount]) && numberCount > 0)
				{
					numbers[numberCount] = "1";
				}

				var mass = a.getExactMass().doubleValue();
                mass = mass * double.Parse(numbers[numberCount], CultureInfo.InvariantCulture);
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
				var isofac = Isotopes.getInstance();
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

		public static IMolecularFormula GetMolecularFormula(IAtomContainer atomContainer)
		{
			var formula = new MolecularFormula();
			var charge = 0;
			var hydrogen = new Atom("H");

			foreach (var iAtom in atomContainer.atoms().ToWindowsEnumerable<IAtom>())
			{
				formula.addIsotope(iAtom);
				charge += iAtom.getFormalCharge().intValue();
				var implicitHydrogenCount = iAtom.getImplicitHydrogenCount();
				var implicitHydrogenCountValue = implicitHydrogenCount != null ? implicitHydrogenCount.intValue() : (int?) null;

				if (implicitHydrogenCountValue.HasValue && implicitHydrogenCountValue.Value > 0)
				{
					formula.addIsotope(hydrogen, implicitHydrogenCountValue.Value);
				}
			}

			formula.setCharge(new Integer(charge));
			return formula;
		}

		public static bool IsPossibleNeutralLoss(Dictionary<string, double> originalFormulaMap, IMolecularFormula neutralLossFormula)
		{
			var isPossible = false;
			var neutralLossFormulaMap = ParseFormula(neutralLossFormula);

			foreach (var element in neutralLossFormulaMap.Keys)
			{
				var massElementOrig = 0.0;
				if (originalFormulaMap.TryGetValue(element, out massElementOrig))
				{
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