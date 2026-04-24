namespace MaskedPi.Core;

/// <summary>
/// ルールJSONのルート。
/// </summary>
public sealed class RuleSet
{
    public List<RuleDefinition> Rules { get; set; } = new();
}
