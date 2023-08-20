﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SticksAndStones.Repository;

[assembly: FunctionsStartup(typeof(SticksAndStones.Startup))]

namespace SticksAndStones;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddDbContext<GameDbContext>(
            options =>
            {
                options.UseInMemoryDatabase("SticksAndStones");
            });
    }
}
