# Linux-specific considerations



## Global NPM vs Userspace NPM via Node Version Manager


As Linux has a stricter access policy you may want to install NPM in the userspace so you can still install global packages (e.g. html-validate) without sudoing. The easiest way to do this is via NVM. If you don't have NVM yet, [follow the guide here](https://github.com/Lombiq/NPM-Targets/tree/dev#global-npm-vs-userspace-npm-via-node-version-manager-on-linux).

This library configures processes launched by Atata (via `Atata.Cli.ProgramCli`) to use Bash as login shell on non-Windows systems by default, like this:

```csharp
ProgramCli.DefaultShellCliCommandFactory = OSDependentShellCliCommandFactory
    .UseCmdForWindows()
    .UseForOtherOS(new BashShellCliCommandFactory("-login"));
```

If your project has different requirements, you can change it in your `OrchardCoreUITestBase.ExecuteTestAfterSetupAsync` implementation. Set a new value in the configuration function you pass to `base.ExecuteTestAsync`.


## SQL Server Usage

Since 2017, Microsoft SQL Server is available on [RHEL](https://redhat.com/rhel/), [SUSE](https://www.suse.com/products/server/) and [Ubuntu](https://ubuntu.com/) as well as a [Linux-based Docker image](https://hub.docker.com/_/microsoft-mssql-server) that you can run on any OS.

We suggest using the Docker image even on those OSes. It reduces the number of unknowns and moving parts in your setup, it's easier to reset if something goes wrong, and that's what we support. We have a guide for setting up SQL Server for Linux on Docker [here](Configuration.md#using-sql-server-from-a-docker-container).
