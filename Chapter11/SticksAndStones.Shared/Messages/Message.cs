using System;

namespace SticksAndStones.Messages;

public abstract class Message
{
    public Type TypeInfo { get { return this.GetType(); } }
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Guid SenderId { get; set; }

    public Message (Guid senderId) { this.SenderId = senderId; }
}