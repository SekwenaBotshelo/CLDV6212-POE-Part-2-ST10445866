using Azure.Data.Tables;
using ABCRetailers.Functions.Models;

namespace ABCRetailers.Functions.Helpers;

public static class Map
{
    public static CustomerDto CustomerToDto(TableEntity entity)
    {
        return new CustomerDto
        {
            Name = entity.GetString("Name"),
            Email = entity.GetString("Email"),
            Phone = entity.GetString("Phone")
        };
    }

    public static ProductDto ProductToDto(TableEntity entity)
    {
        return new ProductDto
        {
            Name = entity.GetString("Name"),
            Description = entity.GetString("Description"),
            Price = entity.GetDouble("Price") ?? 0,
            ImageBase64 = entity.GetString("ImageUrl")
        };
    }
}