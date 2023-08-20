using SticksAndStones.Models;

namespace SticksAndStones.Messages;

public class PlayerChallenge : Message
{
	public Player Challenger { get; set; }
	public Player Opponent { get; set; }

	public ChallengeStatus Status { get; set; }

	public string GameId { get; set; }

	public PlayerChallenge() : base()
	{ }

	public PlayerChallenge(Player challenger, Player opponent, ChallengeStatus status) : base(challenger.Id)
	{
		this.Challenger = challenger;
		this.Opponent = opponent;
		this.Status = status;
	}

	public PlayerChallenge(Player challenger, Player opponent, ChallengeStatus status, string gameId) : this(challenger, opponent, status)
	{
		this.GameId = gameId;
	}
}
