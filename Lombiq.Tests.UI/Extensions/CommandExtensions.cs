using CliWrap.EventStream;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Executes a <see cref="Command"/> as a dotnet.exe command that starts a long-running application, and waits
        /// for the app to be started.
        /// </summary>
        public static async Task ExecuteDotNetApplicationAsync(
            this Command command,
            Action<StandardErrorCommandEvent> stdErrHandler = default,
            CancellationToken cancellationToken = default)
        {
            await using var enumerator = command.ListenAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current is StandardOutputCommandEvent stdOut &&
                    stdOut.Text.Contains("Application started. Press Ctrl+C to shut down.", StringComparison.InvariantCulture))
                {
                    return;
                }
                else if (enumerator.Current is StandardErrorCommandEvent stdErr)
                {
                    stdErrHandler?.Invoke(stdErr);
                }
            }
        }
    }
}
