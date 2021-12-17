using GoogleDriveTools.Transcoder;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureAppConfiguration(app =>
    {
        app.AddCommandLine(args);
    })
    .Build();

await host.RunAsync();
