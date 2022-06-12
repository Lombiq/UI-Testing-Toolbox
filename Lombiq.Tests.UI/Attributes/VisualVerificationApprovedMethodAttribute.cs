using System;

namespace Lombiq.Tests.UI.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class VisualVerificationApprovedMethodAttribute : Attribute
{
}
