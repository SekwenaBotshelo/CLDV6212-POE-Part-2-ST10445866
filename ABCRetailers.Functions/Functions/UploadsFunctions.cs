using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Functions
{
    public class UploadsFunctions
    {
        private readonly ILogger<UploadsFunctions> _logger;
        private readonly ShareClient _shareClient;

        public UploadsFunctions(ILogger<UploadsFunctions> logger)
        {
            _logger = logger;
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var shareName = Environment.GetEnvironmentVariable("UploadsShareName") ?? "uploads";

            _shareClient = new ShareClient(connectionString, shareName);
            _shareClient.CreateIfNotExists();
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "uploads")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // Read request body manually
                using var ms = new MemoryStream();
                await req.Body.CopyToAsync(ms);
                ms.Position = 0;

                // Optional: validate file size (max 10 MB)
                if (ms.Length == 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("No file provided.");
                    return response;
                }
                if (ms.Length > 10_000_000)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("File too large. Max 10 MB allowed.");
                    return response;
                }

                // Generate unique file name
                var fileName = $"{Guid.NewGuid()}.dat"; // You can append extension if needed

                var rootDir = _shareClient.GetRootDirectoryClient();
                var fileClient = rootDir.GetFileClient(fileName);

                // Create file and upload content
                await fileClient.CreateAsync(ms.Length);
                ms.Position = 0;
                await fileClient.UploadRangeAsync(new HttpRange(0, ms.Length), ms);

                // Return file URL
                var fileUrl = fileClient.Uri.ToString();
                response.StatusCode = HttpStatusCode.Created;
                await response.WriteStringAsync($"File uploaded successfully: {fileUrl}");

                _logger.LogInformation($"File uploaded: {fileUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Error uploading file.");
            }

            return response;
        }
    }
}