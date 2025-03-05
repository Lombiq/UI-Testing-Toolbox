// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type,
// member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Security",
    "MA0009:Add regex evaluation timeout",
    Justification = "Regexes in this project don't use user input.")]
