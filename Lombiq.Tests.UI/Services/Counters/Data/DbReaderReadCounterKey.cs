using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public class DbReaderReadCounterKey : DbCommandCounterKey
{
    public override string DisplayName => "Database reader read counter";

    public DbReaderReadCounterKey(string commandText, IEnumerable<CounterDbCommandParameter> parameters)
        : base(commandText, parameters)
    {
    }

    public DbReaderReadCounterKey(string commandText, params CounterDbCommandParameter[] parameters)
        : this(commandText, parameters.AsEnumerable())
    {
    }

    public static DbReaderReadCounterKey CreateFrom(DbCommand dbCommand) =>
        new(
            dbCommand.CommandText,
            dbCommand.Parameters
                .OfType<DbParameter>()
                .Select(parameter => new CounterDbCommandParameter(parameter.ParameterName, parameter.Value)));
}
