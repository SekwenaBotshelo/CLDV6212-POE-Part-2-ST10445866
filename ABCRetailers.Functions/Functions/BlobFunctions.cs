using System;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ABCRetailers.Functions;

public class BlobFunctions
{
    private readonly ILogger<BlobFunctions> _logger;

    public BlobFunctions(ILogger<BlobFunctions> logger)
    {
        _logger = logger;
    }

    // Trigger for product images container
    [Function("ProductImageBlobTrigger")]
    public async Task ProductImageTrigger(
        [BlobTrigger("product-images/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        string name)
    {
        _logger.LogInformation($"Product image uploaded: {name}");
        await LogBlobInfo(blobClient, name);
        // Optionally update product TableEntity with new ImageUrl
    }

    // Trigger for payment proofs container
    [Function("PaymentProofBlobTrigger")]
    public async Task PaymentProofTrigger(
        [BlobTrigger("payment-proofs/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        string name)
    {
        _logger.LogInformation($"Payment proof uploaded: {name}");
        await LogBlobInfo(blobClient, name);
        // Optionally send queue message to OrdersFunctions for verification
    }

    // Trigger for $logs container (typically system logs; you may just log info)
    [Function("LogsBlobTrigger")]
    public async Task LogsTrigger(
        [BlobTrigger("$logs/{name}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        string name)
    {
        _logger.LogInformation($"Log blob created: {name}");
        await LogBlobInfo(blobClient, name);
    }

    // Helper method to log basic blob info
    private async Task LogBlobInfo(BlobClient blobClient, string name)
    {
        try
        {
            BlobProperties properties = await blobClient.GetPropertiesAsync();
            _logger.LogInformation($"Blob {name} - Size: {properties.ContentLength} bytes, Type: {properties.ContentType}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing blob {name}: {ex.Message}");
        }
    }
}
