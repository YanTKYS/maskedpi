using MaskedPi.Core;

namespace MaskedPi.Core.Tests;

public sealed class RuleTestRunnerTests
{
    [Fact]
    public void Run_ReturnsPassForMatchingCase()
    {
        var service = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        var runner = new RuleTestRunner(service);

        var rules = new List<RuleDefinition>
        {
            new()
            {
                Name = "Phone-General",
                Enabled = true,
                Category = RuleCategory.Phone,
                Pattern = "(?:0\\d{1,4}-?\\d{1,4}-?\\d{3,4})",
                Replacement = "[電話番号]",
                Priority = 40,
                Source = "fixed"
            }
        };

        var cases = new List<RuleTestCase>
        {
            new()
            {
                CaseName = "phone",
                Input = "090-1111-2222",
                ExpectedOutput = "[電話番号]",
                ExpectedHitRules = new List<string> { "Phone-General" }
            }
        };

        var results = runner.Run(rules, cases);

        Assert.Single(results);
        Assert.True(results[0].Passed);
    }
}
