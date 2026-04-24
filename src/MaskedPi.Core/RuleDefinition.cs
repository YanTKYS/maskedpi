using System.Text.Json.Serialization;

namespace MaskedPi.Core;

/// <summary>
/// JSON から読み込むマスキングルール。
/// </summary>
public sealed class RuleDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuleCategory Category { get; set; } = RuleCategory.Other;

    public string Pattern { get; set; } = string.Empty;
    public string Replacement { get; set; } = "[MASK]";
    public int Priority { get; set; } = 100;
    public bool IgnoreCase { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SampleText { get; set; }
}
