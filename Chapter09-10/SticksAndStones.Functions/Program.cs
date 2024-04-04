using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SticksAndStones.Repository;
using SticksAndStones.Hubs;
using SticksAndStones.Handlers;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddDbContextFactory<GameDbContext>(
            options =>
            {
                options.UseInMemoryDatabase("SticksAndStones");
            });
        services.AddServerlessHub<GameHub>();
        services.AddSingleton<ChallengeHandler>();
    })
    .Build();

host.Run();