using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public sealed class DbExecuteCounterKey : DbCommandCounterKey
{
    public DbExecuteCounterKey(string commandText, IEnumerable<KeyValuePair<string, object>> parameters)
        : base(commandText, parameters)
    {
    }

    public static DbExecuteCounterKey CreateFrom(DbCommand dbCommand) =>
        new(
            dbCommand.CommandText,
            dbCommand.Parameters
                .OfType<DbParameter>()
                .Select(parameter => new KeyValuePair<string, object>(parameter.ParameterName, parameter.Value)));
}
