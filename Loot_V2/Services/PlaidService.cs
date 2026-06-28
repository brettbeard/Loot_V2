using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Link;
using Going.Plaid.Transactions;

using Loot_V2.Contracts.Services;
using Loot_V2.Models;

namespace Loot_V2.Services;

public class PlaidService : IPlaidService
{
    private readonly PlaidClient _client;
    private readonly ILocalSettingsService _settings;

    private const string AccessTokenKey = "PlaidAccessToken";
    private string? _accessToken;
    private bool _initialized;

    public PlaidService(PlaidClient client, ILocalSettingsService settings)
    {
        _client = client;
        _settings = settings;
    }

    public bool IsConnected => !string.IsNullOrEmpty(_accessToken);

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _accessToken = await _settings.ReadSettingAsync<string>(AccessTokenKey);
        _initialized = true;
    }

    public async Task<string> CreateLinkTokenAsync()
    {
        var response = await _client.LinkTokenCreateAsync(new LinkTokenCreateRequest
        {
            ClientName = "Loot_V2",
            Language = Language.English,
            CountryCodes = new[] { CountryCode.Us },
            User = new LinkTokenCreateRequestUser { ClientUserId = "loot-v2-user" },
            Products = new[] { Products.Transactions },
        });
        return response.LinkToken;
    }

    public async Task ExchangePublicTokenAsync(string publicToken)
    {
        var response = await _client.ItemPublicTokenExchangeAsync(new ItemPublicTokenExchangeRequest
        {
            PublicToken = publicToken,
        });
        _accessToken = response.AccessToken;
        await _settings.SaveSettingAsync(AccessTokenKey, _accessToken);
    }

    public async Task<IEnumerable<PlaidTransaction>> GetTransactionsAsync(DateTime start, DateTime end)
    {
        var response = await _client.TransactionsGetAsync(new TransactionsGetRequest
        {
            AccessToken = _accessToken!,
            StartDate = DateOnly.FromDateTime(start),
            EndDate = DateOnly.FromDateTime(end),
        });

        return response.Transactions.Select(t => new PlaidTransaction
        {
            TransactionId = t.TransactionId ?? string.Empty,
            AccountId = t.AccountId ?? string.Empty,
            Name = t.MerchantName ?? string.Empty,
            Amount = (decimal)t.Amount,
            Date = t.Date ?? DateOnly.MinValue,
            Category = t.PersonalFinanceCategory?.Primary,
            Pending = t.Pending ?? false,
        });
    }
}
