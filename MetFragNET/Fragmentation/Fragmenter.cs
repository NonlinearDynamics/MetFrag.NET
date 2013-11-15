using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MetFragNET.Algorithm;
using MetFragNET.Spectra;
using MetFragNET.Tools;
using ikvm.extensions;
using java.io;
using java.lang;
using org.openscience.cdk.aromaticity;
using org.openscience.cdk.config;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.ringsearch;
using org.openscience.cdk.silent;
using org.openscience.cdk.tools.manipulator;
using Double = System.Double;
using String = System.String;

namespace MetFragNET.Fragmentation
{
	public class Fragmenter
	{
		private readonly Dictionary<string, double> atomMasses = new Dictionary<string, double>();
		private readonly IList<Peak> peakList;

		//store the sum formula with its atom container properties
		private readonly Dictionary<string, List<IAtomContainer>> sumformulaToFragMap = new Dictionary<string, List<IAtomContainer>>();
		private IRingSet allRings;
		private List<IBond> aromaticBonds;
		private List<IAtom> atomList = new List<IAtom>();
		private int atomsContained;
		private FragmentationConfig config;
		private Double currentFragWeight;
		private List<BondPair> knownBonds;
		private double minWeight;
		private Dictionary<double, NeutralLoss> neutralLoss;
		private IAtomContainer originalMolecule;
		private PostProcessor pp;
		private double protonMass = MolecularFormulaTools.GetMonoisotopicMass("H1");

		public Fragmenter(IList<Peak> peakList, FragmentationConfig config)
		{
			this.peakList = peakList;
			this.config = config;

			SetMinWeight();
			ReadInNeutralLosses();
		}

		public IAtomContainer markAllBonds(IAtomContainer original)
		{
			MoleculeTools.MoleculeNumbering(original);
			atomsContained = original.getAtomCount();
			return original;
		}

		private bool preprocessMolecule(IAtomContainer original)
		{
			//prepare atom weights
			var atomWeightCanBeCalculated = prepareAtomWeights(original);
			// If the SDF contains an "R" or something, we don't know its mass, so this whole endevour is fruitless
			// (since we won't be able to match the fragment masses to the peaks).
			if (!atomWeightCanBeCalculated)
			{
				return false;
			}

			//mark all the bonds and atoms with numbers --> identify them later on        
			originalMolecule = markAllBonds(original);

			//do ring detection with the original molecule
			var allRingsFinder = new AllRingsFinder();

			// Steve: Set a really large timeout, because we don't want to crash just because it took a long time.
			// The size limit of 7 below should stop it looping forever.
			allRingsFinder.setTimeout(int.MaxValue);
			// TODO: Steve: The 7 is a max ring size - I added this to prevent it getting in to infinite loops (7 comes from MetFrag
			// where it is used in some other random class). Don't know if we need to change this??
			allRings = allRingsFinder.findAllRings(originalMolecule, Integer.valueOf(7));
			aromaticBonds = new List<IBond>();

			CDKHueckelAromaticityDetector.detectAromaticity(originalMolecule);

			foreach (var bond in originalMolecule.bonds().ToWindowsEnumerable<IBond>())
			{
				//lets see if it is a ring and aromatic
				var rings = allRings.getRings(bond);
				//don't split up aromatic rings...see constructor for option
				for (var i = 0; i < rings.getAtomContainerCount(); i++)
				{
					var aromatic = AromaticityCalculator.isAromatic((IRing)rings.getAtomContainer(i), originalMolecule);
					if (aromatic)
					{
						aromaticBonds.Add(bond);
						break;
					}
				}
			}
			return true;
		}

		public List<IAtomContainer> GenerateFragmentsEfficient(IAtomContainer atomContainer, bool verbose, int treeDepthMax, string identifier, CancellationToken isCancelled)
		{
			isCancelled.ThrowIfCancellationRequested();

			var fragmentsReturn = new List<IAtomContainer>();

			//now set a new min weight
			minWeight = minWeight - treeDepthMax;

			//fragments not yet split up enough...QUEUE --> BFS
			var fragmentQueue = new Queue<IAtomContainer>();

			//do preprocess: find all rings and aromatic rings...mark all bonds
			var preprocessedOk = preprocessMolecule(atomContainer);
			if (!preprocessedOk)
			{
				return new List<IAtomContainer>();
			}
			//create new PostProcess object
			pp = new PostProcessor(allRings, neutralLoss.ToDictionary(x => x.Key, x => x.Value));

			//add original molecule to it
			fragmentQueue.Enqueue(originalMolecule);
			var treeDepth = 1;

			//add neutral loss in the first step for sure
			var molecularFormula = MolecularFormulaTools.GetMolecularFormula(originalMolecule);
			//now add neutral losses to it
			var fragsNeutralLoss = AddNeutralLosses(originalMolecule, molecularFormula, true);

			fragmentsReturn.Add(createMolecule(atomContainer, "0.0", treeDepth));
			foreach (var fragNeutralLoss in fragsNeutralLoss)
			{
				fragmentQueue.Enqueue(fragNeutralLoss);
				fragNeutralLoss.setProperty("TreeDepth", "1");
				fragmentsReturn.Add(createMolecule(fragNeutralLoss, (String)fragNeutralLoss.getProperty("BondEnergy"), treeDepth));
			}

			//get the number of preprocessed spectra
			var tempLevelCount = fragmentQueue.Count;

			while (fragmentQueue.Count > 0)
			{
				isCancelled.ThrowIfCancellationRequested();

				//remember already tried combinations of ring...so there are less combinations
				knownBonds = new List<BondPair>();

				//get a fragment from the priority queue
				var currentFragment = fragmentQueue.Dequeue();
				var atomBonds = GetAtomBondsDictionary(currentFragment);

				//reduce the number of fragments in this level
				tempLevelCount--;

				//add to result list...don't break fragments which only have 2 bonds left
				if (currentFragment.getBondCount() < 2)
				{
					continue;
				}

				var splitableBonds = getSplitableBonds(currentFragment, atomBonds);

				//no splitable bonds are found
				if (splitableBonds.Count == 0)
				{
					continue;
				}

				foreach (var bond in splitableBonds)
				{
					var parts = splitMolecule(currentFragment, bond, atomBonds);

					foreach (var partContainer in parts)
					{
						fragmentQueue.Enqueue(partContainer);
						fragmentsReturn.Add(createMolecule(partContainer, (String)partContainer.getProperty("BondEnergy"), treeDepth));
					}
				}

				//set the number of fragments for this level
				if (tempLevelCount <= 0)
				{
					//count for the current level
					tempLevelCount = fragmentQueue.Count;
					treeDepth++;
				}

				//generate only fragments until a specified depth
				if (treeDepth >= treeDepthMax)
				{
					break;
				}
			}

			//return all fragments
			return fragmentsReturn;
		}

		private IMolecule createMolecule(IAtomContainer atomContainer, String bondEnergy, int treeDepth)
		{
			IMolecule molecule = new Molecule(atomContainer);

			molecule.setProperties(atomContainer.getProperties());
			molecule.setProperty("BondEnergy", bondEnergy);
			molecule.setProperty("TreeDepth", treeDepth.toString());

			return molecule;
		}

		private List<IBond> getSplitableBonds(IAtomContainer atomContainer, IDictionary<IAtom, IList<IBond>> atomBonds)
		{
			// find the splitable bonds
			var splitableBonds = new List<IBond>();

			foreach (var bond in atomContainer.bonds().ToWindowsEnumerable<IBond>())
			{
				var isTerminal = false;

				// lets see if it is a terminal bond...we dont want to split up the hydrogen
				foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>())
				{
					//dont split up "terminal" H atoms
					if (atomBonds[atom].Count == 1 && atom.getSymbol().StartsWith("H"))
					{
						//terminal hydrogen...ignore it
						isTerminal = true;
						break;
					}
				}

				if (!isTerminal)
				{
					splitableBonds.Add(bond);
				}
			}
			return splitableBonds;
		}


		private IEnumerable<IAtomContainer> splitMolecule(IAtomContainer atomContainer, IBond bond, IDictionary<IAtom, IList<IBond>> atomBonds)
		{
			//if this bond is in a ring we have to split another bond in this ring where at least one 
			//bond is in between. Otherwise we wont have two fragments. Else normal split.

			var ret = new List<IAtomContainer>();

			//get bond energy for splitting this bond
			var currentBondEnergy = BondEnergies.Lookup(bond);			

			//bond is in a ring....so we have to split up another bond to break it
			var rings = allRings.getRings(bond);
			if (rings.getAtomContainerCount() != 0)
			{
				foreach (var bondInRing in rings.getAtomContainer(0).bonds().ToWindowsEnumerable<IBond>())
				{
					//if the bonds are the same...this wont split up the ring
					if (bondInRing == bond)
					{
						continue;
					}

					//check for already tried bonds
					var check = new BondPair(bond, bondInRing);
					if (knownBonds.Contains(check))
					{
						continue;
					}
					knownBonds.Add(new BondPair(bond, bondInRing));


					var set = new List<IAtomContainer>();
					var bondListList = new List<List<IBond>>();
					var fragWeightList = new List<Double>();

					foreach (var currentAtom in bond.atoms().ToWindowsEnumerable<IAtom>())
					{
						//List with bonds in Ring
						var partRing = new List<IBond>();
						//reset the weight because it is computed inside the traverse
						currentFragWeight = 0.0;
						//initialize new atom list
						atomList = new List<IAtom>();

						//clone current atom because a single electron is being added...homolytic cleavage
						partRing = traverse(atomBonds, currentAtom, partRing, bond, bondInRing);

						bondListList.Add(partRing);
						fragWeightList.Add(currentFragWeight);

						var temp = makeAtomContainer(currentAtom, partRing);
						//set the properties again!
						var properties = atomContainer.getProperties();
						temp.setProperties(properties);


						//*********************************************************
						//BOND ENERGY CALCULATION
						//calculate bond energy
						var currentBondEnergyRing = BondEnergies.Lookup(bondInRing);

						//*********************************************************

						//now set property
						temp = setBondEnergy(temp, (currentBondEnergyRing + currentBondEnergy));
						set.Add(temp);
					}

					//now maybe add the fragments to the list
					for (var j = 0; j < set.Count; j++)
					{
						//Render.Draw(set.getAtomContainer(j), "");
						if (set[j].getAtomCount() > 0 && set[j].getBondCount() > 0 && set[j].getAtomCount() != atomContainer.getAtomCount())
						{
							//now check the current mass
							var fragMass = getFragmentMass(set[j], fragWeightList[j]);
							//check the weight of the current fragment
							if (!isHeavyEnough(fragMass))
							{
								continue;
							}

							//returns true if isomorph
							//set the current sum formula
							var fragmentFormula = MolecularFormulaTools.GetMolecularFormula(set[j]);
							var currentSumFormula = MolecularFormulaTools.GetString(fragmentFormula);

							if (isIdentical(set[j], currentSumFormula))
							{
								continue;
							}

							//add the fragment to the return list
							ret.Add(set[j]);
						}
					}
				}
			}
			else
			{
				var set = new List<IAtomContainer>();
				var bondListList = new List<List<IBond>>();
				var fragWeightList = new List<Double>();

				//get the atoms from the splitting bond --> create 2 fragments
				foreach (var currentAtom in bond.atoms().ToWindowsEnumerable<IAtom>())
				{
					var part = new List<IBond>();
					//reset the weight because it is computed inside the traverse
					currentFragWeight = 0.0;
					//initialize new atom list
					atomList = new List<IAtom>();
					part = traverse(atomBonds, currentAtom, part, bond);
					bondListList.Add(part);

					//create Atomcontainer out of bondList        		
					var temp = makeAtomContainer(currentAtom, part);
					//set the properties again!
					var properties = atomContainer.getProperties();
					temp.setProperties(properties);
					//now calculate the correct weight subtrating the possible neutral loss mass

					fragWeightList.Add(currentFragWeight);


					//now set property: BondEnergy!
					temp = setBondEnergy(temp, currentBondEnergy);
					set.Add(temp);
				}


				//at most 2 new molecules
				for (var i = 0; i < set.Count; i++)
				{
					if (set[i].getAtomCount() > 0 && set[i].getBondCount() > 0 && set[i].getAtomCount() != atomContainer.getAtomCount())
					{
						//now check the current mass
						var fragMass = getFragmentMass(set[i], fragWeightList[i]);
						//check the weight of the current fragment
						if (!isHeavyEnough(fragMass))
						{
							continue;
						}

						//set the current sum formula
						var fragmentFormula = MolecularFormulaTools.GetMolecularFormula(set[i]);
						var currentSumFormula = MolecularFormulaTools.GetString(fragmentFormula);
						//returns true if isomorph (fast isomorph check)
						if (isIdentical(set[i], currentSumFormula))
						{
							continue;
						}

						ret.Add(set[i]);
					}
				}
			}

			return ret;
		}


		/**
		 * Sets the bond energy.
		 * 
		 * @param mol the mol
		 * @param bondEnergy the bond energy
		 * 
		 * @return the i atom container
		 */

		private IAtomContainer setBondEnergy(IAtomContainer mol, Double bondEnergy)
		{
			var props = mol.getProperties();
			if (props.get("BondEnergy") != null)
			{
				var sumEnergy = Convert.ToDouble((String)props.get("BondEnergy")) + bondEnergy;
				props.put("BondEnergy", sumEnergy.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				props.put("BondEnergy", bondEnergy.ToString(CultureInfo.InvariantCulture));
			}

			mol.setProperties(props);
			return mol;
		}


		/**
		 * Sets the bond energy.
		 * 
		 * @param mol the mol
		 * @param bondEnergy the bond energy
		 * 
		 * @return the i atom container
		 */

		private IAtomContainer setBondEnergy(IAtomContainer origMol, IAtomContainer mol, Double bondEnergy)
		{
			var props = mol.getProperties();
			var bondEnergyOrig = (String)origMol.getProperty("BondEnergy");
			if (bondEnergyOrig != null)
			{
				var sumEnergy = Convert.ToDouble(bondEnergyOrig) + bondEnergy;
				props.put("BondEnergy", sumEnergy.ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				props.put("BondEnergy", bondEnergy.ToString(CultureInfo.InvariantCulture));
			}

			mol.setProperties(props);
			return mol;
		}


		/**
		 * Gets the fragment mass subtracting the neutral loss from it.
		 * It also sets the new FragmentWeight property
		 * 
		 * @param fragment the fragment
		 * @param mass the mass
		 * 
		 * @return the fragment mass
		 */

		private double getFragmentMass(IAtomContainer fragment, double mass)
		{
			var massFinal = mass;
			var nlMass = 0.0;
			if (fragment.getProperty("FragmentMass") != null && (string)fragment.getProperty("FragmentMass") != "")
			{
				if (fragment.getProperty("NlMass") != null && (string)fragment.getProperty("NlMass") != "")
				{
					var tempNLMass = fragment.getProperty("NlMass").ToString().Split(',');
					for (var i = 0; i < tempNLMass.Count(); i++)
					{
						nlMass += Convert.ToDouble(tempNLMass[i]);
					}
				}
			}
			massFinal = massFinal - nlMass;
			fragment.setProperty("FragmentMass", massFinal.ToString(CultureInfo.InvariantCulture));
			return massFinal;
		}

		/**
		 * Checks if the fragment is heavy enough.
		 * 
		 * @param mass the mass
		 * 
		 * @return true, if is heavy enough
		 * 
		 * @throws CDKException the CDK exception
		 * @throws Exception the exception
		 */

		private bool isHeavyEnough(Double mass)
		{
			var candidate = false;

			//positive or negative mode!?
			protonMass = protonMass * config.Mode;
			var min = (minWeight - (config.Mzabs + PpmTool.GetPPMDeviation(minWeight, config.Mzppm)));
			if ((mass + protonMass) > min)
			{
				candidate = true;
			}

			return candidate;
		}


		/**
		 * Quick isomorphism check:
		 * ...if the fragment is isomorph to the ones already in the map
		 * ...only the atoms are compared and a list of bonds is stored in a
		 * 		map with sum formula to fragment
		 * 
		 * 
		 * @param fragment the fragment to be checked
		 * 
		 * @return true, if successful
		 * 
		 * @throws CDKException the CDK exception
		 */

		private bool isIdentical(IAtomContainer fragment, string currentSumFormula)
		{
			var isomorph = false;

			List<IAtomContainer> fragsToCompare = null;
			
			//iterate over list to check for isomorphism
			if (sumformulaToFragMap.TryGetValue(currentSumFormula, out fragsToCompare))
			{
				isomorph = identicalAtoms(fragment, fragsToCompare);

				if (isomorph)
				{
					//now replace fragment if its "bond energy is less"
					var bondEnergy = Double.Parse((String)fragment.getProperty("BondEnergy"), CultureInfo.InvariantCulture);
					foreach (var atomContainer in fragsToCompare)
					{
                        if (Double.Parse((String)atomContainer.getProperty("BondEnergy"), CultureInfo.InvariantCulture) > bondEnergy)
						{
							addFragmentToListMapReplace(fragment, currentSumFormula);
						}
					}
				}
					//if not in map (with this formula) add it
				else
				{
					addFragmentToListMap(fragment, currentSumFormula);
				}
			}
			else
			{
				//sum formula has no entry in map yet
				addFragmentToListMap(fragment, currentSumFormula);
			}
			return isomorph;
		}

		/**
		 * Very quick and easy isomorphism check.
		 * 
		 * @param molecule1 the molecule1
		 * @param fragsToCompare the frags to compare
		 * 
		 * @return true, if successful
		 */

		private bool identicalAtoms(IAtomContainer molecule1, List<IAtomContainer> fragsToCompare)
		{
			var molFormula = MolecularFormulaTools.GetMolecularFormula(molecule1);
			var molFormulaString = MolecularFormulaTools.GetString(molFormula);

			for (var i = 0; i < fragsToCompare.Count; i++)
			{
				//no match
				if (molecule1.getBondCount() != fragsToCompare[i].getBondCount() && molecule1.getAtomCount() != fragsToCompare[i].getAtomCount())
				{
					continue;
				}

				//Molecular Formula redundancy check
				var molFormulaFrag = MolecularFormulaTools.GetMolecularFormula(fragsToCompare[i]);
				var molFormulaFragString = MolecularFormulaTools.GetString(molFormulaFrag);
				if (molFormulaString.Equals(molFormulaFragString))
				{
					return true;
				}
			}

			//no match found
			return false;
		}

		/**
		 * Adds a fragment to the list of fragments with the current sum formula as key.
		 * 
		 * @param fragment the fragment
		 */

		private void addFragmentToListMap(IAtomContainer frag, String currentSumFormula)
		{
			//add sum formula molecule comb. to map
			List<IAtomContainer> tempList = null;

			if (sumformulaToFragMap.TryGetValue(currentSumFormula, out tempList))
			{
				tempList = tempList.ToList();
				tempList.Add(frag);
				sumformulaToFragMap[currentSumFormula] = tempList;
			}
			else
			{
				var temp = new List<IAtomContainer>();
				temp.Add(frag);
				sumformulaToFragMap[currentSumFormula] = temp;
			}
		}


		/**
		 * Adds a fragment to the list of fragments with the current sum formula as key and replaces 
		 * the current entry.
		 * 
		 * @param fragment the fragment
		 */

		private void addFragmentToListMapReplace(IAtomContainer frag, String currentSumFormula)
		{
			//add sum formula molecule comb. to map
			List<IAtomContainer> tempList = null;

			if (sumformulaToFragMap.TryGetValue(currentSumFormula, out tempList))
			{
				tempList = tempList.ToList();
				tempList.Clear();
				tempList.Add(frag);
				sumformulaToFragMap[currentSumFormula] = tempList;
			}
			else
			{
				var temp = new List<IAtomContainer>();
				temp.Add(frag);
				sumformulaToFragMap[currentSumFormula] = temp;
			}
		}

		/**
		 * Set new minweight.
		 * 
		 * @param fragment the fragment
		 */

		private void SetMinWeight()
		{
			minWeight = peakList.Any() ? peakList.Min(p => p.Mass) : 0;
		}

		/**
		 * Prepare atom weights. Get all atom weights from the sum formula
		 * 
		 * @param mol the mol
		 */

		private bool prepareAtomWeights(IAtomContainer mol)
		{
			var molecularFormula = MolecularFormulaTools.GetMolecularFormula(mol);

			var elements = MolecularFormulaManipulator.elements(molecularFormula);
			foreach (var element in elements.ToWindowsEnumerable<IElement>())
			{
				IAtom a = new Atom(element);
				try
				{
					IsotopeFactory.getInstance(a.getBuilder()).configure(a);
				}
				catch (IllegalArgumentException)
				{
					// This means it failed to get the mass for this element, so its an unknown element like "R" for example
					return false;
				}

				//get mass and store in map
				atomMasses[element.getSymbol()] = a.getExactMass().doubleValue();
			}
			return true;
		}


		/**
		 * Make atom container from a given bond list. For each bond iterate over atoms and add them to the partContainer 
		 * 
		 * @param the atom
		 * @param List of parts
		 * 
		 * @return partContainer
		 */

		private IAtomContainer makeAtomContainer(IAtom atom, List<IBond> parts)
		{
			var atoms = new List<IAtom>();
			var bonds = new List<IBond>();
			var atomsDone = new Dictionary<string, bool>();

			atoms.Add(atom);
			atomsDone[atom.getID()] = true;

			foreach (var aBond in parts)
			{
				foreach (var bondedAtom in aBond.atoms().ToWindowsEnumerable<IAtom>())
				{
					var done = false;
					atomsDone.TryGetValue(bondedAtom.getID(), out done);
					//check if the atom is already contained
					if (done)
					{
						continue;
					}
					
					atoms.Add(bondedAtom);
					atomsDone[bondedAtom.getID()] = true;
				}
				bonds.Add(aBond);				
			}

			IAtomContainer partContainer = new AtomContainer();
			partContainer.setAtoms(atoms.ToArray());
			partContainer.setBonds(bonds.ToArray());
			return partContainer;
		}


		/**
		 * Resursively traverse the molecule to get all the bonds in a list and return them. Start
		 * at the given atom
		 * 
		 * @param atomContainer the atom container
		 * @param atom the atom
		 * @param bondList the bond list
		 * 
		 * @return the list< i bond>
		 */

		private List<IBond> traverse(IDictionary<IAtom, IList<IBond>> atomBonds, IAtom atom, List<IBond> bondList, IBond bondToRemove)
		{
			var connectedBonds = atomBonds[atom];

			foreach (var aBond in connectedBonds)
			{
				if (bondList.Contains(aBond) || aBond.Equals(bondToRemove))
				{
					continue;
				}
				bondList.Add(aBond);

				//get the weight of the bonded atoms
				foreach (var atomWeight in aBond.atoms().ToWindowsEnumerable<IAtom>())
				{
					//get the prepared mass of the atom if it is not already counted
					if (!atomList.Contains(atomWeight))
					{
						currentFragWeight += atomMasses[atomWeight.getSymbol()];
						atomList.Add(atomWeight);
					}
				}

				var nextAtom = aBond.getConnectedAtom(atom);
				if (atomBonds[nextAtom].Count == 1)
				{
					continue;
				}
				traverse(atomBonds, nextAtom, bondList, bondToRemove);
			}
			return bondList;
		}

		private IDictionary<IAtom, IList<IBond>> GetAtomBondsDictionary(IAtomContainer atomContainer)
		{
			var dict = new Dictionary<IAtom, IList<IBond>>();
			foreach (var bond in atomContainer.bonds().ToWindowsEnumerable<IBond>())
			{
				foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>())
				{
					if (!dict.ContainsKey(atom))
					{
						dict[atom] = new List<IBond>();
					}
					dict[atom].Add(bond);
				}
			}
			return dict;
		}

		/**
		 * Resursively traverse the molecule to get all the bonds in a list and return them. Start at the given Atom.
		 * Ignore the 2 given bonds --> split up a ring!
		 * 
		 * @param atomContainer the atom container
		 * @param atom the atom
		 * @param bondList the bond list
		 * 
		 * @return the list< i bond>
		 */

		private List<IBond> traverse(IDictionary<IAtom, IList<IBond>> atomBonds, IAtom atom, List<IBond> bondList, IBond bondToRemove, IBond bondToRemove2)
		{
			var connectedBonds = atomBonds[atom];
			foreach (var aBond in connectedBonds)
			{
				if (bondList.Contains(aBond) || aBond.Equals(bondToRemove) || aBond.Equals(bondToRemove2))
				{
					continue;
				}
				bondList.Add(aBond);
				//get the weight of the bonded atoms
				foreach (var atomWeight in aBond.atoms().ToWindowsEnumerable<IAtom>())
				{
					//get the prepared mass of the atom if it is not already counted
					if (!atomList.Contains(atomWeight))
					{
						currentFragWeight += atomMasses[atomWeight.getSymbol()];
						atomList.Add(atomWeight);
					}
				}

				var nextAtom = aBond.getConnectedAtom(atom);
				if (atomBonds[nextAtom].Count == 1)
				{
					continue;
				}
				traverse(atomBonds, nextAtom, bondList, bondToRemove, bondToRemove2);
			}
			return bondList;
		}

		/**
		 * Adds the neutral losses but only where it possibly explaines a peak.
		 * Properties to be set seperated by ",", each column is one "entry":
		 * <ul>
		 * <li>Neutral loss list: elemental composition (-H2O,-HCOOH,CO2)
		 * <li>Neutral loss masses (18.01056, 46.00548, 43.98983)
		 * <li>Hydrogen difference (-H,-H,+H)
		 * <li>Current fragment mass...a single value (147.0232)
		 * </ul>
		 * 
		 * @param fragment the fragment
		 * @param fragmentFormula the fragment formula
		 * @param initialMolecule the initial molecule only important for the start
		 * 
		 * @return the list< i atom container>
		 * 
		 * @throws IOException Signals that an I/O exception has occurred.
		 * @throws CloneNotSupportedException the clone not supported exception
		 * @throws CDKException the CDK exception
		 */

		private IEnumerable<IAtomContainer> AddNeutralLosses(IAtomContainer fragment, IMolecularFormula fragmentFormula, bool initialMolecule)
		{
			var ret = new List<IAtomContainer>();
			var mass = MolecularFormulaTools.GetMonoisotopicMass(fragmentFormula);
			var originalFormulaMap = MolecularFormulaTools.ParseFormula(fragmentFormula);
			var checked_ = false;

			//in the first layer add all neutral losses!!! afterwards only if it matches a peak!
			foreach (var peak in peakList)
			{
				if (initialMolecule && checked_)
				{
					break;
				}

				var peakLow = peak.Mass - config.Mzabs - PpmTool.GetPPMDeviation(peak.Mass, config.Mzppm);
				var peakHigh = peak.Mass + config.Mzabs + PpmTool.GetPPMDeviation(peak.Mass, config.Mzppm);
				checked_ = true;

				foreach (var neutralLossMass in neutralLoss.Keys)
				{
					//filter appropriate neutral losses by mode...0 means this occurs in both modes
					if (neutralLoss[neutralLossMass].Mode == config.Mode || neutralLoss[neutralLossMass].Mode == 0)
					{
						var neutralLossFormula = neutralLoss[neutralLossMass].ElementalComposition;
						var isPossibleNeutralLoss = MolecularFormulaTools.IsPossibleNeutralLoss(originalFormulaMap, neutralLossFormula);

						if ((isPossibleNeutralLoss && ((mass + protonMass) - neutralLossMass) >= peakLow && (((mass + protonMass) - neutralLossMass) <= peakHigh)) || initialMolecule)
						{
							var fragmentsNL = pp.PostProcess(fragment, neutralLossMass);
							foreach (var x in fragmentsNL)
							{
								var fragmentNL = x;
								var fragmentMolFormula = MolecularFormulaTools.GetMolecularFormula(fragmentNL);
								var fragmentMass = MolecularFormulaTools.GetMonoisotopicMass(fragmentMolFormula);

								//skip this fragment which is lighter than the smallest peak
								if (fragmentMass < minWeight)
								{
									continue;
								}

								//set bond energy
								fragmentNL = setBondEnergy(fragment, fragmentNL, 500.0);

								var props = fragmentNL.getProperties();
								props.put("NeutralLossRule", MolecularFormulaTools.GetString(neutralLossFormula));
								addFragmentToListMap(fragmentNL, MolecularFormulaTools.GetString(fragmentMolFormula));

								//add to result list
								ret.Add(fragmentNL);
							}
							//peak found....test another one
							continue;
						}
					}
				}
			}
			return ret;
		}

		/**
		 * Gets the neutral losses which are stored in a file.
		 * 
		 * @return the neutral losses
		 */

		private void ReadInNeutralLosses()
		{
			neutralLoss = new Dictionary<double, NeutralLoss>();

			const string lossesString = "Ion Mode	Exact DM	Topological fragment	Elemental Composition	H-Difference	Furthest Distance	Atom to start Hydrogen Connected to Start Atom	Substance Class Identifier\n"
			                            + "+ -	18.01056	OH	H2O	-1	3	O	1	Non-specific (OH)\n"
			                            + "+	46.00548	COOH	HCOOH	-1	3	O	0	Aliphatic Acids, Amino Acid Conjugates â€¦\n"
			                            + "+ -	17.02655	NH2	NH3	-1	3	N	2	not sure\n"
			                            + "+ -	27.01090	CN	HCN	-1	3	N	0	Alkaloids\n"
			                            + "+ -	30.01056	COH	CH2O	-1	3	O	0	Methoxylated aromatic compounds\n"
			                            + "#+ -	27.99491	COH	CO	1	3	O	0	Flavonoids, Coumarins, Chromones â€¦.\n"
			                            + "#+ -	43.98983	COOH	CO2	1	3	O	0	Aromatic acids, Lactones \n"
			                            + "#-	79.95681	SO3H	SO3	1	3	S	0	0	-sulfated compounds\n"
			                            + "#+ -	162.05282	C6H11O5	C6H10O5	1	6	O	0	Glucosides";

			var lines = lossesString.Split('\n');

			//Read File Line By Line, skipping header
			foreach (var lineArray in lines.Skip(1).Where(l => !l.StartsWith("#")).Select(l => l.Split('\t')))
			{
				int mode;
				switch (lineArray[0])
				{
					case "+ -":
						mode = 0;
						break;
					case "-":
						mode = -1;
						break;
					default:
						mode = 1;
						break;
				}
				var mfT = new MolecularFormula();
				var mfE = new MolecularFormula();
				var nl = new NeutralLoss(MolecularFormulaManipulator.getMolecularFormula(lineArray[3], mfE), MolecularFormulaManipulator.getMolecularFormula(lineArray[2], mfT), mode, Integer.parseInt(lineArray[4]), Integer.parseInt(lineArray[5]), lineArray[6], Integer.parseInt(lineArray[7]));
                var deltaM = Double.Parse(lineArray[1], CultureInfo.InvariantCulture);
				neutralLoss[deltaM] = nl;
			}
		}
	}
}