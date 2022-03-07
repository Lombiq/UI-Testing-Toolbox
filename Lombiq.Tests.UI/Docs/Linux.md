# Linux-specific considerations



## Global NPM vs Userspace NPM via Node Version Manager


As Linux has a stricter access policy you may want to install NPM in the userspace so you can still install global packages (e.g. html-validate) without sudoing. The easiest way to do this is via NVM. If you don't have NVM yet, [follow the guide here](https://github.com/Lombiq/NPM-Targets/tree/dev#global-npm-vs-userspace-npm-via-node-version-manager-on-linux).

As of writing this document, [Atata doesn't run bash as login shell](https://github.com/atata-framework/atata-cli/issues/1) so NVM wouldn't be loaded and the executables of globally installed packages' CLI executables won't be available. There are two workarounds for this:
* Run your dev environment inside an NVM scope. For example: `nvm exec default rider`, `nvm exec default code`, etc.
* Create a proxy command for each using the function from the NVM setup guide above, e.g.: `proxy-nvm-command html-validate`.

## SQL Server Usage


Since 2017 Microsoft SQL Server is available on [RHEL](https://redhat.com/rhel/), [SUSE](https://www.suse.com/products/server/) and [Ubuntu](https://ubuntu.com/) as well as a [Linux-based Docker image](https://hub.docker.com/_/microsoft-mssql-server) that you can run on any OS.

We suggest using the Docker image even on the aforementioned OSes. It reduces the number of unknowns and moving parts in your setup, it's easier to reset if something goes wrong, and that's what we support. We have a guide for setting up SQL Server for Linux on Docker [here](Configuration.md#using-sql-server-from-a-docker-container).
