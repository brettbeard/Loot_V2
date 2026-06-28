using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.Core.Services;

public class MatchingService : IMatchingService
{
    public IList<OFXImportTransaction> RankCandidates(
        MonthTransaction expected,
        IEnumerable<OFXImportTransaction> candidates)
    {
        return candidates
            .Select(c => (Transaction: c, Score: Score(expected, c)))
            .OrderByDescending(x => x.Score)
            .Select(x => x.Transaction)
            .ToList();
    }

    private static double Score(MonthTransaction expected, OFXImportTransaction candidate)
    {
        var score = 0.0;

        // Amount (weight: high — 100 pts max)
        var candidateAbs = Math.Abs(candidate.Amount);
        if (candidateAbs == expected.Amount)
            score += 100;
        else if (expected.Amount > 0 && Math.Abs(candidateAbs - expected.Amount) / expected.Amount <= 0.05m)
            score += 50;

        // Name keyword match (weight: medium — 50 pts max)
        var expectedWords = expected.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var candidateName = candidate.Name.ToLowerInvariant();
        var matchedWords = expectedWords.Count(w => candidateName.Contains(w.ToLowerInvariant()));
        if (expectedWords.Length > 0)
            score += 50.0 * matchedWords / expectedWords.Length;

        // Date proximity (weight: medium — 30 pts max)
        var dayDiff = Math.Abs(candidate.Date.Day - expected.Date.Day);
        score += dayDiff switch
        {
            0 => 30,
            <= 3 => 20,
            <= 5 => 10,
            _ => 0
        };

        return score;
    }
}
