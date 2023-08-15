using System;

namespace SticksAndStones.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.Empty;

    public string GamerTag { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public Uri? AvatarUrl { get; set; } = null;
    public DateTime LastSeen { get; set; } = DateTime.MinValue;
    public Game? Game { get; set; } = null;
}
