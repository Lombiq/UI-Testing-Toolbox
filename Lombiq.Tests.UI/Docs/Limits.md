# Limits on parallel test execution

Tests need to use ports for running the Orchard Core app and SMTP server with its web UI if necessary. To allow parallelized test execution in the same process, as well as [executing tests in multiple processes](#multi-process) the interval of available ports need to be fixed. The current limits are as following:

- Up to 100 concurrent tests in the same process.
- Up to 10 concurrent processes on the same machine.

Anything above these will cause random port collisions.
