using System.Collections.Generic;
using org.openscience.cdk;
using org.openscience.cdk.graph;
using org.openscience.cdk.interfaces;

namespace MetFragNET.Fragmentation
{
	public class PostProcessor
	{
		/** The all rings. */
		private readonly IRingSet allRingsOrig;
		private readonly IDictionary<double, NeutralLoss> neutralLoss;
		private IRingSet allRings;

		/**
		 * Instantiates a new post processing step. It reads in the neutral loss
		 * table and needs all the aromatic and ring bonds.
		 * 
		 * @param original
		 *            the original
		 * @param aromaticBonds
		 *            the aromatic bonds
		 * @param allRings
		 *            the all rings
		 * @throws IOException
		 * @throws NumberFormatException
		 */

		public PostProcessor(IRingSet allRings, IDictionary<double, NeutralLoss> neutralLossTable)
		{
			allRingsOrig = allRings;
			neutralLoss = neutralLossTable;
		}

		/**
		 * Post process a fragment. --> find neutral possible neutral losses read in
		 * from the file
		 * 
		 * @param original
		 *            the original
		 * 
		 * @return the i atom container set
		 * 
		 * @throws CDKException
		 *             the CDK exception
		 * @throws CloneNotSupportedException
		 *             the clone not supported exception
		 */

		public List<IAtomContainer> PostProcess(IAtomContainer original, double neutralLossMass)
		{
			// Render.Draw(original, "Original Main");

			var ret = new List<IAtomContainer>();
			allRings = new RingSet();

			if (allRingsOrig.getAtomContainerCount() > 0)
			{
				// get the rings which are not broken up yet
				var bondMap = new Dictionary<IBond, int>();
				var count = 0;

				foreach (var bondOrig in original.bonds().ToWindowsEnumerable<IBond>())
				{
					bondMap[bondOrig] = count;
					count++;
				}

				// check for rings which are not broken up!
				IRingSet validRings = new RingSet();
				for (var i = 0; i < allRingsOrig.getAtomContainerCount(); i++)
				{
					var bondcount = 0;

					foreach (var bondRing in allRingsOrig.getAtomContainer(i).bonds().ToWindowsEnumerable<IBond>())
					{
						if (bondMap.ContainsKey(bondRing))
						{
							bondcount++;
						}
					}
					if (bondcount == allRingsOrig.getAtomContainer(i).getBondCount())
					{
						validRings.addAtomContainer(allRingsOrig.getAtomContainer(i));
					}
				}
				// rings which are not split up
				allRings = validRings;
			}

			IAtomContainer temp = new AtomContainer();
			var doneAtoms = new List<IAtom>();
			var doneBonds = new List<IBond>();

			// now find out the important atoms of the neutral loss
			var atomToStart = neutralLoss[neutralLossMass].AtomToStart;

			foreach (var bond in original.bonds().ToWindowsEnumerable<IBond>())
			{
				if (doneBonds.Contains(bond))
				{
					continue;
				}
				else
				{
					doneBonds.Add(bond);
				}

				// check if this was checked b4
				foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>())
				{
					if (doneAtoms.Contains(atom))
					{
						continue;
					}
					else
					{
						doneAtoms.Add(atom);
					}

					// a possible hit
					if (atom.getSymbol().Equals(atomToStart) && !allRings.contains(atom))
					{
						// Render.Draw(original, "BEFORE");
						// check if it is a terminal bond...and not in between!
						var atomList = original.getConnectedAtomsList(atom);
						var atomCount = 0;
						foreach (var iAtom in atomList.ToWindowsEnumerable<IAtom>())
						{
							// dont check
							if (iAtom.getSymbol().Equals("H"))
							{
								continue;
							}
							else
							{
								atomCount++;
							}
						}
						// not a terminal atom...so skip it!
						if (atomCount > 1)
						{
							continue;
						}

						temp = checkForCompleteNeutralLoss(original, atom,
						                                   neutralLossMass);
						if (temp.getAtomCount() > 0)
						{
							if (ConnectivityChecker.isConnected(temp))
							{
								ret.Add(temp);
							}
							else
							{
								var set = ConnectivityChecker
									.partitionIntoMolecules(temp);
								foreach (var molecule in set.atomContainers().ToWindowsEnumerable<IAtomContainer>())
								{
									ret.Add(molecule);
								}
							}
							// create a atom container
							temp = new AtomContainer();
						}
					}
				}
			}
			return ret;
		}

		/**
		 * Check for the other atoms nearby in the fragment.
		 * 
		 * @param candidateOxygen
		 *            the candidate oxygen atom
		 * @param frag
		 *            the frag
		 * @param proton
		 *            the proton
		 * 
		 * @return true, if successful
		 * @throws CloneNotSupportedException
		 * @throws CDKException
		 */

		private IAtomContainer checkForCompleteNeutralLoss(IAtomContainer frag, IAtom candidateAtom, double neutralLossMass)
		{
			IAtomContainer ret = new AtomContainer();

			// create a copy from the original fragment
			var part = new List<IBond>();
			part = traverse(frag, candidateAtom, part);
			var fragCopy = makeAtomContainer(candidateAtom, part);

			// set properties again
			var properties = frag.getProperties();
			fragCopy.setProperties(properties);

			// now get the other atoms from the neutral loss
			var atomsToFind = new List<string>();
			var addHydrogen = false;
			// one hydrogen is lost with the neutral loss
			if (neutralLoss[neutralLossMass].HydrogenDifference == -1)
			{
				foreach (var isotope in neutralLoss[neutralLossMass].ElementalComposition.isotopes().ToWindowsEnumerable<IIsotope>())
				{
					var c = neutralLoss[neutralLossMass].ElementalComposition.getIsotopeCount(isotope);

					for (var i = 0; i < c; i++)
					{
						atomsToFind.Add(isotope.getSymbol());
					}
				}
			}
			else
			{
				foreach (var isotope in neutralLoss[neutralLossMass].TopoFragment.isotopes().ToWindowsEnumerable<IIsotope>())
				{
					var c = neutralLoss[neutralLossMass].ElementalComposition.getIsotopeCount(isotope);

					for (var i = 0; i < c; i++)
					{
						atomsToFind.Add(isotope.getSymbol());
					}
					addHydrogen = true;
				}
			}

			// at most 2 bonds between the oxygen and other atoms (at most 1 H and 2
			// C)
			var count = neutralLoss[neutralLossMass].Distance;
			// list storing all atoms to be removed later on if complete neutral
			// loss was found
			var foundAtoms = new List<IAtom>();
			// list storing all bonds to remove
			var foundBonds = new List<IBond>();
			// list storing all bonds already checked
			var checkedBonds = new List<IBond>();
			// list storing all checked atoms
			var checkedAtoms = new List<IAtom>();
			// queue storing all bonds to check for a particular distance
			var bondQueue = new List<IBond>();
			// List storing all bonds to be checked for the next distance
			var bondsFurther = new List<IBond>();
			// get all bonds from this atom distance = 1

			var bondList = fragCopy.getConnectedBondsList(candidateAtom);
			foreach (var bond in bondList.ToWindowsEnumerable<IBond>())
			{
				if (bond != null)
				{
					bondQueue.Add(bond);
				}
			}

			var hydrogenStartAtom = neutralLoss[neutralLossMass].HydrogenOnStartAtom;
			var firstBonds = true;

			while (count > 0)
			{
				IBond currentBond = null;
				if (bondQueue.Count > 0)
				{
					currentBond = bondQueue[bondQueue.Count - 1];
					bondQueue.RemoveAt(bondQueue.Count - 1);
				}

				// check for already tried bonds
				if (checkedBonds.Contains(currentBond) && currentBond != null)
				{
					continue;
				}
				else if (currentBond != null)
				{
					checkedBonds.Add(currentBond);
				}

				if (currentBond != null)
				{
					foreach (var atom in currentBond.atoms().ToWindowsEnumerable<IAtom>())
					{
						// check for already tried atoms
						if (checkedAtoms.Contains(atom))
						{
							continue;
						}
						else
						{
							checkedAtoms.Add(atom);
						}

						if (firstBonds && atom.getSymbol().Equals("H"))
						{
							hydrogenStartAtom--;
						}

						// thats the starting atom
						if (atom.getSymbol().Equals(candidateAtom.getSymbol()))
						{
							var removed = atomsToFind.Remove(candidateAtom.getSymbol());
							if (removed)
							{
								foundAtoms.Add(atom);
								// remove bond
								if (!foundBonds.Contains(currentBond))
								{
									foundBonds.Add(currentBond);
								}
							}
								// this bond has to be removed
							else if (!foundBonds.Contains(currentBond) && atomsToFind.Contains(atom.getSymbol()))
							{
								foundBonds.Add(currentBond);
							}

							continue;
						}
						// found atom...remove it from the atoms to find list
						// do not remove atoms from ring
						if (atomsToFind.Contains(atom.getSymbol()) && !allRings.contains(atom) && atomsToFind.Count > 0)
						{
							var removed = atomsToFind.Remove(atom.getSymbol());
							if (removed)
							{
								foundAtoms.Add(atom);
							}
							else
							{
								continue;
							}

							if (!foundBonds.Contains(currentBond))
							{
								foundBonds.Add(currentBond);
							}

							continue;
						}

						// only walk along C-Atoms!
						if (!atom.getSymbol().Equals("C"))
						{
							continue;
						}

						// get new bonds
						var bondsToAddTemp = fragCopy.getConnectedBondsList(atom);

						foreach (var bond in bondsToAddTemp.ToWindowsEnumerable<IBond>())
						{
							if (bond != null)
							{
								bondsFurther.Add(bond);
							}
						}
					}
				}

				// break condition
				if (currentBond == null && bondQueue.Count == 0 && bondsFurther.Count == 0)
				{
					break;
				}

				// now check if the queue is empty...checked all bonds in this
				// distance
				if (bondQueue.Count == 0)
				{
					count--;
					// set new queue data
					foreach (var bond in bondsFurther)
					{
						if (bond != null)
						{
							bondQueue.Add(bond);
						}
					}
					// reinitialize
					bondsFurther = new List<IBond>();
					// the initially connected bonds are all checked!
					firstBonds = false;
				}
			}

			// found complete neutral loss
			if (atomsToFind.Count == 0)
			{
				foreach (var atom in foundAtoms)
				{
					fragCopy.removeAtomAndConnectedElectronContainers(atom);
				}

				// TODO add hydrogen somewhere
				if (addHydrogen)
				{
					var props = fragCopy.getProperties();
					props.put("hydrogenAddFromNL", "1");
					fragCopy.setProperties(props);
				}

				// add fragment to return if enough H were connected and fragment is
				// still connected
				if (hydrogenStartAtom <= 0)
				{
					ret.add(fragCopy);
				}
			}

			return ret;
		}

		/**
		 * Traverse like in a the real fragmenter. The main purpose is to create a
		 * copy of of the old fragment
		 * 
		 * @param atomContainer
		 *            the atom container
		 * @param atom
		 *            the atom
		 * @param bondList
		 *            the bond list
		 * 
		 * @return the list< i bond>
		 */

		private List<IBond> traverse(IAtomContainer atomContainer, IAtom atom, List<IBond> bondList)
		{
			var connectedBonds = atomContainer.getConnectedBondsList(atom);

			foreach (var aBond in connectedBonds.ToWindowsEnumerable<IBond>())
			{
				if (bondList.Contains(aBond))
				{
					continue;
				}
				bondList.Add(aBond);
				var nextAtom = aBond.getConnectedAtom(atom);
				if (atomContainer.getConnectedAtomsCount(nextAtom) == 1)
				{
					continue;
				}
				traverse(atomContainer, nextAtom, bondList);
			}
			return bondList;
		}

		private IAtomContainer makeAtomContainer(IAtom atom, List<IBond> parts)
		{
			IAtomContainer partContainer = new AtomContainer();
			partContainer.addAtom(atom);
			foreach (var aBond in parts)
			{
				foreach (var bondedAtom in aBond.atoms().ToWindowsEnumerable<IAtom>())
				{
					if (!partContainer.contains(bondedAtom))
					{
						partContainer.addAtom(bondedAtom);
					}
				}
				partContainer.addBond(aBond);
			}
			return partContainer;
		}
	}
}