using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace GHUploader.Models
{
    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public CustomMultipartFormDataStreamProvider(string path) : base(path) { }

        //.net forces certain file names for security, this overrides that method and lets us choose our own file name
        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            string str = headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            if (headers.ContentRange != null) //this is chunked upload, use different randomization
            {
                return RandomizeTempName() + str.Substring(str.LastIndexOf('.') + 1);
            }
            else
            {
                return RandomizeName() + str.Substring(str.LastIndexOf('.') + 1);
            }

        }

        //using the same file format as we use in classic.
        private string RandomizeName()
        {
            Random rand = new Random();
            int xVal = Convert.ToInt32(100000 * rand.NextDouble());
            DateTime localDate = DateTime.Now;

            return "File0" + localDate.Second + xVal + ".";
        }
        private string RandomizeTempName()
        {
            Random rand = new Random();
            int xVal = Convert.ToInt32(1000000 * rand.NextDouble());
            DateTime localDate = DateTime.Now;

            return "FileTMP" + xVal + ".";
        }
    }
}