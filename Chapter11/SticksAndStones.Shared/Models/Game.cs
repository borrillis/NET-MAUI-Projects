
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SticksAndStones.Models;

public class Game
{
    public Guid Id { get; set; } = Guid.Empty;

    public Player PlayerOne { get; set; }
    public Player PlayerTwo { get; set; }

    public Player NextPlayer { get; set; }

    public List<int> Sticks = new List<int>(24);
    public List<int> Stones = new List<int>(9);
    public List<int> Score = new List<int>(2);

    public bool Completed { get; set; } = false;

    public int PlayerOneScore
    {
        get => Score[0];
        set => Score[0] = value;
    }

    public int PlayerTwoScore
    {
        get => Score[1];
        set => Score[1] = value;
    }

    public static Game New(Player challenger, Player opponent)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            PlayerOne = opponent,
            PlayerTwo = challenger,
            NextPlayer = opponent
        };
    }
}


