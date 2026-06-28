using Loot_V2.Models;

namespace Loot_V2.Contracts.Services;

public interface IPlaidService
{
    bool IsConnected { get; }
    Task InitializeAsync();
    Task<string> CreateLinkTokenAsync();
    Task ExchangePublicTokenAsync(string publicToken);
    Task<IEnumerable<PlaidTransaction>> GetTransactionsAsync(DateTime start, DateTime end);
}
