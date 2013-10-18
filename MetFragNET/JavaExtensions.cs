using System.Collections.Generic;
using java.lang;

namespace MetFragNET
{
	public static class JavaExtensions
	{
		public static IEnumerable<T> ToWindowsEnumerable<T>(this Iterable iterable)
		{
			var iterator = iterable.iterator();

			while (iterator.hasNext())
			{
				yield return (T)iterator.next();
			}
		}
	}
}