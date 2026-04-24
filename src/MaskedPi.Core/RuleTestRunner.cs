namespace MaskedPi.Core;

/// <summary>
/// JSON定義されたテストケースを一括実行する。
/// </summary>
public sealed class RuleTestRunner
{
    private readonly MaskingService _maskingService;

    public RuleTestRunner(MaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    public List<RuleTestCaseResult> Run(IReadOnlyCollection<RuleDefinition> rules, IReadOnlyCollection<RuleTestCase> cases)
    {
        var results = new List<RuleTestCaseResult>();

        foreach (var testCase in cases)
        {
            var masked = _maskingService.Mask(testCase.Input, rules);
            var expectedRules = new HashSet<string>(testCase.ExpectedHitRules, StringComparer.Ordinal);
            var actualRules = masked.Replacements.Select(r => r.RuleName).ToHashSet(StringComparer.Ordinal);

            var outputMatched = string.IsNullOrEmpty(testCase.ExpectedOutput) || masked.MaskedText == testCase.ExpectedOutput;
            var rulesMatched = expectedRules.Count == 0 || expectedRules.IsSubsetOf(actualRules);

            results.Add(new RuleTestCaseResult
            {
                CaseName = testCase.CaseName,
                Passed = outputMatched && rulesMatched,
                ExpectedOutput = testCase.ExpectedOutput,
                ActualOutput = masked.MaskedText,
                ExpectedHitRules = testCase.ExpectedHitRules,
                ActualHitRules = actualRules.OrderBy(x => x).ToList(),
                Description = testCase.Description
            });
        }

        return results;
    }
}

public sealed class RuleTestCaseResult
{
    public string CaseName { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string ExpectedOutput { get; init; } = string.Empty;
    public string ActualOutput { get; init; } = string.Empty;
    public List<string> ExpectedHitRules { get; init; } = new();
    public List<string> ActualHitRules { get; init; } = new();
    public string Description { get; init; } = string.Empty;
}
