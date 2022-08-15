using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Security",
    "SCS0027: Potential Open Redirect vulnerability was found.",
    Justification = "This is safe because the module is only available in UI tests.",
    Scope = "module")]
