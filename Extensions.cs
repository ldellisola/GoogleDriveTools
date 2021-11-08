using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDrive
{
    public static class Extensions
    {
        public static GoogleDriveService.MimeType MimeType(this FileInfo f)
        {
            switch (f.Extension.ToLower())
            {
                case ".mkv":
                    return GoogleDriveService.MimeType.MKV;
                case ".flv":
                    return GoogleDriveService.MimeType.FLV;
                case ".mp4":
                    return GoogleDriveService.MimeType.MP4;
                case ".mov":
                    return GoogleDriveService.MimeType.MOV;
                case ".avi":
                    return GoogleDriveService.MimeType.AVI;
                case ".wmv":
                    return GoogleDriveService.MimeType.WMV;
                case ".txt":
                    return GoogleDriveService.MimeType.TXT;
                default:
                    return GoogleDriveService.MimeType.Unknown;
            }
        }

        public static string GetString(this Enum value){
            // Get the type
            Type type = value.GetType();

            // Get fieldinfo for this type
            FieldInfo fieldInfo = type.GetField(value.ToString());

            if (fieldInfo == null)
            {
                return "";
            }
            // Get the stringvalue attributes
            StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
                typeof(StringValueAttribute), false) as StringValueAttribute[];

            // Return the first if there was a match.
            return attribs.Length > 0 ? attribs[0].value : null;
        }
    }
}
