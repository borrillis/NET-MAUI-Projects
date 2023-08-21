﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using SticksAndStones.Messages;
using SticksAndStones.Repository;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace SticksAndStones.Hubs
{
    internal class GameHub : ServerlessHub
    {
        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        GameDbContext context;

        public GameHub(GameDbContext dbcontext)
        {
            context = dbcontext;
        }

        [FunctionName("Connect")]
        public async Task<IActionResult> Connect(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("A new client is requesting connection");

                var result = await JsonSerializer.DeserializeAsync<ConnectRequest>(req.Body, jsonOptions);
                var newPlayer = result.Player;

                if (newPlayer is null)
                {
                    var error = new ArgumentException("No player data.", "Player");
                    log.LogError(error, "Failure to deserialize arguments");
                    return new BadRequestObjectResult(error);
                }

                if (string.IsNullOrEmpty(newPlayer.GamerTag))
                {
                    var error = new ArgumentException("A GamerTag is required for all players.", "GamerTag");
                    log.LogError(error, "Invalid value for GamerTag");
                    return new BadRequestObjectResult(error);
                }

                if (string.IsNullOrEmpty(newPlayer.EmailAddress))
                {
                    var error = new ArgumentException("An Email Address is required for all players.", "EmailAddress");
                    log.LogError(error, "Invalid value for EmailAddress");
                    return new BadRequestObjectResult(error);
                }

                log.LogInformation("Checking for GameTag usage");
                var gamerTagInUse = (from p in context.Players
                                     where string.Equals(p.GamerTag, newPlayer.GamerTag, StringComparison.OrdinalIgnoreCase)
                                     && !string.Equals(p.EmailAddress, newPlayer.EmailAddress, StringComparison.OrdinalIgnoreCase)
                                     select p).Any();
                if (gamerTagInUse)
                {
                    var error = new ArgumentException($"The GamerTag {newPlayer.GamerTag} is in use, please choose another.", "GamerTag");
                    log.LogError(error, "GamerTag in use.");
                    return new BadRequestObjectResult(error);
                }

                log.LogInformation("Locating Player record.");
                var thisPlayer = (from p in context.Players
                                  where string.Equals(p.EmailAddress, newPlayer.EmailAddress, StringComparison.OrdinalIgnoreCase)
                                  select p).FirstOrDefault();
                if (thisPlayer is null)
                {
                    log.LogInformation("Player not found, creating.");
                    thisPlayer = newPlayer;
                    thisPlayer.Id = Guid.NewGuid();
                    context.Add(thisPlayer);
                    await context.SaveChangesAsync();
                }

                log.LogInformation("Updating player login time.");
                thisPlayer.GamerTag = newPlayer.GamerTag;
                thisPlayer.LastSeen = DateTime.UtcNow;
                context.Players.Update(thisPlayer);
                await context.SaveChangesAsync();

                // Notify other connected players that a new player has connected
                log.LogInformation("Notifying connected players of new player.");
                await Clients.All.SendCoreAsync("PlayerStatusChanged", new[] { new PlayerStatusUpdateMessage(thisPlayer) });

                // Get the set of available players
                log.LogInformation("Getting the set of available players.");
                var players = (from player in context.Players
                               where player.Game == null && player.Id != thisPlayer.Id
                               select player).ToList();
                var connectionInfo = await NegotiateAsync(new NegotiationOptions() { UserId = thisPlayer.Id.ToString() });

                log.LogInformation("Creating response.");
                var connectResponse = new ConnectResponse()
                {
                    Player = thisPlayer,
                    Players = players,
                    ConnectionInfo = new Models.ConnectionInfo { Url = connectionInfo.Url, AccessToken = connectionInfo.AccessToken }
                };

                log.LogInformation("Sending response.");
                return new OkObjectResult(connectResponse);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception");
                return new BadRequestObjectResult(ex);

            }
        }
    }
}
