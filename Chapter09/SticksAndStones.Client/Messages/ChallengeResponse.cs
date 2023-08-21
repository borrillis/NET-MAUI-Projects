using CommunityToolkit.Mvvm.Messaging.Messages;
using SticksAndStones.Models;

namespace SticksAndStones.Messages;

public class ChallengeResponse : ValueChangedMessage<PlayerChallenge>
{
    public ChallengeResponse(PlayerChallenge challenge) : base(challenge)
    {
    }
}
