using SticksAndStones.Models;

namespace SticksAndStones.Services;

public partial class GameService
{
    private ServiceConnection service;
    private Settings settings;
    public GameService(ServiceConnection service, Settings settings)
    {
        this.service = service;
        this.settings = settings;
    }
    public async Task<Game?> GetGameByIdAsync(string gameId)
    {
        (Game? game, AsyncError? error) = await service.PostAsync<Game>(new($"{settings.ServerUrl}/Game/GetGameById"), gameId);
        if (error != null)
        {
            return null;
        }
        return game;
    }
}
