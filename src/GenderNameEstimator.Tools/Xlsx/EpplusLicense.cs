using System.Text.Json;

using OfficeOpenXml;

namespace GenderNameEstimator.Tools.Xlsx;

public static class EpplusLicense
{
    private static readonly object SyncRoot = new();
    private static bool _configured;

    public static void Configure()
    {
        lock (SyncRoot)
        {
            if (_configured)
            {
                return;
            }

            var environmentLicense = Environment.GetEnvironmentVariable("EPPlusLicense");
            const string commercialPrefix = "Commercial:";
            if (environmentLicense?.StartsWith(commercialPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                ExcelPackage.License.SetCommercial(environmentLicense[commercialPrefix.Length..]);
                _configured = true;
                return;
            }

            using var stream = typeof(EpplusLicense).Assembly.GetManifestResourceStream("GnE.appsettings.Secrets.json");
            if (stream is not null)
            {
                using var configuration = JsonDocument.Parse(stream);
                if (configuration.RootElement.TryGetProperty("EPPlus", out var epplus)
                    && epplus.TryGetProperty("LicenseKey", out var licenseKey)
                    && !string.IsNullOrWhiteSpace(licenseKey.GetString()))
                {
                    ExcelPackage.License.SetCommercial(licenseKey.GetString()!);
                    _configured = true;
                    return;
                }
            }

            throw new InvalidOperationException(
                "EPPlus commercial licensing is not configured. Set EPPlusLicense=Commercial:<key> "
                + "or build the Richardson Oliver distribution with its injected settings.");
        }
    }
}
