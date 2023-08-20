using System;
namespace SticksAndStones.Messages;

public class PhotoUrlMessage : Message
{
    public PhotoUrlMessage() { }
    public PhotoUrlMessage(string playerId) : base(playerId) { }

    public string Url { get; set; }
}

