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

    // 将来の業務別ルールセット切替用。現時点では common を標準とする。
    public string Profile { get; set; } = "common";

    // 運用補足。例: 誤検出を避ける制約、帳票名など。
    public string? Notes { get; set; }

    // ルールの由来。例: common/local-dictionary/manual。
    public string? Source { get; set; }

    // 検索やグルーピングのためのタグ。
    public List<string> Tags { get; set; } = new();
}
