using System.Text;
using System.Text.RegularExpressions;

namespace MaskedPi.Core;

/// <summary>
/// 正規表現ルールによる検出・置換本体。
/// 優先順位順に候補を評価し、重複範囲は先勝ちとする。
/// </summary>
public sealed class RuleEngine
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(300);

    public MaskingResult Apply(string input, IReadOnlyCollection<RuleDefinition> rules)
    {
        if (string.IsNullOrEmpty(input) || rules.Count == 0)
        {
            return new MaskingResult { MaskedText = input ?? string.Empty };
        }

        var candidates = new List<CandidateMatch>();

        foreach (var rule in rules.Where(r => r.Enabled).OrderBy(r => r.Priority))
        {
            Regex regex;
            try
            {
                regex = new Regex(
                    rule.Pattern,
                    RegexOptions.CultureInvariant | (rule.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None),
                    RegexTimeout);
            }
            catch
            {
                continue;
            }

            foreach (Match match in regex.Matches(input))
            {
                if (!match.Success || match.Length == 0)
                {
                    continue;
                }

                candidates.Add(new CandidateMatch(rule, match.Index, match.Length, match.Value));
            }
        }

        var selected = SelectNonOverlapping(candidates, input.Length);
        var maskedText = Rebuild(input, selected);

        var replacements = selected
            .OrderBy(m => m.Start)
            .Select(m => new ReplacementRecord
            {
                RuleName = m.Rule.Name,
                Priority = m.Rule.Priority,
                Category = m.Rule.Category,
                StartIndex = m.Start,
                Length = m.Length,
                OriginalText = m.Value,
                ReplacementText = m.Rule.Replacement,
                Source = string.IsNullOrWhiteSpace(m.Rule.Source) ? "unknown" : m.Rule.Source!,
                Notes = m.Rule.Notes
            })
            .ToList();

        var counts = replacements
            .GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var ruleCounts = replacements
            .GroupBy(r => r.RuleName)
            .ToDictionary(g => g.Key, g => g.Count());

        return new MaskingResult
        {
            MaskedText = maskedText,
            Replacements = replacements,
            CategoryCounts = counts,
            RuleHitCounts = ruleCounts
        };
    }

    private static List<CandidateMatch> SelectNonOverlapping(List<CandidateMatch> candidates, int textLength)
    {
        var occupied = new bool[textLength];
        var selected = new List<CandidateMatch>();

        foreach (var match in candidates
                     .OrderBy(c => c.Rule.Priority)
                     .ThenBy(c => c.Start)
                     .ThenByDescending(c => c.Length))
        {
            var end = match.Start + match.Length;
            var overlaps = false;
            for (var i = match.Start; i < end && i < occupied.Length; i++)
            {
                if (occupied[i])
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                continue;
            }

            for (var i = match.Start; i < end && i < occupied.Length; i++)
            {
                occupied[i] = true;
            }
            selected.Add(match);
        }

        return selected;
    }

    private static string Rebuild(string input, List<CandidateMatch> selected)
    {
        if (selected.Count == 0)
        {
            return input;
        }

        var sorted = selected.OrderBy(m => m.Start).ToList();
        var sb = new StringBuilder(input.Length);

        var cursor = 0;
        foreach (var match in sorted)
        {
            if (cursor < match.Start)
            {
                sb.Append(input, cursor, match.Start - cursor);
            }

            sb.Append(match.Rule.Replacement);
            cursor = match.Start + match.Length;
        }

        if (cursor < input.Length)
        {
            sb.Append(input, cursor, input.Length - cursor);
        }

        return sb.ToString();
    }

    private sealed record CandidateMatch(RuleDefinition Rule, int Start, int Length, string Value);
}
