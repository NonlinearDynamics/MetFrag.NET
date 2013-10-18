using System;

namespace MetFragNET.Spectra
{
	public class Peak
	{
		public Peak(double mass, double intensity)
		{
			Mass = mass;
			Intensity = intensity;
		}

		public double Mass { get; set; }
		public double Intensity { get; set; }

		public int NominalMass
		{
			get { return (int)Math.Round(Mass); }
		}
	}
}