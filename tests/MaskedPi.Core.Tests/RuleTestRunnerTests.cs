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
                Source = "fixed",
                Profile = "common"
            }
        };

        var cases = new List<RuleTestCase>
        {
            new()
            {
                CaseName = "phone",
                Input = "090-1111-2222",
                ExpectedOutput = "[電話番号]",
                ExpectedHitRules = new List<string> { "Phone-General" },
                Profile = "common"
            }
        };

        var results = runner.Run(rules, cases);

        Assert.Single(results);
        Assert.True(results[0].Passed);
    }

    [Fact]
    public void Run_UsesCaseProfile_WhenSpecified()
    {
        var service = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        var runner = new RuleTestRunner(service);

        var rules = new List<RuleDefinition>
        {
            new() { Name = "TaxOnly", Enabled = true, Category = RuleCategory.LocalRule, Pattern = "納税通知書番号", Replacement = "[税務]", Priority = 10, Profile = "tax" }
        };

        var cases = new List<RuleTestCase>
        {
            new() { CaseName = "tax", Input = "納税通知書番号", ExpectedOutput = "[税務]", ExpectedHitRules = new() { "TaxOnly" }, Profile = "tax" },
            new() { CaseName = "welfare", Input = "納税通知書番号", ExpectedOutput = "納税通知書番号", ExpectedHitRules = new(), Profile = "welfare" }
        };

        var results = runner.Run(rules, cases);

        Assert.True(results[0].Passed);
        Assert.True(results[1].Passed);
        Assert.Equal("tax", results[0].Profile);
        Assert.Equal("welfare", results[1].Profile);
    }
}
