// Do you know static code analysis? It can make your life easier and your code better by automatically checking for all
// kinds of formatting mistakes, potential bugs, and security issues. However, one of the analyzers don't really
// understand UI tests so we have to disable it here.
// If you'd like to learn more about analyzers check out our project: https://github.com/Lombiq/.NET-Analyzers.

using System.Diagnostics.CodeAnalysis;

// This is disabling this analyzer: https://rules.sonarsource.com/csharp/RSPEC-2699.
[assembly: SuppressMessage(
    "Minor Code Smell",
    "S2699:Add at least one assertion to this test case.",
    Justification = "Assertions are made implicitly in UI tests.",
    Scope = "module")]

// NEXT STATION: Let's start doing something with testing, actually, and configure how tests should work. Head over to
// UITestBase.cs.
