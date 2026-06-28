using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Loot_V2.Messages;

public class TitleChangedMessage : ValueChangedMessage<string>
{
    public TitleChangedMessage(string value) : base(value) { }
}

public class MonthDataChangedMessage { }

public class OFXDataLoadedMessage { }

public class TransactionMatchedMessage
{
    public Guid TransactionId { get; init; }
    public string OFXTransactionId { get; init; } = string.Empty;
}

public class UnexpectedTransactionAddedMessage
{
    public string OFXTransactionId { get; init; } = string.Empty;
}
