using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

namespace GoogleDriveTools.Eraser
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);


                var driveSettings = new DriveAuthorizationSettings(
                                                                   _configuration["GoogleDrive:AppName"],
                                                                   new FileInfo(_configuration["GoogleDrive:CredentialsPath"]),
                                                                   _configuration["GoogleDrive:DataStore"]
                                                                  );


                using (var drive = new GoogleDriveService(driveSettings, true, _configuration["GoogleDrive:PathStorage"]))
                {
                    await drive.Authenticate();

                    var query = new QueryBuilder().TypeNotContains("video").And().TypeNotContains("folder").And().IsOwner("ldellisola@itba.edu.ar");

                    foreach (var folderName in _configuration.GetSection("GoogleDrive:MonitoredFolders").Get<List<string>>())
                    {
                        var folder = await drive.FindFolder(folderName, token: stoppingToken);

                        if (folder == null)
                            continue;

                        var resources = await folder.GetInnerResources(query, true, stoppingToken);

                        _logger.LogInformation($"{resources.Count()} resources to delete in {folderName}");

                        foreach (var resource in resources)
                        {
                            _logger.LogInformation($"Deleting {resource.Name}");
                            await resource.Delete(stoppingToken);
                        }
                    }

                }

                _logger.LogInformation("Fihished deleting files");

                await Task.Delay(60 * 60 * 1000, stoppingToken);
            }
        }
    }
}