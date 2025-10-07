using System.Net;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Functions;

public class CustomersFunctions
{
    private readonly ILogger<CustomersFunctions> _logger;
    private readonly TableClient _customersTable;

    public CustomersFunctions(ILogger<CustomersFunctions> logger)
    {
        _logger = logger;
        var storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableName = Environment.GetEnvironmentVariable("CustomersTableName");

        _customersTable = new TableClient(storageConnection, tableName);
        _customersTable.CreateIfNotExists();
    }

    [Function("GetAllCustomers")]
    public HttpResponseData GetAllCustomers([HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
    {
        var customers = _customersTable.Query<TableEntity>().Select(c => new
        {
            Id = c.RowKey,
            Name = c.GetString("Name"),
            Email = c.GetString("Email"),
            Phone = c.GetString("Phone")
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(JsonSerializer.Serialize(customers));
        return response;
    }

    [Function("GetCustomerById")]
    public HttpResponseData GetCustomerById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var entity = _customersTable.GetEntity<TableEntity>("CustomersPartition", id);
            var customer = new
            {
                Id = entity.Value.RowKey,
                Name = entity.Value.GetString("Name"),
                Email = entity.Value.GetString("Email"),
                Phone = entity.Value.GetString("Phone")
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(customer));
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Customer with Id {id} not found.");
            return response;
        }
    }

    [Function("CreateCustomer")]
    public async Task<HttpResponseData> CreateCustomer([HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var dto = JsonSerializer.Deserialize<CustomerDto>(body);

        var entity = new TableEntity("CustomersPartition", Guid.NewGuid().ToString())
        {
            { "Name", dto.Name },
            { "Email", dto.Email },
            { "Phone", dto.Phone }
        };

        _customersTable.AddEntity(entity);

        var response = req.CreateResponse(HttpStatusCode.Created);
        response.WriteString($"Customer created with Id {entity.RowKey}");
        return response;
    }

    [Function("UpdateCustomer")]
    public async Task<HttpResponseData> UpdateCustomer([HttpTrigger(AuthorizationLevel.Function, "put", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        try
        {
            var existing = _customersTable.GetEntity<TableEntity>("CustomersPartition", id);
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<CustomerDto>(body);

            existing.Value["Name"] = dto.Name;
            existing.Value["Email"] = dto.Email;
            existing.Value["Phone"] = dto.Phone;

            _customersTable.UpdateEntity(existing.Value, existing.Value.ETag, TableUpdateMode.Replace);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Customer {id} updated successfully.");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Customer with Id {id} not found.");
            return response;
        }
    }

    [Function("DeleteCustomer")]
    public HttpResponseData DeleteCustomer([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        try
        {
            _customersTable.DeleteEntity("CustomersPartition", id);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Customer {id} deleted successfully.");
            return response;
        }
        catch (RequestFailedException)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            response.WriteString($"Customer with Id {id} not found.");
            return response;
        }
    }
}

public class CustomerDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}
