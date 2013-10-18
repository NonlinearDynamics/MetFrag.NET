using MetFragNET.Results;
using NUnit.Framework;
using org.openscience.cdk;
using org.openscience.cdk.interfaces;

namespace MetFragNETTests
{
	[TestFixture]
	public class BondEnergyCalculatorTests
	{
		[Test]
		public void TotalBondEnergy_WhenThereAreNoBonds_ReturnsZero()
		{
			// Arrange
			var molecule = new AtomContainer();
			var calculator = new BondEnergyCalculator(molecule);

			// Act
			var bondEnergy = calculator.TotalBondEnergy();

			// Assert
			Assert.That(bondEnergy, Is.EqualTo(0));
		}

		[Test]
		public void TotalBondEnergy_WhenThereAreBonds_ReturnsSumOfTheBondEnergies()
		{
			// Arrange
			var molecule = new AtomContainer();

			// Bonds: C=C, C-H, C~N
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("N")));
			molecule.addBond(0, 1, IBond.Order.DOUBLE);
			molecule.addBond(1, 2, IBond.Order.SINGLE);
			molecule.addBond(1, 3, IBond.Order.TRIPLE);

			var calculator = new BondEnergyCalculator(molecule);

			// Act
			var bondEnergy = calculator.TotalBondEnergy();

			// Assert
			Assert.That(bondEnergy, Is.EqualTo(612 + 412 + 890));
		}

		[Test]
		public void TotalBondEnergy_WhenThereAreUnknownBonds_ReplacesUnknownsWithC_CBond()
		{
			// Arrange
			var molecule = new AtomContainer();

			// Bonds: C=C, C-H, C~N, N~X (Unknown)
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("N")));
			molecule.addAtom(new Atom(new Element("X")));
			molecule.addBond(0, 1, IBond.Order.DOUBLE);
			molecule.addBond(1, 2, IBond.Order.SINGLE);
			molecule.addBond(1, 3, IBond.Order.TRIPLE);
			molecule.addBond(3, 4, IBond.Order.TRIPLE);

			var calculator = new BondEnergyCalculator(molecule);

			// Act
			var bondEnergy = calculator.TotalBondEnergy();

			// Assert
			Assert.That(bondEnergy, Is.EqualTo(612 + 412 + 890 + 348));
		}

		[Test]
		public void TotalBondEnergy_WhenThereAreBondRings_ReturnsTheSumOfAllBonds()
		{
			// Arrange
			var molecule = new AtomContainer();

			// Bonds: 5 carbon ring, each with an H bonded by C-H
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));
			molecule.addAtom(new Atom(new Element("C")));

			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("H")));
			molecule.addAtom(new Atom(new Element("H")));

			// Ring
			molecule.addBond(0, 1, IBond.Order.SINGLE);
			molecule.addBond(1, 2, IBond.Order.SINGLE);
			molecule.addBond(2, 3, IBond.Order.SINGLE);
			molecule.addBond(3, 4, IBond.Order.SINGLE);
			molecule.addBond(4, 0, IBond.Order.SINGLE);

			// C-H bonds
			molecule.addBond(0, 5, IBond.Order.SINGLE);
			molecule.addBond(1, 6, IBond.Order.SINGLE);
			molecule.addBond(2, 7, IBond.Order.SINGLE);
			molecule.addBond(3, 8, IBond.Order.SINGLE);
			molecule.addBond(4, 9, IBond.Order.SINGLE);

			var calculator = new BondEnergyCalculator(molecule);

			// Act
			var bondEnergy = calculator.TotalBondEnergy();

			// Assert
			Assert.That(bondEnergy, Is.EqualTo(5 * 348 + 5 * 412));
		}
	}
}