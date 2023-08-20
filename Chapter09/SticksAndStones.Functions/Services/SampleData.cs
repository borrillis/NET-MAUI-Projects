using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SticksAndStones.Repository;
using SticksAndStones.Repository.Dto;

namespace SticksAndStones.Functions;

public class SampleData
{
    private const string AddSampleDataFunction = nameof(AddSampleData);

    readonly GameContext context;

    public SampleData(GameContext dbContext)
    {
        this.context = dbContext;
    }

    [FunctionName(AddSampleDataFunction)]
    public IActionResult AddSampleData(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = AddSampleDataFunction)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("SampleData: Add players");

        var playerNames = new string[] { "Michael", "Rita", "Lydia", "Aaron", "George", "Kathy", "Ernest", "Anna", "Madeline" };
        var gamePairs = new Tuple<string, string>[] { new Tuple<string, string>("Michael", "Rita") };

        context.Players.AddRange(CreatePlayers(playerNames));
        context.SaveChanges();

        context.Games.AddRange(CreateGames(gamePairs));
        context.SaveChanges();


        return new OkResult();
    }

    private static IEnumerable<Player> CreatePlayers(string[] playerNames)
    {
        return from player in playerNames
               select new Player()
               {
                   Name = player,
                   EmailAddress = $"{player.ToLower()}@gmail.com",
               };
    }

    private IEnumerable<Game> CreateGames(IEnumerable<Tuple<string,string>> gamePairs)
    {
        return from pair in gamePairs
               select new Game()
               {
                   PlayerOneId = (from p in context.Players
                                  where p.Name == pair.Item1
                                  select p.Id).First(),
                   PlayerTwoId = (from p in context.Players
                                  where p.Name == pair.Item2
                                  select p.Id).First(),
                   PlayerOneScore = 0,
                   PlayerTwoScore = 0,
                   NextPlayer = (from p in context.Players
                                 where p.Name == pair.Item1
                                 select p.Id).First(),
                   Sticks = new List<int>(24),
                   Stones = new List<int>(9)

               };
    }
}

