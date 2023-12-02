namespace Lombiq.Tests.UI.SecurityScanning;

/// <summary>
/// Controls how likely ZAP is to report potential vulnerabilities. See <see
/// href="https://www.zaproxy.org/docs/desktop/ui/dialogs/scanpolicy/#threshold">the official docs</see>.
/// </summary>
public enum ScanRuleThreshold
{
    Off,
    Default,
    Low,
    Medium,
    High,
}

/// <summary>
/// Controls the number of attacks that ZAP will perform. See <see
/// href="https://www.zaproxy.org/docs/desktop/ui/dialogs/scanpolicy/#strength">the official docs</see>. 
/// </summary>
public enum ScanRuleStrength
{
    Default,
    Low,
    Medium,
    High,
    Insane,
}
