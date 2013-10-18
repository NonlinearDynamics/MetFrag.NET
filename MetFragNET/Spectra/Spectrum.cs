using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MetFragNET.Spectra
{
	public class Spectrum
	{
		private readonly double exactMass;
		private readonly int mode;

		public Spectrum(string peaks, double exactMass, int mode)
		{
			Peaks = ParsePeaks(peaks);
			this.exactMass = exactMass;
			this.mode = mode;
		}

		public int Mode
		{
			get { return mode; }
		}

		public double ExactMass
		{
			get { return exactMass; }
		}

		public IEnumerable<Peak> Peaks { get; set; }

		private IEnumerable<Peak> ParsePeaks(string peakString)
		{
			//replace all "," with "."
			peakString = peakString.Replace(",", ".");


			// Compile regular expression --> remove spaces in front and end of lines
			var pattern = new Regex(@"^[ \t]+|[ \t]+$");
			//replace all tabs whitespaces at end of the line
			var patternTabs = new Regex(@"[\t]+");

			var parsedPeaks = new List<Peak>();
			var lines = peakString.Split('\n');

			foreach (var line in lines)
			{
				//skip comment
				if (line.StartsWith("#"))
				{
					continue;
				}

				// Replace all occurrences of pattern in input
				var output = pattern.Replace(line, "");
				output = output.Replace("  ", " ");

				output = patternTabs.Replace(line, " ");

				var array = output.Split(' ');
				var arrayClean = new string[3];

				var count = 0;
				for (var i = 0; i < array.Length; i++)
				{
					if (array[i] == null || array[i].Equals(""))
					{
						continue;
					}
					else
					{
						arrayClean[count] = array[i];
						count++;
					}
				}

				if (count >= 2)
				{
					parsedPeaks.Add(new Peak(Double.Parse(arrayClean[0]), Double.Parse(arrayClean[1])));
				}
			}

			return parsedPeaks;
		}
	}
}