using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace GHUploader.Models
{
    public class UploadProcessingResult
    {
        public bool IsComplete { get; set; }

        public string FileName { get; set; }

        public string LocalFilePath { get; set; }

        public NameValueCollection FileMetadata { get; set; }
    }
}