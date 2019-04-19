using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FineUploaderAspCoreExample.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Extensions.Configuration;

namespace FineUploaderAspCoreExample.Controllers
{
    public class HomeController : Controller
    {
        readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Produces("application/json")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string qquuid, string qqfilename, int qqtotalfilesize, IFormFile qqfile)
        {
            int intFileDBID = 0;
            string strRealFilePath = "";
            //temp folder to put files in before upload to Blob Storage. Am making this a relative folder in the project since App Service and local environments may use different paths.
            //I included a placeholder file in the temp directory and set it to Copy Always so that the temp directory will deploy.
            string tempFolder = "temp";

            // original file name - qqfile.FileName
            // user file name - qqfilename

            if (qqfile.Length > 0)
            {

                var filePath = Path.Combine(tempFolder, qqfilename);
                //var filePath = Path.Combine(/*strCurrentPath, */qqfilename);

                try
                {

                    //Set these in the appsettings.json
                    string accountName = _configuration["StorageAccount"];
                    string accountKey = _configuration["StorageAccountKey"];

                    StorageCredentials credentials = new StorageCredentials(accountName, accountKey);
                    CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, useHttps: true);

                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer container = blobClient.GetContainerReference("temp");

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await qqfile.CopyToAsync(stream);

                        intFileDBID = 1; // change on file DB ID, if needed
                        strRealFilePath = filePath;  // change on file real path, if needed
                        CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(qqfilename);
                        await cloudBlockBlob.UploadFromStreamAsync(stream);
                    }
                    var guid = Guid.NewGuid().ToString(); 
                }
                catch (Exception ex)
                {
                    // return to JS fail and error
                    return Ok(new { success = false, error = ex.Message });
                }
            }

            // return to JS success and extra information about uploded file
            return Ok(new { success = true, fileid = intFileDBID, fpath = strRealFilePath, error = "" });
        }


        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
