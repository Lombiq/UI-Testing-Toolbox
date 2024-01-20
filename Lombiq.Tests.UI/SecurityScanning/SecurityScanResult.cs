using Microsoft.CodeAnalysis.Sarif;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanResult(string reportsDirectoryPath, SarifLog sarifLog)
{
    public string ReportsDirectoryPath { get; } = reportsDirectoryPath;
    public SarifLog SarifLog { get; } = sarifLog;
}
