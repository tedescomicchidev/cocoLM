using Microsoft.EntityFrameworkCore;
using Portal.Application;
using Portal.Infrastructure.Data;
using Portal.Infrastructure.Services;
using Portal.Infrastructure.Settings;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ConfidentialSettings>(builder.Configuration.GetSection("Confidential"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<FederatedLearningService>();

builder.Services.AddHostedService<FederatedLearningWorker>();

var host = builder.Build();
await host.RunAsync();

public sealed class FederatedLearningWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public FederatedLearningWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<FederatedLearningService>();
            await service.AggregateAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
