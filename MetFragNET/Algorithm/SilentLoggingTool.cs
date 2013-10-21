using java.lang;
using org.openscience.cdk.tools;

namespace MetFragNET.Algorithm
{
	public class SilentLoggingTool : ILoggingTool
	{
		public static object create(Class sourceClass)
		{
			return new SilentLoggingTool();
		}

		public void error(object obj, params object[] objarr)
		{
		}

		public void debug(object obj)
		{
		}

		public void error(object obj)
		{
		}

		public void debug(object obj, params object[] objarr)
		{
		}

		public void info(object obj, params object[] objarr)
		{
		}

		public void warn(object obj)
		{
		}

		public void warn(object obj, params object[] objarr)
		{
		}

		public void info(object obj)
		{
		}

		public bool isDebugEnabled()
		{
			return false;
		}

		public void dumpSystemProperties()
		{
		}

		public void setStackLength(int i)
		{
		}

		public void dumpClasspath()
		{
		}

		public void fatal(object obj)
		{
		}
	}

}