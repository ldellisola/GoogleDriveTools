using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogleDriveTools.Transcoder
{
    internal class MediaSettings
    {

        [JsonIgnore]
        private readonly string serializedStorage  ;

        public MediaSettings(string storagePath = @"MediaSettings.json")
        {
            this.serializedStorage = storagePath;
            var settings = JsonSerializer.Deserialize<MediaSettings>(File.ReadAllText(serializedStorage));
            if (settings != null)
            {
                this.allowedExtensions = settings.allowedExtensions;
                this.folders = settings.folders;
            }
        }


        [JsonInclude]
        public List<Folder> folders = new();
        [JsonInclude]
        public List<string> allowedExtensions = new();

        public class Folder
        {
            [JsonInclude]
            public string local { get; private set; }
            [JsonInclude]
            public string remote { get; private set; }

        }


    }
}
}
