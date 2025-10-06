namespace ABCRetailersST10356144.Functions.Models;

public record CustomerDto(string Id, string FirstName, string LastName, string Username, string Email, string ShipAddress);

public record ProductDto(string Id, string ProductName, string Description, decimal Price, int AvailableStock, string ImageURL);

public record OrderDto(
    string Id, string CustomerID, string ProductID, string ProductName,
    int Quantity, decimal UnitPrice, decimal TotalPrice, DateTimeOffset OrderDate, string Status);

