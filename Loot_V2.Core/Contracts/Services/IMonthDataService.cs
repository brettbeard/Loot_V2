using Loot_V2.Core.Models;

namespace Loot_V2.Core.Contracts.Services;

public interface IMonthDataService
{
    MonthData? CurrentMonth { get; }
    bool IsDirty { get; }
    string? CurrentFilePath { get; }

    event EventHandler? DataChanged;
    event EventHandler? OFXDataLoaded;

    void NewMonth(int year, int month, IEnumerable<BudgetItem> selectedItems);
    MonthData? Open(string filePath);
    void Save(string filePath);

    void AddTransaction(MonthTransaction transaction);
    void UpdateTransaction(MonthTransaction transaction);
    void DeleteTransaction(Guid id);
    void MatchTransaction(Guid transactionId, OFXImportTransaction ofxTransaction);
    void AddUnexpectedTransaction(OFXImportTransaction ofxTransaction, string? customName = null);
    void SetOFXData(IList<OFXImportTransaction> transactions, decimal ledgerBalance);
    void SetOFXTransactionHidden(string ofxId, bool hidden);
    void RecalculateRunningBalances();
}
