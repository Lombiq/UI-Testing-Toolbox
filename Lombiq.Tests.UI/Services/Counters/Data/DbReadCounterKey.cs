using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public class DbReadCounterKey : DbCommandCounterKey
{
    public DbReadCounterKey(string commandText, IEnumerable<KeyValuePair<string, object>> parameters)
        : base(commandText, parameters)
    {
    }

    public static DbReadCounterKey CreateFrom(DbCommand dbCommand) =>
        new(
            dbCommand.CommandText,
            dbCommand.Parameters
                .OfType<DbParameter>()
                .Select(parameter => new KeyValuePair<string, object>(parameter.ParameterName, parameter.Value)));
}
