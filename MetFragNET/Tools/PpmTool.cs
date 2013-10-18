namespace NldMetFrag_DotNet.Tools
{
	public class PpmTool
	{
		public static double GetPPMDeviation(double peak, double ppm)
		{
			//calculate the allowed error for the given peak m/z in Th
			return (peak / 1000000.0) * ppm;
		}
	}
}