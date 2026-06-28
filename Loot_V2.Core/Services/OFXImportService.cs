using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.Core.Services;

public class OFXImportService : IOFXImportService
{
    public Task<(IList<OFXImportTransaction> Transactions, decimal LedgerBalance)> ParseAsync(string filePath)
    {
        return Task.Run(() =>
        {
            var parser = new OFXSharp.OFXDocumentParser();
            OFXSharp.OFXDocument doc;

            using (var stream = File.OpenRead(filePath))
                doc = parser.Import(stream);

            var transactions = doc.Transactions
                .Select(t => new OFXImportTransaction
                {
                    Id = t.TransactionID ?? Guid.NewGuid().ToString(),
                    Date = DateOnly.FromDateTime(t.Date),
                    Name = t.Name ?? t.Memo ?? string.Empty,
                    Amount = t.Amount,
                    IsMatched = false
                })
                .ToList();

            var ledgerBalance = doc.Balance?.LedgerBalance ?? 0m;

            return ((IList<OFXImportTransaction>)transactions, ledgerBalance);
        });
    }
}
