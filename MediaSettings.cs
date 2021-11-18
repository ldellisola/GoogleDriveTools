using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogleDrive
{
    public class MediaSettings
    {
        [JsonIgnore]
        private static MediaSettings Reference { get; set; } = null;
        [JsonIgnore]
        private static readonly string SerializedStorage = @"MediaSettings.json";
        public static MediaSettings GetInstance()
        {
            if (Reference != null)
                return Reference;

            if (File.Exists(SerializedStorage))
            {
                Reference = JsonSerializer.Deserialize<MediaSettings>(File.ReadAllText(SerializedStorage));
            }
            else
            {
                Reference = new MediaSettings();
            }

            return Reference;
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
