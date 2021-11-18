using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using System;
using Google.Apis.Drive.v3;

namespace GoogleDrive
{
    class Program
    {
        static readonly string ApplicationName = "GDrive Uploader";
        static readonly string Credentials = "Credentials.json";
        static readonly string UserCreadentials = "token.json";
        static readonly string LogFile = "GoogleDrive.log";

        static async Task Main()
        {

            // Set up
            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(LogFile, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);

            Log.Logger = loggerConfiguration.CreateLogger();

            var driveService = new GoogleDriveService(new GoogleDriveService.Settings
            {
                ApplicationName = ApplicationName,
                CredentialsPath = Credentials,
                UserCredentialPath = UserCreadentials,
                Scopes = new string[1] { DriveService.Scope.Drive }
            });

            await driveService.SignIn();

            var paths = PathStorage.GetInstance();
            var mediaSettings = MediaSettings.GetInstance();

            foreach (var folder in mediaSettings.folders)
            {
                if (paths[folder.remote] == null)
                    paths[folder.remote] = await driveService.GetFolderId(folder.remote);
            }

            var files = mediaSettings.folders
                            .ConvertAll(folder =>
                            {
                                var files = mediaSettings.allowedExtensions
                                             .ConvertAll(ext => System.IO.Directory.GetFiles(folder.local, $"*.{ext}", SearchOption.AllDirectories))
                                             .SelectMany(t => t);

                                return Tuple.Create(folder,files);
                            })
                            .SelectMany(tuple => tuple.Item2.ToList().ConvertAll(r => Tuple.Create(tuple.Item1, r)))
                            .OrderBy(tuple => tuple.Item2)
                            .ToList();

            // Upload
            foreach (var fileTuple in files)
            {
                var file = fileTuple.Item2;
                var folder = fileTuple.Item1;

                try
                {
                    await driveService.UploadFile(file, folder.remote + file.Remove(0, folder.local.Length));
                }
                catch
                {
                    Log.Error($"Upload failed: {file}");
                }
            }

            // Shut down
            driveService.Stop();
            PathStorage.Store();

        }
    }
}
