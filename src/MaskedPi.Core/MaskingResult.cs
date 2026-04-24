namespace MaskedPi.Core;

/// <summary>
/// マスキング処理結果。
/// </summary>
public sealed class MaskingResult
{
    public string MaskedText { get; init; } = string.Empty;
    public List<ReplacementRecord> Replacements { get; init; } = new();
    public Dictionary<RuleCategory, int> CategoryCounts { get; init; } = new();
    public Dictionary<string, int> RuleHitCounts { get; init; } = new();

    public int TotalReplacements => Replacements.Count;
}
