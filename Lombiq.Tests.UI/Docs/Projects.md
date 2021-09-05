# Projects in the UI Testing Toolbox



The UI Testing Toolbox encompasses the following projects:

- `Lombiq.Tests.UI`: Contains all features used for UI testing. This is the project that your test projects need to reference.
- `Lombiq.Tests.UI.AppExtensions`: UI testing-related configuration extensions for the web app under test. This is the project that your web project need to reference.
- `Lombiq.Tests.UI.Shortcuts`: Provides some useful shortcuts for common operations that UI tests might want to do or check, e.g. turning features on or off, or logging in users. If you utilize these shortcuts then your web projects needs to reference this project to load as an Orchard Core module.

