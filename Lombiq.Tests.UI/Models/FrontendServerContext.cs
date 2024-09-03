using CliWrap;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models;

public class FrontendServerContext
{
    public int Port { get; set; }
    public CommandTask<CommandResult> Task { get; set; }
    public Func<Task> Stop { get; set; }
}
