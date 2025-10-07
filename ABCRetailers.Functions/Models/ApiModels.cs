using System;

namespace ABCRetailers.Functions.Models
{
    // DTO for Customer API
    public class CustomerDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    // DTO for Product API
    public class ProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string ImageBase64 { get; set; } // optional base64 for image upload
    }

    // DTO for Order API
    public class OrderDto
    {
        public string CustomerId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
