# Projects in the UI Testing Toolbox

The UI Testing Toolbox encompasses the following projects:

- `Lombiq.Tests.UI`: Contains all features used for UI testing. This is the project that your test projects need to reference.
- [`Lombiq.Tests.UI.AppExtensions`](../../Lombiq.Tests.UI.AppExtensions/Readme.md): UI testing-related configuration extensions for the web app under test. This is the project that your web project needs to reference.
- [`Lombiq.Tests.UI.Shortcuts`](../../Lombiq.Tests.UI.Shortcuts/Readme.md): Provides some useful shortcuts for common operations that UI tests might want to do or check, e.g. turning features on or off, or logging in users. If you utilize these shortcuts then your web projects needs to reference this project to load as an Orchard Core module.
- [`Lombiq.Tests.UI.Samples`](../../Lombiq.Tests.UI.Samples/Readme.md): Example UI testing project.
