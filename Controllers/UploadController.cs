using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading;
using System.Threading.Tasks;
using GHUploader.Controllers;
using System.IO;
using System.Diagnostics;

namespace GHUploader.Models
{
    public class UploadController : ApiController
    {
        [HttpPost]
        [ValidateMimeMultipartContentFilter]
        public async Task<IHttpActionResult> UploadDocument()
        {
            UploadFileService uploadFileService;
            UploadProcessingResult uploadResult;
            //Starts here, receieve the file data through the request header
            try { uploadFileService = new UploadFileService(Request); }
            catch(Exception ex){
                string errorMessage = ex.Message + " Failed to create UploadFileService.";

                return Content(HttpStatusCode.InternalServerError, errorMessage, Configuration.Formatters.JsonFormatter);
            }

            try { uploadResult = await uploadFileService.HandleRequest(Request); }
            catch (Exception ex){
                string errorMessage = ex.Message + " Failed to upload the result.";

                return Content(HttpStatusCode.InternalServerError, errorMessage, Configuration.Formatters.JsonFormatter);
            }

            if (uploadResult.IsComplete)
            {
                //if the upload is completed, send a json response with the file name so that the ado.net side can save to database.
                if (uploadResult.FileMetadata["isCustomSite"] == "true")
                {
                    //if the upload was actually a zipped file, just return the uploadResult. A bit of a hack. I'm sorry.
                    return Content(HttpStatusCode.OK, uploadResult, Configuration.Formatters.JsonFormatter);
                }
                string str = uploadResult.LocalFilePath;
                int pos = str.LastIndexOf('\\') + 1;

                return Content(HttpStatusCode.OK, str.Substring(pos,str.Length-pos), Configuration.Formatters.JsonFormatter);
            }

            return Ok(HttpStatusCode.Continue);

        }

        [HttpGet]
        //In a future iteration, we can save the file using an encryption/decryption method and use a get method to return the desired file name.
        public string GetDocument()
        {
            return "Success";
        }
    }
}
