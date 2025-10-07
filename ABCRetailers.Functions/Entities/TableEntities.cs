using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Entities;

public class CustomerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "CustomersPartition";
    public string RowKey { get; set; } // Customer ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Customer properties
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

public class ProductEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "ProductsPartition";
    public string RowKey { get; set; } // Product ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Product properties
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public string ImageUrl { get; set; } // blob link
}

public class OrderEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "OrdersPartition";
    public string RowKey { get; set; } // Order ID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Order properties
    public string CustomerId { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } // Pending, Completed, etc.
    public DateTime OrderDate { get; set; }
}
