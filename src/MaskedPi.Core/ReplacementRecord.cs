namespace MaskedPi.Core;

/// <summary>
/// 置換1件の情報。監査やデバッグ用途で保持する。
/// </summary>
public sealed class ReplacementRecord
{
    public required string RuleName { get; init; }
    public required RuleCategory Category { get; init; }
    public required int StartIndex { get; init; }
    public required int Length { get; init; }
    public required string OriginalText { get; init; }
    public required string ReplacementText { get; init; }
}
