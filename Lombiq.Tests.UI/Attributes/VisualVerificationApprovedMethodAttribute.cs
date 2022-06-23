using System;

namespace Lombiq.Tests.UI.Attributes;

/// <summary>
/// This attribute is used to mark the VisualVerificationApproved methods in the call stack to reach the consumer method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class VisualVerificationApprovedMethodAttribute : Attribute
{
}
