using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StorageApi.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace StorageApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        IConfiguration config;
        String connectionString;
        public ImagesController(IHostingEnvironment environment, IConfiguration config)
        {
            this.config = config;
            connectionString = config.GetValue<string>("AzureConnectionString");
        }     

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var httpRequest = HttpContext.Request;
            if(httpRequest.Form.Files.Count < 1)
            {
                return BadRequest();
            }

            foreach(var file in httpRequest.Form.Files)
            {
                var postedFile = httpRequest.Form.Files.GetFile(file.FileName);
                Guid guid = Guid.NewGuid();
                CloudStorageAccount storageAccount;
                if (CloudStorageAccount.TryParse(connectionString, out storageAccount))
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("imagecontainer");
                    if(!cloudBlobContainer.Exists())
                    {
                        cloudBlobContainer.CreateIfNotExists();
                    }
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(guid.ToString());

                    if (!cloudBlockBlob.Exists())
                    {
                        await cloudBlockBlob.UploadFromFileAsync(postedFile.FileName);
                    }
                    else
                    {
                        return BadRequest("Error while uploading.");
                    }
                    return Ok("File: " + postedFile.FileName + " uploaded.");
                }               
            }
            return BadRequest("Error ocurred");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            CloudStorageAccount cloudStorageAccount;
            if(CloudStorageAccount.TryParse(connectionString, out cloudStorageAccount))
            {
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("imagecontainer");
                if(!cloudBlobContainer.Exists())
                {
                    return NotFound();
                }
                BlobContinuationToken blobContinuationToken = null;
                BlobResultSegment blobResultSegment = cloudBlobContainer.ListBlobsSegmented(blobContinuationToken);
                return Ok(blobResultSegment.Results.ToList());
            }
            return BadRequest("Cannot connect to cloud.");
        }


    }
}