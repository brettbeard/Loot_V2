using Loot_V2.Core.Models;

namespace Loot_V2.Core.Contracts.Services;

public interface IMatchingService
{
    IList<OFXImportTransaction> RankCandidates(
        MonthTransaction expected,
        IEnumerable<OFXImportTransaction> candidates);
}
