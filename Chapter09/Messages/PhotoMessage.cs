using System;
namespace SticksAndStones.Messages;

public class PhotoMessage : Message
{
    public PhotoMessage() { }
    public PhotoMessage(string playerId) : base(playerId) { }

    public string Base64Photo { get; set; }
    public string ImageFormat { get; set; }
}
