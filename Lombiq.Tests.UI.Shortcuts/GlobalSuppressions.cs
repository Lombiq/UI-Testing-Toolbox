using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Security",
    "SCS0027:Potential Open Redirect vulnerability was found where \'{0}\' in \'{1}\' may be tainted by user-controlled data from \'{2}\' in method \'{3}\'.",
    Justification = "This is safe because the module is only available in UI tests.",
    Scope = "module")]
