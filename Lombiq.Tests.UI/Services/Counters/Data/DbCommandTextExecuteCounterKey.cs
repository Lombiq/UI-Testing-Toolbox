using System;
using System.Data.Common;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public sealed class DbCommandTextExecuteCounterKey : DbCommandCounterKey
{
    public override string DisplayName => "Database command execute counter";

    public DbCommandTextExecuteCounterKey(string commandText)
        : base(commandText, Enumerable.Empty<CounterDbCommandParameter>())
    {
    }

    public static DbCommandTextExecuteCounterKey CreateFrom(DbCommand dbCommand) =>
        new(dbCommand.CommandText);

    public override bool Equals(ICounterKey other)
    {
        if (ReferenceEquals(this, other)) return true;

        return other is DbCommandTextExecuteCounterKey otherKey
            && GetType() == otherKey.GetType()
            && string.Equals(CommandText, otherKey.CommandText, StringComparison.OrdinalIgnoreCase);
    }
}
