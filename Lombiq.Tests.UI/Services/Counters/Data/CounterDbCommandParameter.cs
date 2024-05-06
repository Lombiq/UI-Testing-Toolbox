namespace Lombiq.Tests.UI.Services.Counters.Data;

public class CounterDbCommandParameter(string name, object value)
{
    public string Name { get; set; } = name;
    public object Value { get; set; } = value;
}
