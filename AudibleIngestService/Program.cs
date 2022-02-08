using AudibleIngestService;
using System.Diagnostics;
using Serilog;

string runningInDirectory = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builder, services) =>
    {
        services.Configure<ServiceConfiguration>(builder.Configuration.GetRequiredSection("AppSettings"));
        services.AddHostedService<Worker>();
    })
    .UseContentRoot(runningInDirectory)
    .UseWindowsService();

IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(runningInDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

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
