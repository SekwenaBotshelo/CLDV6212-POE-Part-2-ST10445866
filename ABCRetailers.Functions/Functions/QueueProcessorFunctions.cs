using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers_ST10445866_POE.Services;

namespace ABCRetailers.Functions;

public class QueueProcessorFunctions
{
    private readonly ILogger<QueueProcessorFunctions> _logger;

    public QueueProcessorFunctions(ILogger<QueueProcessorFunctions> logger)
    {
        _logger = logger;
    }

    // Function to process order notifications
    [Function("ProcessOrderNotifications")]
    public async Task ProcessOrderQueue(
        [QueueTrigger("order-notifications", Connection = "AzureWebJobsStorage")] QueueMessage orderMessage)
    {
        if (orderMessage == null) return;

        _logger.LogInformation("Order queue processed: {0}", orderMessage.MessageText);

        try
        {
            // Deserialize the queue message to OrderDto
            var order = JsonSerializer.Deserialize<OrderDto>(orderMessage.MessageText);

            if (order != null)
            {
                // Use HttpClient to call your existing Functions API
                using var http = new HttpClient();
                http.BaseAddress = new Uri("http://localhost:7019/api/"); // Functions app URL
                var response = await http.PostAsJsonAsync("orders", order);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Order {0} saved to Orders table via Functions API.", order.Id);
                }
                else
                {
                    _logger.LogError("Failed to save order {0}. Status code: {1}", order.Id, response.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing order message: {0}", ex.Message);
        }
    }

    // Function to process stock updates
    [Function("ProcessStockUpdates")]
    public async Task ProcessStockQueue(
        [QueueTrigger("stock-updates", Connection = "AzureWebJobsStorage")] QueueMessage stockMessage)
    {
        if (stockMessage == null) return;

        _logger.LogInformation("Stock queue processed: {0}", stockMessage.MessageText);

        try
        {
            // You could implement stock update logic here, e.g., call an API or update a table
            _logger.LogInformation("Stock message processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing stock message: {0}", ex.Message);
        }
    }
}
