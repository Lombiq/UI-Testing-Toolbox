using Atata;
using System.Linq;

namespace Lombiq.Tests.UI.Components;

[ControlDefinition(ContainingClass = "alert")]
public class AlertMessage<TOwner> : Control<TOwner>
    where TOwner : PageObject<TOwner>
{
    [UseParentScope]
    [GetsContentFromSource(ContentSource.FirstChildTextNode)]
    public Text<TOwner> Text { get; private set; }

    public ValueProvider<bool, TOwner> IsSuccess =>
        CreateValueProvider("success state", GetIsSuccess);

    private bool GetIsSuccess() =>
        DomClasses.Value.Contains("message-success");
}
