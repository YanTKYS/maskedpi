using System.Text;
using System.Text.Json;
using MaskedPi.Core;

namespace MaskedPi.Core.Tests;

public sealed class MaskingServiceTests
{
    [Fact]
    public void RuleLoader_LoadsRulesJsonSuccessfully()
    {
        var repoRoot = FindRepoRoot();
        var rulePath = Path.Combine(repoRoot, "config", "rules.json");

        var loader = new RuleLoader();
        var result = loader.Load(rulePath);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Rules);
    }

    [Fact]
    public void DictionaryProvider_LoadsLocalDictionarySuccessfully()
    {
        var repoRoot = FindRepoRoot();
        var dictPath = Path.Combine(repoRoot, "config", "local_dictionary.json");

        var provider = new DictionaryProvider();
        var result = provider.Load(dictPath);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Dictionary.NameLabels);
    }

    [Fact]
    public void DictionaryLoad_Succeeds_WhenOptionalKeysMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictPath = Path.Combine(tempDir, "dict.json");

        File.WriteAllText(rulePath, "{\"rules\":[]}");
        File.WriteAllText(dictPath, "{\"nameLabels\":[\"氏名\"]}");

        var service = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        var result = service.Load(rulePath, dictPath);

        Assert.True(result.Success);
    }

    [Fact]
    public void RuleLoader_SkipsInvalidRule()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var rulePath = Path.Combine(tempDir, "rules.json");
        File.WriteAllText(rulePath, "{\"rules\":[{\"name\":\"bad\",\"pattern\":\"([\",\"enabled\":true}]}");

        var result = new RuleLoader().Load(rulePath);

        Assert.True(result.Success);
        Assert.Empty(result.Rules);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void PriorityAndOverlap_FirstWinnerOnly()
    {
        var service = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine());
        var rules = new List<RuleDefinition>
        {
            new()
            {
                Name = "Local-High",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = "申請番号[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                Replacement = "[申請番号]",
                Priority = 10,
                Source = "fixed"
            },
            new()
            {
                Name = "Local-Low",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = "[一-龥]{1,8}番号[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                Replacement = "[識別子]",
                Priority = 90,
                Source = "fixed"
            }
        };

        var masked = service.Mask("申請番号: SINSEI-1001", rules);

        Assert.Equal("[申請番号]", masked.MaskedText);
        Assert.Single(masked.Replacements);
        Assert.Equal("Local-High", masked.Replacements[0].RuleName);
    }

    [Fact]
    public void LocalRule_WinsOverPhoneRule_WhenOverlapExpected()
    {
        var rules = new List<RuleDefinition>
        {
            new()
            {
                Name = "Local-Hotline",
                Enabled = true,
                Category = RuleCategory.LocalRule,
                Pattern = "相談管理番号[:：]?\\s*090-1234-5678",
                Replacement = "[相談管理番号]",
                Priority = 10,
                Source = "fixed"
            },
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

        var masked = new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine())
            .Mask("相談管理番号: 090-1234-5678", rules);

        Assert.Equal("[相談管理番号]", masked.MaskedText);
    }

    [Fact]
    public void Load_GeneratesNameLabelAndDepartmentRules()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);

        Assert.True(result.Success);
        Assert.Contains(result.Rules, r => r.Name == "DynamicNameLabelRule");
        Assert.Contains(result.Rules, r => r.Name == "DynamicDepartmentPersonRule");
    }

    [Fact]
    public void DepartmentPersonRule_AvoidsCommonNonNameWords()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);
        Assert.True(result.Success);

        var masked = fixture.Service.Mask("福祉課 御中", result.Rules);

        Assert.Equal("福祉課 御中", masked.MaskedText);
    }

    [Fact]
    public void Mask_LongInput_DoesNotThrowAndReturnsOutput()
    {
        var fixture = CreateFixture();
        var result = fixture.Service.Load(fixture.RulePath, fixture.DictionaryPath);
        Assert.True(result.Success);

        var sb = new StringBuilder();
        for (var i = 0; i < 10000; i++)
        {
            sb.Append("申請番号: SINSEI-1001 ");
        }

        var masked = fixture.Service.Mask(sb.ToString(), result.Rules);

        Assert.NotNull(masked);
        Assert.NotEmpty(masked.MaskedText);
    }

    [Fact]
    public void RuleTestCaseLoader_LoadsJsonCases()
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, "config", "test_cases.json");
        var result = new RuleTestCaseLoader().Load(path);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Cases);
    }

    private static TestFixture CreateFixture()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"maskedpi-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var rulePath = Path.Combine(tempDir, "rules.json");
        var dictionaryPath = Path.Combine(tempDir, "local_dictionary.json");

        var rules = new
        {
            rules = new[]
            {
                new
                {
                    name = "Fixed-ApplicationNumber",
                    enabled = true,
                    category = "LocalRule",
                    pattern = "申請番号[:：]?\\s*[0-9A-Za-z\\-]{3,}",
                    replacement = "[申請番号]",
                    priority = 12,
                    ignoreCase = false,
                    description = "fixed"
                }
            }
        };

        var dictionary = new
        {
            nameLabels = new[] { "氏名", "申請者" },
            addressLabels = new[] { "住所", "現住所" },
            dateLabels = new[] { "生年月日", "申請日" },
            contactLabels = new[] { "電話番号", "メール" },
            localLabels = new[] { "申請番号", "相談管理番号" },
            idPrefixes = new[] { "SINSEI-", "CASE-" },
            idSuffixKeywords = new[] { "番号", "ID" },
            departmentNames = new[] { "福祉課", "税務課" },
            surnames = new[] { "佐藤" },
            givenNames = new[] { "太郎" }
        };

        File.WriteAllText(rulePath, JsonSerializer.Serialize(rules));
        File.WriteAllText(dictionaryPath, JsonSerializer.Serialize(dictionary));

        return new TestFixture(
            new MaskingService(new RuleLoader(), new DictionaryProvider(), new RuleEngine()),
            rulePath,
            dictionaryPath);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "config")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private sealed record TestFixture(MaskingService Service, string RulePath, string DictionaryPath);
}
