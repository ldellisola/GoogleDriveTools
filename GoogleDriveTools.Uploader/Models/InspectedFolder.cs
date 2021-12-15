using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoogleDriveTools.Uploader.Models
{
    internal class InspectedFolder
    {
        public string Local { get; set; }
        public string Remote { get; set; }
    }   
}
