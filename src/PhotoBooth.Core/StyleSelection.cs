using System.Security.Cryptography;

namespace PhotoBooth.Core;

public static class StyleSelection
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string> choices, int photoCount)
    {
        if (photoCount is not (1 or 4)) throw new ArgumentOutOfRangeException(nameof(photoCount));
        var ids = choices.ToArray();
        if (ids.Length < 1 || ids.Length > photoCount ||
            ids.Any(id => id is null || !StyleCatalog.ValidId(id) || id == StyleCatalog.Mixed) ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Length ||
            (ids.Length > 1 && ids.Contains(StyleCatalog.Chef)))
            throw new ArgumentException("Choose one theme for one photo, or one to four themes for four photos.", nameof(choices));
        Array.Sort(ids, StringComparer.Ordinal);
        return Array.AsReadOnly(ids);
    }

    public static bool MatchesPlan(IReadOnlyList<string> choices, IReadOnlyList<string> plan, int count)
    {
        if (plan.Count != count || plan.Any(id => string.IsNullOrWhiteSpace(id) || !StyleCatalog.ValidId(id) || id is StyleCatalog.Chef or StyleCatalog.Mixed)) return false;
        if (choices.Count == 1 && choices[0] == StyleCatalog.Chef) return true;
        if (plan.Any(id => !choices.Contains(id)) || choices.Any(id => !plan.Contains(id))) return false;
        var uses = plan.GroupBy(id => id).Select(group => group.Count()).ToArray();
        return uses.Max() - uses.Min() <= 1 && (choices.Count == 1 || DiagonalRepeats(plan));
    }

    // Capture indices are printed row by row. Repeats may touch chronologically,
    // but never share an edge on the four-photo sheet.
    public static bool DiagonalRepeats(IReadOnlyList<string> plan) => plan.Count != 4 ||
        (plan[0] != plan[1] && plan[0] != plan[2] && plan[1] != plan[3] && plan[2] != plan[3]);

    // Rehearsal only. The authenticated server freezes the authoritative plan for online sessions.
    public static IReadOnlyList<string> RehearsalPlan(IReadOnlyList<string> choices, int count)
    {
        var selected = Normalize(choices, count);
        var pool = selected[0] == StyleCatalog.Chef ? StyleCatalog.BuiltIn.Styles.Select(s => s.Id).ToArray() : selected.ToArray();
        RandomNumberGenerator.Shuffle(pool.AsSpan());
        if (pool.Length >= count) return Array.AsReadOnly(pool.Take(count).ToArray());
        var slots = Enumerable.Range(0, count).Select(i => pool[i % pool.Length]).ToArray();
        var plans = new List<string[]>();
        void Visit(List<string> prefix, List<string> remaining)
        {
            if (remaining.Count == 0) { if (pool.Length == 1 || DiagonalRepeats(prefix)) plans.Add(prefix.ToArray()); return; }
            foreach (var id in remaining.Distinct().ToArray())
            {
                var next = new List<string>(remaining); next.Remove(id);
                Visit([..prefix, id], next);
            }
        }
        Visit([], slots.ToList());
        return Array.AsReadOnly(plans[RandomNumberGenerator.GetInt32(plans.Count)]);
    }
}
