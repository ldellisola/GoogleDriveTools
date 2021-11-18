using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleDrive
{
    public class GoogleDriveService
    {
        private readonly Settings settings = null;
        private DriveService service;

        public GoogleDriveService(Settings settings_)
        {
            this.settings = settings_;
        }

        public GoogleDriveService(string ApplicationName_, string CretentialsPath_, string UserCredentialPath_)
        {
            this.settings = new Settings
            {
                ApplicationName = ApplicationName_,
                CredentialsPath = CretentialsPath_,
                UserCredentialPath = UserCredentialPath_
            };
        }

        public async Task SignIn(CancellationToken token = new CancellationToken())
        {
            UserCredential credential;

            var clientSecrets = await GoogleClientSecrets.FromFileAsync(this.settings.CredentialsPath, token);

            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                this.settings.Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(this.settings.UserCredentialPath, true));


            if (credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
            {
                Log.Information("Auth token is expired. Attempting to refresh");
                if (await credential.RefreshTokenAsync(token))
                    Log.Information("Auth token was refreshed");
                else
                    Log.Warning("Auth token could not be refreshed");
            }

            this.service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.settings.ApplicationName
            });


        }

        public void Stop()
        {
        }

        public async Task<string> CreateFolder(string folderName, string parent = "")
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = MimeType.Folder.GetString()
            };

            var parentId = PathStorage.GetInstance()[parent];



            if (parentId != "")
            {
                fileMetadata.Parents = new List<string>() { parentId };
            }


            var request = service.Files.Create(fileMetadata);

            var result = await request.ExecuteAsync(CancellationToken.None);

            PathStorage.GetInstance()[$"{parent}/{folderName}"] = result.Id;

            return result.Id;
        }

        public async Task UploadFile(string path, string destination)
        {
            destination = destination.Replace("\\", "/");
            destination = destination.Remove(destination.LastIndexOf('/'), destination.Length - destination.LastIndexOf('/'));
            var parentId = await FindNestedFolder(destination);
            var fileinfo = new FileInfo(path);


            var t = await GetFileId(fileinfo.Name, destination);

            Log.Information($"Processing file: {fileinfo.FullName}");


            if (t != null)
            {
                Log.Debug($"File already exists");
                return;
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileinfo.Name,
                Parents = new List<string>() { parentId },

            };

            await using var source = new FileStream(fileinfo.FullName, FileMode.Open, FileAccess.Read);

            var request = service.Files.Create(fileMetadata, source, fileinfo.MimeType().GetString());
            request.Fields = "*";

            int parts = 50;

            Console.WriteLine($"Uploading file: {fileinfo.FullName}");

            var (Left, Top) = Console.GetCursorPosition();


            for (var j = 0; j < parts; j++)
            {
                Console.Write("-");
            }
            Console.Write(" | ");


            Console.Write($"0/{fileinfo.Length / 1000000} MB | ");

            Console.Write("0 s");


            long totalLength = request.ContentStream.Length;
            long bytesSent = 0;
            var startingTime = DateTime.Now;

            request.ProgressChanged += prog =>
            {
                bytesSent = prog.BytesSent;


                Console.SetCursorPosition(Left, Top);

                int slotsSent = (int)((float)parts * bytesSent / totalLength);


                for (int i = 0; i < parts; i++)
                {
                    if (i < slotsSent)
                        Console.Write("#");
                    else
                        Console.Write("-");
                }
                Console.Write(" | ");

                Console.Write($"{bytesSent / 1000000}/{totalLength / 1000000} MB | ");

                Console.Write($"{(int)(DateTime.Now - startingTime).TotalSeconds} s | ");

                if (prog.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Completed       ");
                    Console.ResetColor();
                }
                else if (prog.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR         ");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("In Progress      ");
                    Console.ResetColor();
                }

            };

            try
            {

                var result = await request.UploadAsync(CancellationToken.None);

                Console.WriteLine();

                if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    Log.Error(result.Exception.Message);
                }
                else
                {
                    Log.Information($"File uploaded: {fileinfo.FullName}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                throw;
            }

        }

        public async Task<string> FindNestedFolder(string nestedFolder)
        {
            Log.Debug($"Finding path: {nestedFolder}");
            var pathId = PathStorage.GetInstance()[nestedFolder];

            if (pathId != null)
                return pathId;



            var folders = nestedFolder.Split('/');
            var folderIDs = new List<string>();

            for (int i = 0; i < folders.Length; i++)
            {
                if (i == 0)
                {
                    Log.Debug($"Looking for {folders[0]}");
                    folderIDs.Add((await GetFolderId(folders[0])));
                }
                else
                {
                    string parent = folders.Take(i).Aggregate("", (t, r) => t += $"{r}/").TrimEnd('/');
                    Log.Debug($"Looking for {folders[i]} at {parent}");
                    var id = await GetFolderId(folders[i], parent);

                    if (id == null)
                    {
                        Log.Debug($"Folder doesn't exists, creating it");
                        id = await CreateFolder(folders[i], parent);
                    }
                    folderIDs.Add(id);

                }
            }

            return folderIDs.Last();
        }

        public async Task<string> GetFolderId(string folder, string parent = null)
        {

            var parentId = PathStorage.GetInstance()[parent];
            var request = service.Files.List();
            request.Q += " mimeType = 'application/vnd.google-apps.folder' ";
            request.Q += $" and name = '{folder.Replace(@"'", @"\'")}'";
            request.Q += $" and 'ldellisola@itba.edu.ar' in owners";

            string pathId;
            if (parentId != null)
            {
                request.Q += $" and '{parentId}' in parents ";
                pathId = PathStorage.GetInstance()[$"{parent}/{folder}"];
            }
            else
                pathId = PathStorage.GetInstance()[$"{folder}"];


            if (pathId != null)
                return pathId;

            try
            {
                var result = await request.ExecuteAsync();

                if (result.Files.Count == 0)
                    return null;
                else
                {
                    PathStorage.GetInstance()[$"{parent}/{folder}"] = result.Files.FirstOrDefault().Id;
                    return result.Files.FirstOrDefault().Id;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Query: {request.Q}");
                Log.Error($"Exception: {e.Message}");
                throw;
            }
        }

        public async Task<string> GetFileId(string fileName, string parent = null)
        {
            var parentId = PathStorage.GetInstance()[parent];


            var request = service.Files.List();
            request.Q += $" name = '{fileName.Replace(@"'", @"\'")}'";
            request.Q += " and mimeType != 'application/vnd.google-apps.folder' ";
            request.Q += $" and 'ldellisola@itba.edu.ar' in owners";

            if (parentId != null)
            {
                request.Q += $" and '{parentId}' in parents ";
            }
            try
            {

                var result = await request.ExecuteAsync();

                if (result.Files.Count == 0)
                    return null;
                else
                {
                    return result.Files.FirstOrDefault().Id;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Query: {request.Q}");
                Log.Error($"Exception: {e.Message}");
                throw;
            }
        }


        public enum MimeType
        {
            [StringValue("unknown/unkown")]
            Unknown = -1,
            [StringValue("application/vnd.google-apps.folder")]
            Folder,
            [StringValue("video/x-matroska")]
            MKV,
            [StringValue("video/x-flv")]
            FLV,
            [StringValue("video/mp4")]
            MP4,
            [StringValue("video/quicktime")]
            MOV,
            [StringValue("video/x-msvideo")]
            AVI,
            [StringValue("video/x-ms-wmv")]
            WMV,
            [StringValue("text/plain")]
            TXT
        }


        public class Settings
        {
            public string ApplicationName { get; set; }
            public string CredentialsPath { get; set; }
            public string UserCredentialPath { get; set; }

            public string[] Scopes = { DriveService.Scope.Drive };

        }
    }
}
