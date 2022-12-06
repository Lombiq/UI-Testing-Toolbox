using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public class DbReaderReadCounterKey : DbCommandCounterKey
{
    public DbReaderReadCounterKey(string commandText, IEnumerable<KeyValuePair<string, object>> parameters)
        : base(commandText, parameters)
    {
    }

    public static DbReaderReadCounterKey CreateFrom(DbCommand dbCommand) =>
        new(
            dbCommand.CommandText,
            dbCommand.Parameters
                .OfType<DbParameter>()
                .Select(parameter => new KeyValuePair<string, object>(parameter.ParameterName, parameter.Value)));
}
