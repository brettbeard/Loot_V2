using System.Xml.Linq;

using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.Core.Services;

public class MonthDataService : IMonthDataService
{
    public MonthData? CurrentMonth { get; private set; }
    public bool IsDirty { get; private set; }
    public string? CurrentFilePath { get; private set; }

    public event EventHandler? DataChanged;
    public event EventHandler? OFXDataLoaded;

    public void NewMonth(int year, int month, IEnumerable<BudgetItem> selectedItems)
    {
        var data = new MonthData { Year = year, Month = month, StartingBalance = 0m };

        var startingRow = new MonthTransaction
        {
            Date = new DateOnly(year, month, 1),
            Name = "Starting Balance",
            Amount = 0m,
            Status = TransactionStatus.Expected,
            IsStartingBalance = true
        };
        data.Transactions.Add(startingRow);

        foreach (var item in selectedItems)
        {
            var day = Math.Min(item.DayOfMonth, DateTime.DaysInMonth(year, month));
            data.Transactions.Add(new MonthTransaction
            {
                Date = new DateOnly(year, month, day),
                Name = item.Name,
                Amount = item.Amount,
                IsCredit = item.IsCredit,
                Status = TransactionStatus.Expected
            });
        }

        CurrentMonth = data;
        CurrentFilePath = null;
        RecalculateRunningBalances();
        IsDirty = true;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public MonthData? Open(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var doc = XDocument.Load(filePath);
        var root = doc.Root;
        if (root is null) return null;

        var data = new MonthData
        {
            Year = int.Parse(root.Attribute("Year")?.Value ?? "0"),
            Month = int.Parse(root.Attribute("Month")?.Value ?? "1"),
            StartingBalance = decimal.Parse(root.Attribute("StartingBalance")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture)
        };

        var txns = root.Element("Transactions");
        if (txns is not null)
        {
            foreach (var e in txns.Elements("Transaction"))
            {
                var idAttr = e.Attribute("Id")?.Value;
                var dateAttr = e.Attribute("Date")?.Value;
                if (idAttr is null || dateAttr is null) continue;

                data.Transactions.Add(new MonthTransaction
                {
                    Id = Guid.Parse(idAttr),
                    Date = DateOnly.ParseExact(dateAttr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    Name = e.Attribute("Name")?.Value ?? string.Empty,
                    Amount = decimal.Parse(e.Attribute("Amount")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                    IsCredit = bool.Parse(e.Attribute("IsCredit")?.Value ?? "false"),
                    Status = Enum.TryParse<TransactionStatus>(e.Attribute("Status")?.Value, out var status) ? status : TransactionStatus.Expected,
                    OFXTransactionId = e.Attribute("OFXTransactionId")?.Value,
                    IsStartingBalance = bool.Parse(e.Attribute("IsStartingBalance")?.Value ?? "false")
                });
            }
        }

        var ofxTxns = root.Element("OFXTransactions");
        if (ofxTxns is not null)
        {
            foreach (var e in ofxTxns.Elements("OFXTransaction"))
            {
                var ofxId = e.Attribute("Id")?.Value;
                var ofxDate = e.Attribute("Date")?.Value;
                if (ofxId is null || ofxDate is null) continue;

                data.OFXTransactions.Add(new OFXImportTransaction
                {
                    Id = ofxId,
                    Date = DateOnly.ParseExact(ofxDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    Name = e.Attribute("Name")?.Value ?? string.Empty,
                    Amount = decimal.Parse(e.Attribute("Amount")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
                    IsMatched = bool.Parse(e.Attribute("IsMatched")?.Value ?? "false"),
                    IsHidden  = bool.Parse(e.Attribute("IsHidden")?.Value  ?? "false")
                });
            }
        }

        CurrentMonth = data;
        CurrentFilePath = filePath;
        IsDirty = false;
        RecalculateRunningBalances();
        DataChanged?.Invoke(this, EventArgs.Empty);
        if (data.OFXTransactions.Count > 0)
            OFXDataLoaded?.Invoke(this, EventArgs.Empty);

        return data;
    }

    public void Save(string filePath)
    {
        if (CurrentMonth is null) return;

        var doc = new XDocument(
            new XElement("Month",
                new XAttribute("Year", CurrentMonth.Year),
                new XAttribute("Month", CurrentMonth.Month),
                new XAttribute("StartingBalance", CurrentMonth.StartingBalance),
                new XElement("Transactions",
                    CurrentMonth.Transactions.Select(t =>
                    {
                        var el = new XElement("Transaction",
                            new XAttribute("Id", t.Id),
                            new XAttribute("Date", t.Date.ToString("yyyy-MM-dd")),
                            new XAttribute("Name", t.Name),
                            new XAttribute("Amount", t.Amount),
                            new XAttribute("IsCredit", t.IsCredit),
                            new XAttribute("Status", t.Status),
                            new XAttribute("IsStartingBalance", t.IsStartingBalance));
                        if (t.OFXTransactionId is not null)
                            el.Add(new XAttribute("OFXTransactionId", t.OFXTransactionId));
                        return el;
                    })),
                new XElement("OFXTransactions",
                    CurrentMonth.OFXTransactions.Select(o => new XElement("OFXTransaction",
                        new XAttribute("Id", o.Id),
                        new XAttribute("Date", o.Date.ToString("yyyy-MM-dd")),
                        new XAttribute("Name", o.Name),
                        new XAttribute("Amount", o.Amount),
                        new XAttribute("IsMatched", o.IsMatched),
                        new XAttribute("IsHidden",  o.IsHidden))))));

        doc.Save(filePath);
        CurrentFilePath = filePath;
        IsDirty = false;
    }

    public void AddTransaction(MonthTransaction transaction)
    {
        CurrentMonth?.Transactions.Add(transaction);
        RecalculateRunningBalances();
        MarkDirty();
    }

    public void UpdateTransaction(MonthTransaction transaction)
    {
        if (CurrentMonth is null) return;

        var idx = CurrentMonth.Transactions.FindIndex(t => t.Id == transaction.Id);
        if (idx < 0) return;

        var replaced = new MonthTransaction
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Name = transaction.Name,
            Amount = transaction.Amount,
            IsCredit = transaction.IsCredit,
            Status = transaction.Status,
            OFXTransactionId = transaction.OFXTransactionId,
            IsStartingBalance = transaction.IsStartingBalance
        };
        CurrentMonth.Transactions[idx] = replaced;

        if (transaction.IsStartingBalance)
            CurrentMonth.StartingBalance = transaction.Amount;

        RecalculateRunningBalances();
        MarkDirty();
    }

    public void DeleteTransaction(Guid id)
    {
        if (CurrentMonth is null) return;
        var tx = CurrentMonth.Transactions.FirstOrDefault(t => t.Id == id);
        if (tx is null) return;

        if (tx.OFXTransactionId is not null)
        {
            var ofx = CurrentMonth.OFXTransactions.FirstOrDefault(o => o.Id == tx.OFXTransactionId);
            if (ofx is not null)
                ofx.IsMatched = false;
        }

        CurrentMonth.Transactions.Remove(tx);
        RecalculateRunningBalances();
        MarkDirty();
    }

    public void MatchTransaction(Guid transactionId, OFXImportTransaction ofxTransaction)
    {
        if (CurrentMonth is null) return;

        var tx = CurrentMonth.Transactions.FirstOrDefault(t => t.Id == transactionId);
        if (tx is null) return;

        tx.Status = TransactionStatus.Reconciled;
        tx.OFXTransactionId = ofxTransaction.Id;
        tx.Amount = Math.Abs(ofxTransaction.Amount);
        tx.Date = ofxTransaction.Date;
        tx.IsCredit = ofxTransaction.Amount > 0;

        var ofx = CurrentMonth.OFXTransactions.FirstOrDefault(o => o.Id == ofxTransaction.Id);
        if (ofx is not null)
            ofx.IsMatched = true;

        RecalculateRunningBalances();
        MarkDirty();
    }

    public void AddUnexpectedTransaction(OFXImportTransaction ofxTransaction, string? customName = null)
    {
        if (CurrentMonth is null) return;

        var tx = new MonthTransaction
        {
            Date = ofxTransaction.Date,
            Name = customName ?? ofxTransaction.Name,
            Amount = Math.Abs(ofxTransaction.Amount),
            IsCredit = ofxTransaction.Amount > 0,
            Status = TransactionStatus.Unexpected,
            OFXTransactionId = ofxTransaction.Id
        };
        CurrentMonth.Transactions.Add(tx);

        var ofx = CurrentMonth.OFXTransactions.FirstOrDefault(o => o.Id == ofxTransaction.Id);
        if (ofx is not null)
            ofx.IsMatched = true;

        RecalculateRunningBalances();
        MarkDirty();
    }

    public void SetOFXTransactionHidden(string ofxId, bool hidden)
    {
        if (CurrentMonth is null) return;
        var ofx = CurrentMonth.OFXTransactions.FirstOrDefault(o => o.Id == ofxId);
        if (ofx is null) return;
        ofx.IsHidden = hidden;
        IsDirty = true;
    }

    public void SetOFXData(IList<OFXImportTransaction> transactions, decimal ledgerBalance)
    {
        if (CurrentMonth is null) return;

        var existingIds = CurrentMonth.OFXTransactions.Select(o => o.Id).ToHashSet();
        foreach (var tx in transactions)
            if (!existingIds.Contains(tx.Id))
                CurrentMonth.OFXTransactions.Add(tx);

        RecalculateRunningBalances();
        IsDirty = true;
        OFXDataLoaded?.Invoke(this, EventArgs.Empty);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RecalculateRunningBalances()
    {
        if (CurrentMonth is null) return;

        var sorted = CurrentMonth.Transactions
            .OrderBy(t => t.IsStartingBalance ? 0 : 1)
            .ThenBy(t => t.Date)
            .ToList();

        var balance = CurrentMonth.StartingBalance;
        foreach (var tx in sorted)
        {
            if (tx.IsStartingBalance)
            {
                tx.RunningBalance = balance;
                continue;
            }
            balance = tx.IsCredit ? balance + tx.Amount : balance - tx.Amount;
            tx.RunningBalance = balance;
        }
    }

    private void MarkDirty()
    {
        IsDirty = true;
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
