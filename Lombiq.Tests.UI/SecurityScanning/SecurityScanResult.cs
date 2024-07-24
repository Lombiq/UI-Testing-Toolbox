using Microsoft.CodeAnalysis.Sarif;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanResult
{
    public string ReportsDirectoryPath { get; }
    public string ZapLogPath { get; set; }
    public SarifLog SarifLog { get; }

    public SecurityScanResult(string reportsDirectoryPath, string zapLogPath, SarifLog sarifLog)
    {
        ReportsDirectoryPath = reportsDirectoryPath;
        ZapLogPath = zapLogPath;
        SarifLog = sarifLog;
    }
}
