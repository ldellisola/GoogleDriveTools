using GoogleDriveTools.Uploader.Models;
using SimpleGoogleDrive;
using SimpleGoogleDrive.Exceptions;

namespace GoogleDriveTools.Uploader
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

                var driveSettings = new SimpleGoogleDrive.Models.DriveAuthorizationSettings(
                                                                        _configuration["GoogleDrive:AppName"],
                                                                        new FileInfo(_configuration["GoogleDrive:CredentialsPath"]),
                                                                        _configuration["GoogleDrive:DataStore"]
                                                                        );

                var mediaSettings = new MediaSettings(_configuration["GoogleDrive:MediaSettingsPath"]);

                using (var drive = new GoogleDriveService(driveSettings, true, _configuration["GoogleDrive:PathStorage"]))
                {
                    await drive.Authenticate();


                    var files = mediaSettings.folders.ConvertAll(folder =>
                                        {
                                            var files = new DirectoryInfo(folder.Local).GetFiles("*", SearchOption.AllDirectories)
                                                                           .Where(file => mediaSettings.allowedExtensions.Contains(file.Extension))
                                                                           .ToList();
                                            return (folder, files);
                                        })
                                        .SelectMany(t => t.files.ConvertAll(file => (t.folder, file)))
                                        .OrderBy(t => t.file.FullName);



                    foreach (var (folder, file) in files)
                    {
                        try
                        {
                            var resource = await drive.CreateFile(
                                file,
                                folder.Remote + file.FullName.Replace(folder.Local, ""),
                                onProgress: (prog, total) => _logger.LogInformation($"{file.Length / 1000000} MB : {100 * prog/total} % : {file.Name}"),
                                onFailure: (exception) => _logger.LogError($"File {file.FullName} couldn't be uploaded. Error: {exception}"),
                                token: stoppingToken
                                );
                            file.Delete();
                        }
                        catch (ResourceAlreadyExistsException)
                        {
                            _logger.LogInformation($"File {file.FullName} is already in Google Drive");
                        }
                        catch (ServiceNotAuthenticatedException)
                        {
                            _logger.LogCritical("Google Drive is not authenticated or not running");
                        }
                    }


                }

                _logger.LogInformation("Sleeping for 1 hour");
                await Task.Delay(60 * 60 * 1000, stoppingToken);
            }
        }
    }
}







