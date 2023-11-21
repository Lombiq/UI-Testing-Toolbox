using System;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class YamlNodeExtensions
{
    /// <summary>
    /// Sets <see cref="YamlScalarNode.Value"/> to the given value.
    /// </summary>
    /// <param name="value">The value to set <see cref="YamlScalarNode.Value"/> to.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the supplied YamlNode can't be cast to YamlScalarNode and thus can't have a value set.
    /// </exception>
    public static void SetValue(this YamlNode yamlNode, string value)
    {
        if (yamlNode is not YamlScalarNode)
        {
            throw new ArgumentException(
                "The supplied YamlNode can't be cast to YamlScalarNode and thus can't have a value set.", nameof(yamlNode));
        }

        ((YamlScalarNode)yamlNode).Value = value;
    }
}
