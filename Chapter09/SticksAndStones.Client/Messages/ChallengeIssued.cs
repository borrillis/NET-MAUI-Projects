using CommunityToolkit.Mvvm.Messaging.Messages;
using SticksAndStones.Models;

namespace SticksAndStones.Messages;

public class ChallengeIssued : ValueChangedMessage<Player>
{
    public ChallengeIssued(Player value) : base(value)
    {
    }
}
