using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ABCRetailersST10356144.Functions.Entities;   // ← REQUIRED
using ABCRetailersST10356144.Functions.Helpers;    // ← REQUIRED
using ABCRetailersST10356144.Functions.Models;     // ← REQUIRED

namespace ABCRetailersST10356144.Functions.Functions;

public class CustomersFunctions
{
    private readonly string _conn;
    private readonly string _table;

    public CustomersFunctions(IConfiguration cfg)
    {
        _conn = cfg["connection"] ?? throw new InvalidOperationException("connection missing");
        _table = cfg["TABLE_CUSTOMER"] ?? "Customer";
    }

    [Function("Customers_List")]
    public async Task<HttpResponseData> List(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<CustomerDto>();
        await foreach (var e in table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
        items.Add(Map.ToDto(e));

        return await HttpJson.OkAsync(req, items);
    }

    [Function("Customers_Get")]
    public async Task<HttpResponseData> Get(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            var e = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            return await HttpJson.OkAsync(req, Map.ToDto(e.Value));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Customer not found");
        }
    }

    public record CustomerCreateUpdate(string? FirstName, string? LastName, string? Username, string? Email, string? ShipAddress);

    [Function("Customers_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
        if (input is null || string.IsNullOrWhiteSpace(input.FirstName) || string.IsNullOrWhiteSpace(input.Email))
            return await HttpJson.BadAsync(req, "Name and Email are required");

        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var e = new CustomerEntity
        {
            FirstName = input.FirstName!,
            LastName = input.LastName ?? "",
            Username = input.Username ?? "",
            Email = input.Email!,
            ShipAddress = input.ShipAddress ?? ""
        };
        await table.AddEntityAsync(e);

        return await HttpJson.CreatedAsync(req, Map.ToDto(e));
    }

    [Function("Customers_Update")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
        if (input is null) return await HttpJson.BadAsync(req, "Invalid body");

        var table = new TableClient(_conn, _table);
        try
        {
            var resp = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            var e = resp.Value;

            e.FirstName = input.FirstName ?? e.FirstName;
            e.LastName = input.LastName ?? e.LastName;
            e.Username = input.Username ?? e.Username;
            e.Email = input.Email ?? e.Email;
            e.ShipAddress = input.ShipAddress ?? e.ShipAddress;

            await table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
            return await HttpJson.OkAsync(req, Map.ToDto(e));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Customer not found");
        }
    }

    [Function("Customers_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        await table.DeleteEntityAsync("Customer", id);
        return HttpJson.NoContent(req);
    }
}
