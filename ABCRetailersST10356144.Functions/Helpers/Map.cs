using ABCRetailersST10356144.Functions.Entities;
using ABCRetailersST10356144.Functions.Models;

namespace ABCRetailersST10356144.Functions.Helpers;

public static class Map
{
    // Table Entity  ->  DTOs returned to MVC

    public static CustomerDto ToDto(CustomerEntity e)
        => new(
            Id: e.RowKey,
            FirstName: e.FirstName,
            LastName: e.LastName,
            Username: e.Username,
            Email: e.Email,
            ShipAddress: e.ShipAddress
        );

    public static ProductDto ToDto(ProductEntity e)
        => new(
            Id: e.RowKey,
            ProductName: e.ProductName,
            Description: e.Description,
            Price: (decimal)e.Price,                 // stored as double in Table, cast to decimal for MVC
            AvailableStock: e.AvailableStock,
            ImageURL: e.ImageURL
        );

    public static OrderDto ToDto(OrderEntity e)
    {
        var unitPrice = (decimal)e.UnitPrice;       // double -> decimal for money in MVC
        var total = unitPrice * e.Quantity;

        return new OrderDto(
            Id: e.RowKey,
            CustomerID: e.CustomerID,
            ProductID: e.ProductID,
            ProductName: e.ProductName,
            Quantity: e.Quantity,
            UnitPrice: unitPrice,
            TotalPrice: total,
            OrderDate: e.OrderDate,
            Status: e.Status
        );
    }
}
