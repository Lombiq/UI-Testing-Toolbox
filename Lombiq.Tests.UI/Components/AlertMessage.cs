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

    public DataProvider<bool, TOwner> IsSuccess =>
        GetOrCreateDataProvider("success state", GetIsSuccess);

    private bool GetIsSuccess() =>
        Attributes.Class.Value.Contains("message-success");
}
