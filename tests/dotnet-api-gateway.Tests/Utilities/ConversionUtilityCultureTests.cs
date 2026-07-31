using System;
using System.Globalization;
using DotNetApiGateway.Utilities;
using Xunit;

namespace DotNetApiGateway.Tests.Utilities
{
    public class ConversionUtilityCultureTests
    {
        [Fact]
        public void NumericAndDateParsingUsesInvariantCulture_UnderTurkishCulture()
        {
            // Preserve original culture
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                // Switch to Turkish culture which uses ',' as decimal separator
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

                // Int parsing with invariant format
                int intResult = ConversionUtility.ToInt("1234");
                Assert.Equal(1234, intResult);

                // Double parsing with invariant decimal separator ('.')
                double doubleResult = ConversionUtility.ToDouble("1.5");
                Assert.Equal(1.5, doubleResult);

                // DateTime parsing with ISO format
                DateTime dateResult = ConversionUtility.ToDateTime("2020-12-31");
                Assert.Equal(new DateTime(2020, 12, 31), dateResult);
            }
            finally
            {
                // Restore original culture
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}
