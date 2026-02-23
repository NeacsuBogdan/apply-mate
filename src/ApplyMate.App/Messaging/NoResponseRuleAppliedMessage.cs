using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ApplyMate.App.Messaging;

public sealed class NoResponseRuleAppliedMessage : ValueChangedMessage<int>
{
    public NoResponseRuleAppliedMessage(int value)
        : base(value)
    {
    }
}
