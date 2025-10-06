using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Functions.Models;

namespace ABCRetailersST10356144.Services;

public class FunctionsApiClient : IFunctionsApi
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // Centralize your Function routes here
    private const string CustomersRoute = "customers";
    private const string ProductsRoute = "products";
    private const string OrdersRoute = "orders";
    private const string UploadsRoute = "uploads/proof-of-payment"; // multipart

    public FunctionsApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Functions"); // BaseAddress set in Program.cs
    }

    // ---------- Helpers ----------
    private static HttpContent JsonBody(object obj)
        => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
    {
        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {resp.StatusCode}: {errorContent}");
        }

        var stream = await resp.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<T>(stream, _json);

        if (data == null)
            throw new InvalidOperationException("Failed to deserialize response");

        return data;
    }

    // ---------- Customers ----------
    public async Task<List<Customer>> GetCustomersAsync()
    {
        var customerDtos = await ReadJsonAsync<List<CustomerDto>>(await _http.GetAsync(CustomersRoute));

        return customerDtos.Select(dto => new Customer
        {
            PartitionKey = "Customer",
            RowKey = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            ShipAddress = dto.ShipAddress
        }).ToList();
    }

    public async Task<Customer?> GetCustomerAsync(string id)
    {
        var resp = await _http.GetAsync($"{CustomersRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        var dto = await ReadJsonAsync<CustomerDto>(resp);
        return new Customer
        {
            PartitionKey = "Customer",
            RowKey = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            ShipAddress = dto.ShipAddress
        };
    }

    public async Task<Customer> CreateCustomerAsync(Customer c)
    {
        var response = await _http.PostAsync(CustomersRoute, JsonBody(new
        {
            FirstName = c.FirstName,
            LastName = c.LastName,
            Username = c.Username,
            Email = c.Email,
            ShipAddress = c.ShipAddress
        }));

        var dto = await ReadJsonAsync<CustomerDto>(response);
        return new Customer
        {
            PartitionKey = "Customer",
            RowKey = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            ShipAddress = dto.ShipAddress
        };
    }

    public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
    {
        var response = await _http.PutAsync($"{CustomersRoute}/{id}", JsonBody(new
        {
            FirstName = c.FirstName,
            LastName = c.LastName,
            Username = c.Username,
            Email = c.Email,
            ShipAddress = c.ShipAddress
        }));

        var dto = await ReadJsonAsync<CustomerDto>(response);
        return new Customer
        {
            PartitionKey = "Customer",
            RowKey = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            ShipAddress = dto.ShipAddress
        };
    }

    public async Task DeleteCustomerAsync(string id)
        => (await _http.DeleteAsync($"{CustomersRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Products ----------
    public async Task<List<Product>> GetProductsAsync()
    {
        var productDtos = await ReadJsonAsync<List<ProductDto>>(await _http.GetAsync(ProductsRoute));

        return productDtos.Select(dto => new Product
        {
            PartitionKey = "Product",
            RowKey = dto.Id,
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = (double)dto.Price,
            AvailableStock = dto.AvailableStock,
            ImageURL = dto.ImageURL
        }).ToList();
    }

    public async Task<Product?> GetProductAsync(string id)
    {
        var resp = await _http.GetAsync($"{ProductsRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

        var dto = await ReadJsonAsync<ProductDto>(resp);
        return new Product
        {
            PartitionKey = "Product",
            RowKey = dto.Id,
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = (double)dto.Price,
            AvailableStock = dto.AvailableStock,
            ImageURL = dto.ImageURL
        };
    }

    public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(p.ProductName), "ProductName");
        form.Add(new StringContent(p.Description ?? string.Empty), "Description");
        form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
        form.Add(new StringContent(p.AvailableStock.ToString(System.Globalization.CultureInfo.InvariantCulture)), "AvailableStock");
        if (!string.IsNullOrWhiteSpace(p.ImageURL)) form.Add(new StringContent(p.ImageURL), "ImageURL");
        if (imageFile is not null && imageFile.Length > 0)
        {
            var file = new StreamContent(imageFile.OpenReadStream());
            file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            form.Add(file, "ImageFile", imageFile.FileName);
        }
        return await ReadJsonAsync<Product>(await _http.PostAsync(ProductsRoute, form));
    }

    public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(p.ProductName), "ProductName");
        form.Add(new StringContent(p.Description ?? string.Empty), "Description");
        form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
        form.Add(new StringContent(p.AvailableStock.ToString(System.Globalization.CultureInfo.InvariantCulture)), "AvailableStock");
        if (!string.IsNullOrWhiteSpace(p.ImageURL)) form.Add(new StringContent(p.ImageURL), "ImageURL");
        if (imageFile is not null && imageFile.Length > 0)
        {
            var file = new StreamContent(imageFile.OpenReadStream());
            file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            form.Add(file, "ImageFile", imageFile.FileName);
        }
        return await ReadJsonAsync<Product>(await _http.PutAsync($"{ProductsRoute}/{id}", form));
    }

    public async Task DeleteProductAsync(string id)
        => (await _http.DeleteAsync($"{ProductsRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Orders (use DTOs → map to enum) ----------
    public async Task<List<Order>> GetOrdersAsync()
    {
        var dtos = await ReadJsonAsync<List<OrderDto>>(await _http.GetAsync(OrdersRoute));
        return dtos.Select(ToOrder).ToList();
    }

    public async Task<Order?> GetOrderAsync(string id)
    {
        var resp = await _http.GetAsync($"{OrdersRoute}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        var dto = await ReadJsonAsync<OrderDto>(resp);
        return ToOrder(dto);
    }

    public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
    {
        // With JsonSerializerDefaults.Web, keys serialize as: customerId, productId, quantity
        var payload = new { customerId, productId, quantity };
        var dto = await ReadJsonAsync<OrderDto>(await _http.PostAsync(OrdersRoute, JsonBody(payload)));
        return ToOrder(dto);
    }

    public async Task UpdateOrderStatusAsync(string id, string newStatus)
    {
        var payload = new { status = newStatus };
        (await _http.PatchAsync($"{OrdersRoute}/{id}/status", JsonBody(payload))).EnsureSuccessStatusCode();
    }

    public async Task DeleteOrderAsync(string id)
        => (await _http.DeleteAsync($"{OrdersRoute}/{id}")).EnsureSuccessStatusCode();

    // ---------- Uploads ----------
    public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
    {
        using var form = new MultipartFormDataContent();
        var sc = new StreamContent(file.OpenReadStream());
        sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(sc, "ProofOfPayment", file.FileName);
        if (!string.IsNullOrWhiteSpace(orderId)) form.Add(new StringContent(orderId), "OrderId");
        if (!string.IsNullOrWhiteSpace(customerName)) form.Add(new StringContent(customerName), "CustomerName");

        var resp = await _http.PostAsync(UploadsRoute, form);
        resp.EnsureSuccessStatusCode();

        var doc = await ReadJsonAsync<Dictionary<string, string>>(resp);
        return doc.TryGetValue("fileName", out var name) ? name : file.FileName;
    }

    // ---------- Mapping ----------
    private static Order ToOrder(OrderDto d)
    {
        var status = Enum.TryParse<OderStatus>(d.Status, ignoreCase: true, out var s)
            ? s : OderStatus.Submitted;

        return new Order
        {
            Id = d.Id,
            CustomerID = d.CustomerId,
            ProductID = d.ProductId,
            ProductName = d.ProductName,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            OrderDate = d.OrderDateUtc,
            Status = status
        };
    }

    // DTOs that match Functions JSON (camelCase)
    private sealed record OrderDto(
        string Id,
        string CustomerId,
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        DateTimeOffset OrderDateUtc,
        string Status);
}

// Minimal PATCH extension for HttpClient
internal static class HttpClientPatchExtensions
{
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
        => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content });
}
