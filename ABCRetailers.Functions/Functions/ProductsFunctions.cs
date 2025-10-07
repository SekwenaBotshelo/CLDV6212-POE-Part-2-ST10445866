using System.Net;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Functions;

public class ProductsFunctions
{
    private readonly ILogger<ProductsFunctions> _logger;
    private readonly TableClient _productsTable;
    private readonly BlobContainerClient _blobContainer;

    public ProductsFunctions(ILogger<ProductsFunctions> logger)
    {
        _logger = logger;
        var storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableName = Environment.GetEnvironmentVariable("ProductsTableName");
        var containerName = Environment.GetEnvironmentVariable("UploadsContainerName");

        _productsTable = new TableClient(storageConnection, tableName);
        _productsTable.CreateIfNotExists();

        _blobContainer = new BlobContainerClient(storageConnection, containerName);
        _blobContainer.CreateIfNotExists();
    }

    [Function("GetAllProducts")]
    public HttpResponseData GetAllProducts([HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
    {
        var products = _productsTable.Query<TableEntity>().Select(p => new
        {
            Id = p.RowKey,
            Name = p.GetString("Name"),
            Description = p.GetString("Description"),
            Price = p.GetDouble("Price"),
            ImageUrl = p.GetString("ImageUrl")
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(products));
        return response;
    }

    [Function("GetProductById")]
    public HttpResponseData GetProductById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var entity = _productsTable.GetEntity<TableEntity>("ProductsPartition", id);
            var product = new
            {
                Id = entity.Value.RowKey,
                Name = entity.Value.GetString("Name"),
                Description = entity.Value.GetString("Description"),
                Price = entity.Value.GetDouble("Price"),
                ImageUrl = entity.Value.GetString("ImageUrl")
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(product));
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Product with Id {id} not found.");
            return response;
        }
    }

    [Function("CreateProduct")]
    public async Task<HttpResponseData> CreateProduct([HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var dto = JsonSerializer.Deserialize<ProductDto>(body);

        string imageUrl = null;

        if (!string.IsNullOrEmpty(dto.ImageBase64))
        {
            var imageBytes = Convert.FromBase64String(dto.ImageBase64);
            var blobName = $"{Guid.NewGuid()}.jpg";
            var blobClient = _blobContainer.GetBlobClient(blobName);
            using var ms = new MemoryStream(imageBytes);
            await blobClient.UploadAsync(ms);
            imageUrl = blobClient.Uri.ToString();
        }

        var entity = new TableEntity("ProductsPartition", Guid.NewGuid().ToString())
        {
            { "Name", dto.Name },
            { "Description", dto.Description },
            { "Price", dto.Price },
            { "ImageUrl", imageUrl }
        };

        _productsTable.AddEntity(entity);

        var response = req.CreateResponse(HttpStatusCode.Created);
        response.WriteString($"Product created with Id {entity.RowKey}");
        return response;
    }

    [Function("UpdateProduct")]
    public async Task<HttpResponseData> UpdateProduct([HttpTrigger(AuthorizationLevel.Function, "put", Route = "products/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var existing = _productsTable.GetEntity<TableEntity>("ProductsPartition", id);
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<ProductDto>(body);

            existing.Value["Name"] = dto.Name;
            existing.Value["Description"] = dto.Description;
            existing.Value["Price"] = dto.Price;

            if (!string.IsNullOrEmpty(dto.ImageBase64))
            {
                var imageBytes = Convert.FromBase64String(dto.ImageBase64);
                var blobName = $"{Guid.NewGuid()}.jpg";
                var blobClient = _blobContainer.GetBlobClient(blobName);
                using var ms = new MemoryStream(imageBytes);
                await blobClient.UploadAsync(ms);
                existing.Value["ImageUrl"] = blobClient.Uri.ToString();
            }

            _productsTable.UpdateEntity(existing.Value, existing.Value.ETag, TableUpdateMode.Replace);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Product {id} updated successfully.");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Product with Id {id} not found.");
            return response;
        }
    }

    [Function("DeleteProduct")]
    public HttpResponseData DeleteProduct([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "products/{id}")] HttpRequestData req, string id)
    {
        try
        {
            _productsTable.DeleteEntity("ProductsPartition", id);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Product {id} deleted successfully.");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Product with Id {id} not found.");
            return response;
        }
    }

    [Function("SearchProducts")]
    public HttpResponseData SearchProducts(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/search")] HttpRequestData req)
    {
        var query = req.Url.Query.Split("query=")[1]; // basic parsing, consider robust method
        var products = _productsTable.Query<TableEntity>()
            .Where(p => p.GetString("Name").Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(p => new
            {
                Id = p.RowKey,
                Name = p.GetString("Name"),
                Description = p.GetString("Description"),
                Price = p.GetDouble("Price"),
                ImageUrl = p.GetString("ImageUrl")
            }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(products));
        return response;
    }

}

public class ProductDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public string ImageBase64 { get; set; } // optional
}
