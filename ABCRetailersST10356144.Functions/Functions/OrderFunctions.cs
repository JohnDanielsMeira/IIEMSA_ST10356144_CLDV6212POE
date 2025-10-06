using ABCRetailersST10356144.Functions.Entities;   // ← REQUIRED
using ABCRetailersST10356144.Functions.Helpers;    // ← REQUIRED
using ABCRetailersST10356144.Functions.Models;     // ← REQUIRED
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace ABCRetailersST10356144.Functions.Functions;

public class OrdersFunctions
{
    private readonly string _conn;
    private readonly string _ordersTable;
    private readonly string _productsTable;
    private readonly string _customersTable;
    private readonly string _queueOrder;
    private readonly string _queueStock;

    public OrdersFunctions(IConfiguration cfg)
    {
        _conn = cfg["connection"] ?? throw new InvalidOperationException("connection missing");
        _ordersTable = cfg["TABLE_ORDER"] ?? "Order";
        _productsTable = cfg["TABLE_PRODUCT"] ?? "Product";
        _customersTable = cfg["TABLE_CUSTOMER"] ?? "Customer";
        _queueOrder = cfg["QUEUE_ORDER_NOTIFICATIONS"] ?? "order-notifications";
        _queueStock = cfg["QUEUE_STOCK_UPDATES"] ?? "stock-updates";
    }

    [Function("Orders_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _ordersTable);
        await table.CreateIfNotExistsAsync();

        var items = new List<OrderDto>();
        await foreach (var e in table.QueryAsync<OrderEntity>(x => x.PartitionKey == "Order"))
            items.Add(Map.ToDto(e));

        // newest first
        var ordered = items.OrderByDescending(o => o.OrderDate).ToList();
        return await HttpJson.OkAsync(req, ordered);
    }

    [Function("Orders_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _ordersTable);
        try
        {
            var e = await table.GetEntityAsync<OrderEntity>("Order", id);
            return await HttpJson.OkAsync(req, Map.ToDto(e.Value));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Order not found");
        }
    }

    public record OrderCreate(string CustomerID, string ProductID, int Quantity);

    [Function("Orders_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
    {
        var input = await HttpJson.ReadAsync<OrderCreate>(req);
        if (input is null || string.IsNullOrWhiteSpace(input.CustomerID) || string.IsNullOrWhiteSpace(input.ProductID) || input.Quantity < 1)
            return await HttpJson.BadAsync(req, "CustomerID, ProductID, Quantity >= 1 required");

        var orders = new TableClient(_conn, _ordersTable);
        var products = new TableClient(_conn, _productsTable);
        var customers = new TableClient(_conn, _customersTable);
        await orders.CreateIfNotExistsAsync();
        await products.CreateIfNotExistsAsync();
        await customers.CreateIfNotExistsAsync();

        // Validate refs
        ProductEntity product;
        CustomerEntity customer;

        try
        {
            product = (await products.GetEntityAsync<ProductEntity>("Product", input.ProductID)).Value;
        }
        catch { return await HttpJson.BadAsync(req, "Invalid ProductID"); }

        try
        {
            customer = (await customers.GetEntityAsync<CustomerEntity>("Customer", input.CustomerID)).Value;
        }
        catch { return await HttpJson.BadAsync(req, "Invalid CustomerId"); }

        if (product.AvailableStock < input.Quantity)
            return await HttpJson.BadAsync(req, $"Insufficient stock. Available: {product.AvailableStock}");

        // Snapshot price & reduce stock (naïve; for concurrency use ETag preconditions)
        var order = new OrderEntity
        {
            CustomerID = input.CustomerID,
            ProductID = input.ProductID,
            ProductName = product.ProductName,
            Quantity = input.Quantity,
            UnitPrice = product.Price,
            OrderDate = DateTimeOffset.UtcNow,
            Status = "Submitted"
        };
        await orders.AddEntityAsync(order);

        product.AvailableStock -= input.Quantity;
        await products.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

        // Send queue messages
        var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        var queueStock = new QueueClient(_conn, _queueStock, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        await queueOrder.CreateIfNotExistsAsync();
        await queueStock.CreateIfNotExistsAsync();

        var orderMsg = new
        {
            Type = "OrderCreated",
            OrderID = order.RowKey,
            order.CustomerID,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            order.ProductID,
            ProductName = product.ProductName,
            order.Quantity,
            order.UnitPrice,
            TotalPrice = order.UnitPrice * order.Quantity,
            OrderDate = order.OrderDate,
            order.Status
        };
        await queueOrder.SendMessageAsync(JsonSerializer.Serialize(orderMsg));

        var stockMsg = new
        {
            Type = "StockUpdated",
            ProductID = product.RowKey,
            ProductName = product.ProductName,
            PreviousStock = product.AvailableStock + input.Quantity,
            NewStock = product.AvailableStock,
            UpdatedDateUtc = DateTimeOffset.UtcNow,
            UpdatedBy = "Order System"
        };
        await queueStock.SendMessageAsync(JsonSerializer.Serialize(stockMsg));

        return await HttpJson.CreatedAsync(req, Map.ToDto(order));
    }

    public record OrderStatusUpdate(string Status);

    [Function("Orders_UpdateStatus")]
    public async Task<HttpResponseData> UpdateStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "post", "put", Route = "orders/{id}/status")] HttpRequestData req, string id)
    {
        var input = await HttpJson.ReadAsync<OrderStatusUpdate>(req);
        if (input is null || string.IsNullOrWhiteSpace(input.Status))
            return await HttpJson.BadAsync(req, "Status is required");

        var orders = new TableClient(_conn, _ordersTable);
        try
        {
            var resp = await orders.GetEntityAsync<OrderEntity>("Order", id);
            var e = resp.Value;
            var previous = e.Status;

            e.Status = input.Status;
            await orders.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);

            // notify
            var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await queueOrder.CreateIfNotExistsAsync();
            var statusMsg = new
            {
                Type = "OrderStatusUpdated",
                OrderID = e.RowKey,
                PreviousStatus = previous,
                NewStatus = e.Status,
                UpdatedDateUtc = DateTimeOffset.UtcNow,
                UpdatedBy = "System"
            };
            await queueOrder.SendMessageAsync(JsonSerializer.Serialize(statusMsg));

            return await HttpJson.OkAsync(req, Map.ToDto(e));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Order not found");
        }
    }

    [Function("Orders_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _ordersTable);
        await table.DeleteEntityAsync("Order", id);
        return HttpJson.NoContent(req);
    }
}

