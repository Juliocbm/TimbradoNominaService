using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nomina.WorkerTimbrado;
using Nomina.WorkerTimbrado.Models;
using Nomina.WorkerTimbrado.Services;
using Polly;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerSettings>(context.Configuration.GetSection("WorkerSettings"));
        services.AddHttpClient<TimbradoApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

        services.AddTransient<LiquidacionRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
            return new LiquidacionRepository(settings.ConnectionString);
        });

        services.AddHostedService<Worker>();
    });

var host = builder.Build();
await host.RunAsync();
