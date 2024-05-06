using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public sealed class DbCommandExecuteCounterKey : DbCommandCounterKey
{
    public override string DisplayName => "Database command with parameters execute counter";

    public DbCommandExecuteCounterKey(string commandText, IEnumerable<CounterDbCommandParameter> parameters)
        : base(commandText, parameters)
    {
    }

    public DbCommandExecuteCounterKey(string commandText, params CounterDbCommandParameter[] parameters)
        : this(commandText, parameters.AsEnumerable())
    {
    }

    public static DbCommandExecuteCounterKey CreateFrom(DbCommand dbCommand) =>
        new(
            dbCommand.CommandText,
            dbCommand.Parameters
                .OfType<DbParameter>()
                .Select(parameter => new CounterDbCommandParameter(parameter.ParameterName, parameter.Value)));
}
