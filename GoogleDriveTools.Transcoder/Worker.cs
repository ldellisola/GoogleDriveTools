using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

namespace GoogleDriveTools.Transcoder
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        private readonly int _transcoderThreads = 4;
        private readonly int _uploaderThreads = 1;
        private readonly int _downloaderThreads = 1;
        private readonly bool _useHardwareAcceleration = true;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _downloaderThreads = configuration.GetValue("download-threads", _downloaderThreads);
            _transcoderThreads = configuration.GetValue("transcode-threads", _transcoderThreads);
            _uploaderThreads = configuration.GetValue("upload-threads", _uploaderThreads);
            _useHardwareAcceleration = configuration.GetValue("use-hardware-acceleration", _useHardwareAcceleration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);


                var a = _configuration.GetValue<string>("test");

                var driveSettings = new DriveAuthorizationSettings(
                                                                        _configuration["GoogleDrive:AppName"],
                                                                        new FileInfo(_configuration["GoogleDrive:CredentialsPath"]),
                                                                        _configuration["GoogleDrive:DataStore"]
                                                                        );


                using (var drive = new GoogleDriveService(driveSettings, true, _configuration["GoogleDrive:PathStorage"]))
                {
                    await drive.Authenticate();
                }


                    await Task.Delay(1000, stoppingToken);
            }
        }
    }
}