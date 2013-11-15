using System;
using System.Globalization;
using System.Threading;

namespace MetFragNETTests
{
    public class CultureChanger : IDisposable
    {
        private readonly CultureInfo originalCulture;

        public CultureChanger(string culture)
            : this(new CultureInfo(culture))
        {
        }

        public CultureChanger(CultureInfo culture)
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}