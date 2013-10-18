namespace MetFragNET.Algorithm
{
	public struct FragmentationConfig
	{
		private readonly int mode;
		private readonly double mzabs;
		private readonly double mzppm;

		public FragmentationConfig(double mzabs, double mzppm, int mode)
		{
			this.mzabs = mzabs;
			this.mzppm = mzppm;
			this.mode = mode;
		}

		// These are built in configuration options - the user can't change them through the API,
		// but they are useful to keep as configurable.
		public int TreeDepth
		{
			get { return 2; }
		}

		// These are the things the user can change through the API
		public double Mzabs
		{
			get { return mzabs; }
		}

		public double Mzppm
		{
			get { return mzppm; }
		}

		public int Mode
		{
			get { return mode; }
		}
	}
}