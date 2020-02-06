using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Web.Http;
using System.Web.Configuration;
using System.IO.Compression;
using System;
using System.Configuration;

namespace GHUploader.Models
{
    public class UploadFileService
    {
        private readonly string _epuDataPath;
        private readonly string _database;
        private readonly string _folder;
        private readonly CustomMultipartFormDataStreamProvider _streamProvider;

        public UploadFileService(HttpRequestMessage request)
        {
            if (request.Headers.Contains("database"))
            {
                _database = request.Headers.GetValues("database").FirstOrDefault();
            }
            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("Database not found."))
                };
                throw new HttpResponseException(resp);
            }
            if (request.Headers.Contains("folder"))
            {
                _folder = request.Headers.GetValues("folder").FirstOrDefault();
            }
            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format(" Requested save folder not found."))
                };
                throw new HttpResponseException(resp);
            }

            if (_database == "")
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Could not retrieve client header.")
                };
                throw new HttpResponseException(resp);
            }

            if (_folder == "base" || _folder=="")
            {
                _epuDataPath = ConfigurationManager.AppSettings["EPUDataPath"].ToString() + _database + @"\";
            }
            else
            {
                _epuDataPath = ConfigurationManager.AppSettings["EPUDataPath"].ToString() + _database + @"\" + _folder + @"\";
            }

            try
            {
                _streamProvider = new CustomMultipartFormDataStreamProvider(_epuDataPath);
            }
            catch
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format(" There was an error finding the file path: " + _epuDataPath))
                };
                throw new HttpResponseException(resp);
            }
        }

#region Interface

        public async Task<UploadProcessingResult> HandleRequest(HttpRequestMessage request)
        {
            await request.Content.ReadAsMultipartAsync(_streamProvider);
            return await ProcessFile(request);
        }

#endregion

#region Private implementation

        private async Task<UploadProcessingResult> ProcessFile(HttpRequestMessage request)
        {
            if (request.IsChunkUpload())
            {
                return await ProcessChunk(request);
            }

            if (IsConEdZipFile)
            {
                // Check contents of file for "presentation_html5.html". If exists: extract file and link to index.html.
                var customSitGHcessingResult = HandleZipFile();
                if (customSitGHcessingResult != null) return customSitGHcessingResult;
            }

            return new UploadProcessingResult()
            {
                IsComplete = true,
                FileName = OriginalFileName,
                LocalFilePath = LocalFileName,
                FileMetadata = _streamProvider.FormData
            };
        }

        private async Task<UploadProcessingResult> ProcessChunk(HttpRequestMessage request)
        {
            //use the unique identifier sent from client to identify the file
            FileChunkMetaData chunkMetaData = request.GetChunkMetaData();
            string filePath = Path.Combine(_epuDataPath, string.Format("{0}.temp", chunkMetaData.ChunkIdentifier));
            //append chunks to construct original file
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate | FileMode.Append))
            {
                var localFileInfo = new FileInfo(LocalFileName);
                var localFileStream = localFileInfo.OpenRead();

                await localFileStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();

                fileStream.Close();
                localFileStream.Close();

                //delete chunk
                localFileInfo.Delete();
                //if this is the last chunk, rename file
                if (chunkMetaData.IsLastChunk)
                {
                    File.Move(filePath, LocalFileName);

                    if (IsConEdZipFile)
                    {
                        var customSitGHcessingResult = HandleZipFile();
                        if (customSitGHcessingResult != null) return customSitGHcessingResult;
                    }
                }
            }

            return new UploadProcessingResult()
            {
                IsComplete = chunkMetaData.IsLastChunk,
                FileName = OriginalFileName,
                LocalFilePath = chunkMetaData.IsLastChunk ? LocalFileName : null,
                FileMetadata = _streamProvider.FormData
            };

        }

        private UploadProcessingResult HandleZipFile()
        {
            try
            {
                var RelativePath = _database + @"\" + LocalFileName.Replace(".zip", "").Split('\\').Last();
                // NOTE: BaseDirectory ends with a \ but we don't include the \ in the .Replace method.
                var DirectoryName = AppDomain.CurrentDomain.BaseDirectory.ToLower().Replace("GHuploader", @"GHweb\eps\training\customsites") + RelativePath;
                
                var indexFound = false;
                using (ZipArchive archive = ZipFile.OpenRead(LocalFileName))
                {
                    if (archive.Entries.Any(file => file.Name.ToLower() == "presentation_html5.html"))
                    {
                        ZipFile.ExtractToDirectory(LocalFileName, DirectoryName);
                        indexFound = true;
                    }
                }

                if (indexFound)
                {
                    File.Delete(LocalFileName);
                    var result = new UploadProcessingResult()
                    {
                        IsComplete = true,
                        FileName = OriginalFileName.Replace(".zip", ""),
                        LocalFilePath = RelativePath + @"\presentation_html5.html",
                        FileMetadata = _streamProvider.FormData
                    };
                    result.FileMetadata.Add("isCustomSite", "true");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw ex;
            }

            return null;
        }

#endregion

#region Properties

        private string LocalFileName
        {
            get
            {
                MultipartFileData fileData = _streamProvider.FileData.FirstOrDefault();
                return fileData.LocalFileName;
            }
        }

        private string OriginalFileName
        {
            get
            {
                MultipartFileData fileData = _streamProvider.FileData.FirstOrDefault();
                return fileData.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            }
        }

        private bool IsConEdZipFile
        {
            get
            {
                MultipartFileData fileData = _streamProvider.FileData.FirstOrDefault();
                return fileData.Headers.ContentType.MediaType == "application/x-zip-compressed" && _folder == "testmedia";
            }
        }


#endregion
    }
}