using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Plugins;

namespace ABCRetailers.Functions;

public class OrdersFunctions
{
    private readonly ILogger<OrdersFunctions> _logger;
    private readonly TableClient _ordersTable;
    private readonly QueueClient _ordersQueue;

    public OrdersFunctions(ILogger<OrdersFunctions> logger)
    {
        _logger = logger;

        string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        string ordersTableName = Environment.GetEnvironmentVariable("OrdersTableName");
        string ordersQueueName = Environment.GetEnvironmentVariable("OrdersQueueName");

        _ordersTable = new TableClient(storageConnection, ordersTableName);
        _ordersTable.CreateIfNotExists();

        _ordersQueue = new QueueClient(storageConnection, ordersQueueName);
        _ordersQueue.CreateIfNotExists();
    }

    // List all orders
    [Function("GetAllOrders")]
    public HttpResponseData GetAllOrders([HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
    {
        var orders = _ordersTable.Query<TableEntity>().Select(o => new
        {
            Id = o.RowKey,
            CustomerName = o.GetString("CustomerName"),
            ProductName = o.GetString("ProductName"),
            Quantity = o.GetInt32("Quantity"),
            Status = o.GetString("Status")
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(orders));
        return response;
    }

    // Get order by Id
    [Function("GetOrderById")]
    public HttpResponseData GetOrderById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var entity = _ordersTable.GetEntity<TableEntity>("OrdersPartition", id);
            var order = new
            {
                Id = entity.Value.RowKey,
                CustomerName = entity.Value.GetString("CustomerName"),
                ProductName = entity.Value.GetString("ProductName"),
                Quantity = entity.Value.GetInt32("Quantity"),
                Status = entity.Value.GetString("Status")
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(order));
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Order with Id {id} not found.");
            return response;
        }
    }

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        // Get connection string from settings
        string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var queueClient = new QueueClient(storageConnection, "orders-queue");
        queueClient.CreateIfNotExists();

        // Send message as base64-encoded JSON
        await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(body)));

        var response = req.CreateResponse(System.Net.HttpStatusCode.Accepted);
        response.WriteString("Order enqueued for processing.");
        return response;
    }

    [Function("ProcessOrderQueue")]
    public void ProcessOrderQueue(
    [QueueTrigger("orders-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        var orderDto = JsonSerializer.Deserialize<OrderDto>(queueMessage);

        // Create TableEntity
        var entity = new TableEntity("OrdersPartition", Guid.NewGuid().ToString())
    {
        { "CustomerName", orderDto.CustomerName },
        { "ProductName", orderDto.ProductName },
        { "Quantity", orderDto.Quantity },
        { "Status", "Pending" },
        { "CreatedAt", DateTime.UtcNow }
    };

        // Get TableClient (same as before, or inject if you prefer)
        string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        string ordersTableName = Environment.GetEnvironmentVariable("OrdersTableName");
        var tableClient = new TableClient(storageConnection, ordersTableName);
        tableClient.AddEntity(entity);
    }

    // Update order status
    [Function("UpdateOrderStatus")]
    public async Task<HttpResponseData> UpdateOrderStatus([HttpTrigger(AuthorizationLevel.Function, "put", Route = "orders/{id}/status")] HttpRequestData req, string id)
    {
        try
        {
            var existing = _ordersTable.GetEntity<TableEntity>("OrdersPartition", id);
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<OrderStatusDto>(body);

            existing.Value["Status"] = dto.Status;

            _ordersTable.UpdateEntity(existing.Value, existing.Value.ETag, TableUpdateMode.Replace);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Order {id} status updated to {dto.Status}");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Order with Id {id} not found.");
            return response;
        }
    }

    // Delete order
    [Function("DeleteOrder")]
    public HttpResponseData DeleteOrder([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        try
        {
            _ordersTable.DeleteEntity("OrdersPartition", id);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Order {id} deleted successfully.");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Order with Id {id} not found.");
            return response;
        }
    }
}

// DTO classes
public class OrderDto
{
    public string Id { get; set; }
    public string CustomerName { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderStatusDto
{
    public string Status { get; set; } // e.g., "Pending", "Shipped", "Completed"
}
