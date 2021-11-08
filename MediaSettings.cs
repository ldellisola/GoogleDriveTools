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
        private static string SerializedStorage = @"MediaSettings.json";
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
        public List<Folder> folders = new List<Folder>();
        [JsonInclude]
        public List<string> allowedExtensions = new List<string>();

        //{ ".mkv", ".flv", ".mp4", ".mov", ".avi", ".wmv" };


        public class Folder
        {
            [JsonInclude]
            public string local { get; private set; }
            [JsonInclude]
            public string remote { get; private set; }

        }

        
    }
}
