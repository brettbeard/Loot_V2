using Loot_V2.Core.Models;

namespace Loot_V2.Core.Contracts.Services;

public interface IOFXImportService
{
    Task<(IList<OFXImportTransaction> Transactions, decimal LedgerBalance)> ParseAsync(string filePath);
}
