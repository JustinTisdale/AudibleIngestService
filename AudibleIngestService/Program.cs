using AudibleIngestService;
using System.Diagnostics;
using Serilog;

string runningInDirectory = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
            .AddJsonFile("appsettings.json")
            .Build();

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ServiceConfiguration>(configuration.GetRequiredSection("AppSettings").Get<ServiceConfiguration>());
        services.AddHostedService<Worker>();
    })
    .UseContentRoot(runningInDirectory)
    .UseWindowsService();

string logFileName = configuration["AppSettings:LogFileName"];
Console.WriteLine($"Log file name is {logFileName}");

if (!string.IsNullOrWhiteSpace(logFileName))
{
    var fullPath = Path.Combine(runningInDirectory, logFileName);
    Console.WriteLine($"Log file path is {fullPath}");
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.File(fullPath, flushToDiskInterval: TimeSpan.FromSeconds(10))
        .CreateLogger();

    builder = builder.UseSerilog();
}

IHost host = builder.Build();

await host.RunAsync();
