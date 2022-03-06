# Linux-specific considerations



## Global NPM vs Userspace NPM via Node Version Manager


As Linux has a stricter access policy you may want to install NPM in the userspace so you can still install global packages (e.g. html-validate) without sudoing. The easiest way to do this is via NVM. If you don't have NVM yet, [follow the guide here](https://github.com/Lombiq/NPM-Targets/tree/dev#global-npm-vs-userspace-npm-via-node-version-manager-on-linux).

As of writing this document, [Atata doesn't run bash as login shell](https://github.com/atata-framework/atata-cli/issues/1) so NVM wouldn't be loaded and the executables of globally installed packages' CLI executables won't be available. There are two workarounds for this:
* Run your dev environment inside an NVM scope. For example: `nvm exec default rider`, `nvm exec default code`, etc.
* Create a proxy command for each using the function from the NVM setup guide above, e.g.: `proxy-nvm-command html-validate`.
