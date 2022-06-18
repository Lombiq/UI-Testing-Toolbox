using System;

namespace Lombiq.Tests.UI.Attributes;

/// <summary>
/// This attribute is used to mark VisualVerificationApproved methods in the call stack.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class VisualVerificationApprovedMethodAttribute : Attribute
{
}
