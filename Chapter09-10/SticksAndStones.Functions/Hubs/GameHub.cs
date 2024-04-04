using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Azure.SignalR.Management;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SticksAndStones.Handlers;
using SticksAndStones.Messages;
using SticksAndStones.Models;
using SticksAndStones.Repository;
using System.Text.Json;

namespace SticksAndStones.Hubs;

public interface IGameHub
{
    Task PlayerUpdated(PlayerUpdatedEventArgs playerUpdatedEvent);
    Task Challenge(ChallengeEventArgs challengeEvent);
    Task MatchStarted(MatchStartedEventArgs matchStartedEvent);
    Task MatchUpdated(MatchUpdatedEventArgs matchUpdatedEvent);
}


public class GameHub(IServiceProvider serviceProvider, ILogger<GameHub> logger, IDbContextFactory<GameDbContext> contextFactory, ChallengeHandler challengeHandler) : ServerlessHub<IGameHub>(serviceProvider)
{
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    readonly (int stone, int[] sticks)[][] stickToStoneMap = [
        /* 1 */ [(1, [4, 5, 8]), (0, [0, 0, 0])],
        /* 2 */ [(2, [5, 6, 9]), (0, [0, 0, 0])],
        /* 3 */ [(3, [6, 7,10]), (0, [0, 0, 0])],
        /* 4 */ [(1, [1, 5, 8]), (0, [0, 0, 0])],
        /* 5 */ [(1, [1, 4, 8]), (2, [2, 6, 9])],
        /* 6 */ [(2, [2, 5, 9]), (3, [3, 7,10])],
        /* 7 */ [(3, [3, 6,10]), (0, [0, 0, 0])],
        /* 8 */ [(1, [1, 4, 5]), (4, [11,12,15])],
        /* 9 */ [(2, [2, 5, 6]), (5, [12,13,16])],
        /*10 */ [(3, [3, 6, 7]), (6, [13,14,17])],
        /*11 */ [(4, [8,12,15]), (0, [0, 0, 0])],
        /*12 */ [(4, [8,11,15]), (5, [9,13,16])],
        /*13 */ [(5, [9,12,16]), (6, [10,14,17])],
        /*14 */ [(6, [10,13,17]), (0, [0, 0, 0])],
        /*15 */ [(4, [8,11,12]), (7, [18,19,22])],
        /*16 */ [(5, [9,12,13]), (8, [19,20,23])],
        /*17 */ [(6, [13,14,17]), (9, [20,21,24])],
        /*18 */ [(7, [15,19,22]), (0, [0, 0, 0])],
        /*19 */ [(7, [15,18,22]), (8, [16,20,23])],
        /*20 */ [(8, [16,19,23]), (9, [17,21,24])],
        /*21 */ [(9, [17,20,24]), (0, [0, 0, 0])],
        /*22 */ [(7, [15,18,19]), (0, [0, 0, 0])],
        /*23 */ [(8, [16,19,20]), (0, [0, 0, 0])],
        /*24 */ [(9, [17,20,21]), (0, [0, 0, 0])],
    ];

    [Function("Connect")]
    public async Task<IActionResult> Connect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
        logger.LogInformation("A new client is requesting connection");

        var result = await JsonSerializer.DeserializeAsync<ConnectRequest>(req.Body, jsonOptions);
        var newPlayer = result.Player;

        if (newPlayer is null)
        {
            var error = new ArgumentException("No player data.", "Player");
            logger.LogError(error, "Failure to deserialize arguments");
            return new BadRequestObjectResult(error);
        }

        if (string.IsNullOrEmpty(newPlayer.GamerTag))
        {
            var error = new ArgumentException("A GamerTag is required for all players.", "GamerTag");
            logger.LogError(error, "Invalid value for GamerTag");
            return new BadRequestObjectResult(error);
        }

        if (string.IsNullOrEmpty(newPlayer.EmailAddress))
        {
            var error = new ArgumentException("An Email Address is required for all players.", "EmailAddress");
            logger.LogError(error, "Invalid value for EmailAddress");
            return new BadRequestObjectResult(error);
        }
        
        using var context = contextFactory.CreateDbContext();

        logger.LogInformation("Checking for GamerTag usage");
        var gamerTagInUse = (from p in context.Players 
                             where string.Equals(p.GamerTag, newPlayer.GamerTag, StringComparison.InvariantCultureIgnoreCase) 
                             && !string.Equals(p.EmailAddress, newPlayer.EmailAddress, StringComparison.OrdinalIgnoreCase) 
                             select p).Any();
        if (gamerTagInUse)
        {
            var error = new ArgumentException($"The GamerTag {newPlayer.GamerTag} is in use, please choose another.", "GamerTag");
            logger.LogError(error, "GamerTag in use.");
            return new BadRequestObjectResult(error);
        }

        logger.LogInformation("Locating Player record.");
        var thisPlayer = (from p in context.Players where string.Equals(p.EmailAddress, newPlayer.EmailAddress, StringComparison.OrdinalIgnoreCase) select p).FirstOrDefault();

        if (thisPlayer is null)
        {
            logger.LogInformation("Player not found, creating.");
            thisPlayer = newPlayer;
            thisPlayer.Id = Guid.NewGuid();
            context.Add(thisPlayer);
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Notifying connected players of new player.");
        await Clients.All.PlayerUpdated(new PlayerUpdatedEventArgs(thisPlayer));

        // Get the set of available players
        logger.LogInformation("Getting the set of available players.");
        var players = (from player in context.Players
                       where player.Id != thisPlayer.Id
                       select player).ToList();

        var binaryData = await NegotiateAsync(new NegotiationOptions() { UserId = thisPlayer.Id.ToString() });

        var connectionInfo = binaryData.ToDynamicFromJson();
        
        logger.LogInformation("Creating response.");
        var connectResponse = new ConnectResponse()
        {
            Player = thisPlayer,
            Players = players,
            ConnectionInfo = new Models.ConnectionInfo { Url = connectionInfo.url, AccessToken = connectionInfo.accessToken }
        };

        logger.LogInformation("Sending response.");
        return new OkObjectResult(connectResponse);
    }

    [Function("GetAllPlayers")]
    public IActionResult GetAllPlayers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Players/GetAll")] HttpRequest req)
    {
        // Exclude the playerId if provided
        Guid playerId = Guid.Empty;
        if (req.Query.ContainsKey("id"))
        {
            var id = req.Query["id"];
            if (!string.IsNullOrEmpty(id))
            {
                playerId = new Guid(id!);
            }
        }

        using var context = contextFactory.CreateDbContext();

        // Get the set of available players
        logger.LogInformation("Getting the set of available players.");
        var players = (from player in context.Players
                       where player.Id != playerId
                       select player).ToList();
        return new OkObjectResult(new GetAllPlayersResponse(players));
    }
    
    [Function("IssueChallenge")]
    public async Task<IssueChallengeResponse> IssueChallenge(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Challenge/Issue")] HttpRequest req)
    {
        var result = await JsonSerializer.DeserializeAsync<IssueChallengeRequest>(req.Body, jsonOptions);

        using var context = contextFactory.CreateDbContext();

        Guid challengerId = result.Challenger.Id;
        var challenger = (from p in context.Players
                          where p.Id == challengerId
                          select p).FirstOrDefault();

        Guid opponentId = result.Opponent.Id;
        var opponent = (from p in context.Players
                        where p.Id == opponentId
                        select p).FirstOrDefault();

        if (challenger is null)
            throw new ArgumentException(paramName: nameof(challenger), message: $"{result.Challenger.GamerTag} is not a valid player.");
        if (opponent is null)
            throw new ArgumentException(paramName: nameof(opponent), message: $"{result.Opponent.GamerTag} is not a valid player.");

        var challengerInMatch = (from g in context.Matches
                                where g.PlayerOneId == challengerId || g.PlayerTwoId == challengerId
                                select g).Any();

        var opponentInMatch = (from g in context.Matches
                              where g.PlayerOneId == opponentId || g.PlayerTwoId == opponentId
                              select g).Any();

        if (challengerInMatch)
            throw new ArgumentException(paramName: nameof(challenger), message: $"{challenger.GamerTag} is already in a match!");

        if (opponentInMatch)
            throw new ArgumentException(paramName: nameof(opponent), message: $"{opponent.GamerTag} is already in a match!");

        Guid matchId = Guid.Empty;

        logger.LogInformation("{Challenger} has challenged {Opponent} to a match!", challenger.GamerTag, opponent.GamerTag);

        var challengeInfo = challengeHandler.CreateChallenge(challenger, opponent);
        logger.LogInformation("Challenge [{ChallengeInfoId}] has been created.", challengeInfo.id);

        logger.LogInformation("Waiting on response from {Opponent} for challenge[{ChallengeInfoId}].", opponent.GamerTag, challengeInfo.id);

        await Clients.User(opponent.Id.ToString()).Challenge( new ChallengeEventArgs(challengeInfo.id, challenger, opponent));

        ChallengeResponse response;
        try
        {
            var challenge = await challengeInfo.responseTask.ConfigureAwait(false);
            logger.LogInformation("Got response from {Opponent} for challenge[{ChallengeInfoId}].", opponent.GamerTag, challengeInfo.id);
            response = challenge.Response;
        }
        catch 
        {
            logger.LogInformation("Never received a response from {Opponent} for challenge[{ChallengeInfoId}], it timed out.",  opponent.GamerTag, challengeInfo.id);
            response = ChallengeResponse.TimeOut;
        }
        return new(response);
    }

    [Function("AcknowledgeChallenge")]
    public async Task AcknowledgeChallenge(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Challenge/Ack")] HttpRequest req)
    {
        var result = await JsonSerializer.DeserializeAsync<AcknowledgeChallengeRequest>(req.Body, jsonOptions);

        var challenge = challengeHandler.Respond(result.Id, result.Response);
        if (challenge.Id == Guid.Empty)
        {
            return;
        }

        var challenger = challenge.Challenger;
        var opponent = challenge.Opponent;

        if (result.Response == ChallengeResponse.Declined)
        {
            logger.LogInformation("{Opponent} has declined the challenge from {Challenger}!", opponent.GamerTag, challenger.GamerTag);
        }

        if (result.Response == ChallengeResponse.Accepted)
        {
            logger.LogInformation("{Opponent} has accepted the challenge from {Challenger}!", opponent.GamerTag, challenger.GamerTag);

            using var context = contextFactory.CreateDbContext();

            var match = Match.New(challenger.Id, opponent.Id);
            context.Matches.Add(match);

            opponent.MatchId = challenger.MatchId = match.Id;

            context.Players.Update(opponent);
            context.Players.Update(challenger);
            context.SaveChanges();

            logger.LogInformation("Created match {Match} bewteen {Opponent} and {Challenger}!",match.Id, opponent.GamerTag, challenger.GamerTag);

            // Create Group for Game
            await UserGroups.AddToGroupAsync(opponent.Id.ToString(), $"Match[{match.Id}]");
            await UserGroups.AddToGroupAsync(challenger.Id.ToString(), $"Match[{match.Id}]");
            await Clients.Group($"Match[{match.Id}]").MatchStarted(new MatchStartedEventArgs(match));

            await Clients.All.PlayerUpdated(new PlayerUpdatedEventArgs(opponent));
            await Clients.All.PlayerUpdated(new PlayerUpdatedEventArgs(challenger));
        }
    }

    [Function("GetMatch")]
    public IActionResult GetMatch(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Match/{id}")] HttpRequest req,
        Guid id)
    {
        using var context = contextFactory.CreateDbContext();

        var match = (from m in context.Matches where m.Id == id select m).FirstOrDefault();
        ObjectResult result = new BadRequestObjectResult("Match not found");
        if (match != null) {
            result = new OkObjectResult(new GetMatchResponse(match));
        }
        return result;
    }
    
    [Function("ProcessTurn")]
    public async Task<IActionResult> ProcessTurn(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Game/Move")] HttpRequest req)
    {
        var args = await JsonSerializer.DeserializeAsync<ProcessTurnRequest>(req.Body, jsonOptions);

        var error = ValidateProcessTurnRequest(args);
        if (error is not null)
        {
            logger.LogError(error, "Error validating turn request.");
            return new BadRequestObjectResult(error);
        }

        using var context = contextFactory.CreateDbContext();

        var match = (from m in context.Matches where m.Id == args.MatchId select m).FirstOrDefault() ?? throw new ArgumentException("Invalid MatchId.");

        error = VerifyMatchState(match, args);
        if (error is not null)
        {
            await SaveMatchAndSendUpdates(context, match);
            logger.LogError(error, "Error validating match state.");
            return new BadRequestObjectResult(error);
        }

        match.Sticks[args.Position] = args.Player.Id == match.PlayerOneId ? 1 : -1;

        if (args.Player.Id == match.PlayerOneId)
        {
            match.PlayerOneScore += 1;
        }
        else
        {
            match.PlayerTwoScore += 1;
        }

        // Determine if this play creates a square
        foreach (var tuple in stickToStoneMap[args.Position])
        {
            if (tuple.stone == 0) continue;

            var stickCompletesABox =
            (
                Math.Abs(match.Sticks[tuple.sticks[0] - 1]) +
                Math.Abs(match.Sticks[tuple.sticks[1] - 1]) +
                Math.Abs(match.Sticks[tuple.sticks[2] - 1])
            ) == 3;

            if (stickCompletesABox)
            {
                // If so, place stone, and adjust score
                var player = args.Player.Id == match.PlayerOneId ? 1 : -1;
                match.Stones[tuple.stone - 1] = player;
                if (player > 0)
                {
                    match.PlayerOneScore += 5;
                }
                else
                {
                    match.PlayerTwoScore += 5;
                }
            }
        }

        // Does one player have 3 stones in a row?
        var winner = Guid.Empty;
        var threeInARow = HasThreeInARow(match.Stones);
        if (threeInARow != 0)
            winner = threeInARow > 0 ? match.PlayerOneId : match.PlayerTwoId;

        if (winner == Guid.Empty) // No Winner yet
        {
            // Have all sticks been played, if yes, use top score.
            if (AllSticksHaveBeenPlayed(match))
            {
                winner = match.PlayerOneScore > match.PlayerTwoScore ? match.PlayerOneId : match.PlayerTwoId;
            }
        }

        if (winner == Guid.Empty)
        {
            match.NextPlayerId = args.Player.Id == match.PlayerOneId ? match.PlayerTwoId : match.PlayerOneId;
        }
        else
        {
            match.NextPlayerId = Guid.Empty;
            match.WinnerId = winner;
            match.Completed = true;
        }

        await SaveMatchAndSendUpdates(context, match);

        return new OkObjectResult(new ProcessTurnResponse(match));
    }

    private Exception? ValidateProcessTurnRequest(ProcessTurnRequest args)
    {
        if (args.MatchId == Guid.Empty)
        {
            return new ArgumentException("Invalid MatchId");
        }
        if (args.Player is null)
        {
            return new ArgumentException("Invalid Player");
        }
        if (args.Position < 0 || args.Position > 23)
        {
            return new IndexOutOfRangeException("Position is out of range, must be between 1 and 24");
        }

        return null;
    }

    private Exception? VerifyMatchState(Match match, ProcessTurnRequest args)
    {
        if (match is null)
        {
            return new ArgumentException("Invalid MatchId");
        }

        if (match.WinnerId != Guid.Empty)
        {
            return new ArgumentException("Match is complete");
        }

        if (args.Player.Id != match.NextPlayerId)
        {
            return new ArgumentException($"It is not {args.Player.GamerTag}'s turn");
        }

        if (match.Sticks[args.Position] != 0)
        {
            return new ArgumentException($"Position [{args.Position}] has already been played");
        }

        return null;
    }

    private static bool AllSticksHaveBeenPlayed(Match match)
    {
        return !(from s in match.Sticks where s == 0 select s).Any();
    }

    private static int HasThreeInARow(List<int> stones)
    {
        for (var rc = 0; rc < 3; rc++)
        {
            var rowStart = rc * 3;
            var rowValue = stones[rowStart] + stones[rowStart + 1] + stones[rowStart + 2];
            if (Math.Abs(rowValue) == 3) // we Have a winner!

            {
                return rowValue;
            }

            var colStart = rc;
            var colValue = stones[colStart] + stones[colStart + 3] + stones[colStart + 6];
            if (Math.Abs(colValue) == 3) // We have a winner!
            {
                return colValue;
            }
        }
        var tlbrValue = stones[0] + stones[4] + stones[8];
        var trblValue = stones[2] + stones[4] + stones[6];
        if (Math.Abs(tlbrValue) == 3) { return tlbrValue; }
        if (Math.Abs(trblValue) == 3) { return trblValue; }
        return 0;
    }

    private async Task SaveMatchAndSendUpdates(GameDbContext context, Match match)
    {
        context.Matches.Update(match);
        await context.SaveChangesAsync();
        await Clients.Group($"Match[{match.Id}]").MatchUpdated(new MatchUpdatedEventArgs(match));
        if (match.Completed)
        {
            await UserGroups.RemoveFromGroupAsync(match.PlayerOneId.ToString(), $"Match[{match.Id}]");
            await UserGroups.RemoveFromGroupAsync(match.PlayerTwoId.ToString(), $"Match[{match.Id}]");
        }
    }

}
