using Microsoft.CodeAnalysis.Sarif;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanResult
{
    public string ReportsDirectoryPath { get; }
    public SarifLog SarifLog { get; }

    public SecurityScanResult(string reportsDirectoryPath, SarifLog sarifLog)
    {
        ReportsDirectoryPath = reportsDirectoryPath;
        SarifLog = sarifLog;
    }
}
