using SticksAndStones.Models;

namespace SticksAndStones.Messages
{
    public class PlayerStatusUpdateMessage : Message
    {
        private readonly Player Player;

        public PlayerStatusUpdateMessage(Player player) : base(player.Id)
        {
            this.Player = player;
        }
    }
}
