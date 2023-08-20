
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SticksAndStones.Helpers;
using SticksAndStones.Messages;
using SticksAndStones.Models;
using SticksAndStones.Repository;
using DtoGame = SticksAndStones.Repository.Dto.Game;
using DtoPlayer = SticksAndStones.Repository.Dto.Player;

namespace SticksAndStones.Functions;

public class LobbyService
{
    readonly GameContext context;

    public LobbyService(GameContext dbContext)
    {
        this.context = dbContext;
    }

    [FunctionName("CheckPhoto")]
    public async Task<IActionResult> CheckPhoto(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Lobby/CheckPhoto")] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Received request for photo check.");
        // Process arguments
        using var sr = new StreamReader(req.Body);
        var strMsg = await sr.ReadToEndAsync();
        using var jsonReader = new JsonTextReader(new StringReader(strMsg));
        Message obj = new JsonSerializer().Deserialize<Message>(jsonReader);
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(Message));
        var msgType = assembly.GetType(obj.TypeName);

        if (msgType is not null && msgType == typeof(PhotoMessage))
        {
            log.LogInformation("Checking photo for adult content.");

            var photoMessage = (PhotoMessage)new JsonSerializer().Deserialize(new JsonTextReader(new StringReader(strMsg)), msgType);

            var bytes = Convert.FromBase64String(photoMessage.Base64Photo);

            var stream = new MemoryStream(bytes);
            var subscriptionKey = Environment.GetEnvironmentVariable("ComputerVisionKey");
            var computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey), Array.Empty<DelegatingHandler>())
            {
                Endpoint = Environment.GetEnvironmentVariable("ComputerVisionEndpoint")
            };

            var features = new List<VisualFeatureTypes?>() { VisualFeatureTypes.Adult };

            var result = await computerVision.AnalyzeImageInStreamAsync(stream, features);

            if (result.Adult.IsAdultContent)
            {
                log.LogInformation("photo contained adult content.");
                return new OkObjectResult(new PhotoUrlMessage(photoMessage.PlayerId)); // Image checked, and flagged, object returned with null properties
            }

            var url = await StorageHelper.Upload(bytes, photoMessage.ImageFormat);

            var msg = new PhotoUrlMessage(photoMessage.PlayerId)
            {
                Id = photoMessage.Id,
                Timestamp = photoMessage.Timestamp,
                Url = url
            };

            log.LogInformation("photo contained adult content.");
            return new OkObjectResult(msg); // Image checked and is valid, valid object returned 
        }
        log.LogInformation("Invalid message received.");
        return new OkResult(); // No object returned, bad message
    }

    [FunctionName("Connect")]
    public async Task<IActionResult> Connect(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Lobby/Connect")] HttpRequest req,
        [SignalR(HubName = "lobby")] IAsyncCollector<SignalRMessage> signalRMessages,
        ILogger log)
    {
        // Process arguments
        using StreamReader streamReader = new(req.Body);
        var requestBody = await streamReader.ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        var newPlayer = new Player()
        {
            Id = data?.id,
            Name = data?.name,
            EmailAddress = data?.emailAddress,
            AvatarUrl = data?.avatarUrl
        };

        log.LogInformation($"New connection established for {newPlayer.Name}");

        // Check for player
        var thisPlayer = (from player in context.Players
                         where string.Equals(player.EmailAddress, newPlayer.EmailAddress, StringComparison.OrdinalIgnoreCase)
                         select player).FirstOrDefault();
                        
        if (thisPlayer is null)
        {
            thisPlayer = new DtoPlayer(newPlayer);
            // Add new player to list of players
            try
            {
                context.Players.Add(thisPlayer);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to add player to list of available players");
            }
        }

        thisPlayer.LastConnectTime = DateTime.UtcNow;
        context.Players.Update(thisPlayer);

        // Reset newPlayer to current values
        newPlayer = thisPlayer.ToModel();
        if (newPlayer.InGame) {

        }
        // Send PlayerConnected Message
        await signalRMessages.AddAsync(new SignalRMessage
        {
            Target = SticksAndStones.Constants.PlayerStatusChanged,
            Arguments = new[] { new PlayerStatusChanged(newPlayer, true, thisPlayer.InGame()) }
        });

        // Get the current set of available players
        var players = (from player in context.Players
                       where string.IsNullOrEmpty(player.GameId) && player.Id != thisPlayer.Id
                       select player.ToModel()).ToList();

        // return list of available players
        return new OkObjectResult(new ConnectResult() { Player = newPlayer, Players = players });
    }

    [FunctionName("GetPlayers")]
    public async Task<IActionResult> GetPlayers(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Lobby/GetPlayers")] HttpRequest req,
    ILogger log)
    {
        // Process arguments
        using StreamReader streamReader = new(req.Body);
        var requestBody = await streamReader.ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        var thisPlayer = new Player()
        {
            Id = data?.id,
            Name = data?.name
        };

        log.LogInformation($"Refreshing Player list for {thisPlayer.Name}");

        // Get the current set of available players
        var players = (from player in context.Players
                       where string.IsNullOrEmpty(player.GameId) && player.Id != thisPlayer.Id
                       select player.ToModel()).ToList();

        // return list of available players
        return new OkObjectResult( players );
    }

    [FunctionName("Challenge")]
    public async Task Challenge(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Lobby/Challenge")] HttpRequest req,
        [SignalR(HubName = "lobby")] IAsyncCollector<SignalRMessage> signalRMessages,
        ILogger log)
    {
        // Process arguments
        using StreamReader streamReader = new(req.Body);
        var requestBody = await streamReader.ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string challengerId = data.challenger.id;
        var challengerDto = (from p in context.Players
                          where p.Id == challengerId
                          select p).FirstOrDefault();

        string playerId = data.opponent.id;
        var playerDto = (from p in context.Players
                      where p.Id == playerId
                      select p).FirstOrDefault();
                       
        ChallengeStatus status = (ChallengeStatus)Enum.Parse(typeof(ChallengeStatus), data.status.ToString());
        string gameId = null;

        if (challengerDto is null || playerDto is null)
            return;
        if (status == ChallengeStatus.Declined)
        {
            log.LogInformation($"{playerDto.Name} has declined the challenge from {challengerDto.Name}!");
        }
        if (status == ChallengeStatus.Accepted)
        {
            log.LogInformation($"{playerDto.Name} has accepted the challenge from {challengerDto.Name}!");

            var gameDto = new DtoGame() {
                PlayerOneId = playerId,
                PlayerTwoId = challengerId,
                PlayerOneScore = 0,
                PlayerTwoScore = 0,
                NextPlayer = playerId,
                Sticks = new List<int>(24),
                Stones = new List<int>(9)
            };
            context.Games.Add(gameDto);

            playerDto.GameId = challengerDto.GameId = gameDto.Id;
            
            context.SaveChanges();

            gameId = gameDto.Id;
        }
        if (status == ChallengeStatus.Issued)
        {
            log.LogInformation($"{challengerDto.Name} has challenged {playerDto.Name} to a match!");
        }
        await signalRMessages.AddAsync(new SignalRMessage
        {
            Target = SticksAndStones.Constants.Challenge,
            Arguments = new[] { new PlayerChallenge(challengerDto.ToModel(), playerDto.ToModel(), status, gameId) }
        });
        await signalRMessages.AddAsync(new SignalRMessage
        {
            Target = SticksAndStones.Constants.PlayerStatusChanged,
            Arguments = new[] { new PlayerStatusChanged(playerDto.ToModel(), false, true) }
        });
        await signalRMessages.AddAsync(new SignalRMessage
        {
            Target = SticksAndStones.Constants.PlayerStatusChanged,
            Arguments = new[] { new PlayerStatusChanged(challengerDto.ToModel(), false, true) }
        });

        return;
    }

    [FunctionName("Disconnect")]
    public async Task<IActionResult> Disconnect(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"Lobby/Disconnect")] HttpRequest req,
        [SignalR(HubName = "lobby")] IAsyncCollector<SignalRMessage> signalRMessages,
        ILogger log)
    {
        // Process arguments
        using StreamReader streamReader = new(req.Body);
        var requestBody = await streamReader.ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string playerId = data.id;
        var player = (from p in context.Players
                      where p.Id == playerId
                      select p).FirstOrDefault();

        log.LogInformation($"{player.Name} has left.");

        // Remove player from any game they are currently playing

        // Remove player from list of players
        try
        {
            context.Players.Remove(player);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to remove player from list of available players");
        }

        // Send PlayerDisconnected Message
        await signalRMessages.AddAsync(new SignalRMessage
        {
            Target = SticksAndStones.Constants.PlayerStatusChanged,
            Arguments = new[] { new PlayerStatusChanged(player.ToModel(), false, false) }
        });

        return new OkResult();
    }
}

