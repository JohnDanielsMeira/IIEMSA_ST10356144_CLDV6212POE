using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using ABCRetailersST10356144.Functions.Entities;   // ← REQUIRED
using ABCRetailersST10356144.Functions.Helpers;    // ← REQUIRED
using ABCRetailersST10356144.Functions.Models;     // ← REQUIRED

namespace ABCRetailersST10356144.Functions.Functions;

public class ProductsFunctions
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string _images;

    public ProductsFunctions(IConfiguration cfg)
    {
        _conn = cfg["connection"] ?? throw new InvalidOperationException("connection missing");
        _table = cfg["TABLE_PRODUCT"] ?? "Product";
        _images = cfg["BLOB_PRODUCT_IMAGES"] ?? "product-images";
    }

    [Function("Products_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<ProductDto>();
        await foreach (var e in table.QueryAsync<ProductEntity>(x => x.PartitionKey == "Product"))
            items.Add(Map.ToDto(e));

        return await HttpJson.OkAsync(req, items);
    }

    [Function("Products_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            var e = await table.GetEntityAsync<ProductEntity>("Product", id);
            return await HttpJson.OkAsync(req, Map.ToDto(e.Value));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Product not found");
        }
    }

    [Function("Products_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req, FunctionContext ctx)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        string name = "", desc = "", imageUrl = "";
        double price = 0;
        int stock = 0;

        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            name = form.Text.GetValueOrDefault("ProductName") ?? "";
            desc = form.Text.GetValueOrDefault("Description") ?? "";
            double.TryParse(form.Text.GetValueOrDefault("Price") ?? "0", out price);
            int.TryParse(form.Text.GetValueOrDefault("AvailableStock") ?? "0", out stock);

            var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
            if (file is not null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _images);
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                await using var s = file.Data;
                await blob.UploadAsync(s);
                imageUrl = blob.Uri.ToString();
            }
            else
            {
                imageUrl = form.Text.GetValueOrDefault("ImageURL") ?? "";
            }
        }
        else
        {
            // JSON fallback
            var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
            name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? "" : "";
            desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "";
            price = body.TryGetValue("Price", out var pr) ? Convert.ToDouble(pr) : 0;
            stock = body.TryGetValue("AvailableStock", out var st) ? Convert.ToInt32(st) : 0;
            imageUrl = body.TryGetValue("ImageURL", out var iu) ? iu?.ToString() ?? "" : "";
        }

        if (string.IsNullOrWhiteSpace(name))
            return await HttpJson.BadAsync(req, "ProductName is required");

        var e = new ProductEntity
        {
            ProductName = name,
            Description = desc,
            Price = price,
            AvailableStock = stock,
            ImageURL = imageUrl
        };
        await table.AddEntityAsync(e);

        return await HttpJson.CreatedAsync(req, Map.ToDto(e));
    }

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        var table = new TableClient(_conn, _table);
        try
        {
            var resp = await table.GetEntityAsync<ProductEntity>("Product", id);
            var e = resp.Value;

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);

                if (form.Text.TryGetValue("ProductName", out var name)) e.ProductName = name;
                if (form.Text.TryGetValue("Description", out var desc)) e.Description = desc;
                if (form.Text.TryGetValue("Price", out var priceTxt) && double.TryParse(priceTxt, out var price)) e.Price = price;
                if (form.Text.TryGetValue("AvailableStock", out var stockTxt) && int.TryParse(stockTxt, out var stock)) e.AvailableStock = stock;
                if (form.Text.TryGetValue("ImageURL", out var iu)) e.ImageURL = iu;

                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                if (file is not null && file.Data.Length > 0)
                {
                    var container = new BlobContainerClient(_conn, _images);
                    await container.CreateIfNotExistsAsync();
                    var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                    await using var s = file.Data;
                    await blob.UploadAsync(s, overwrite: false);
                    e.ImageURL = blob.Uri.ToString();
                }
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
                if (body.TryGetValue("ProductName", out var pn)) e.ProductName = pn?.ToString() ?? e.ProductName;
                if (body.TryGetValue("Description", out var d)) e.Description = d?.ToString() ?? e.Description;
                if (body.TryGetValue("Price", out var pr) && double.TryParse(pr.ToString(), out var price)) e.Price = price;
                if (body.TryGetValue("AvailableStock", out var st) && int.TryParse(st.ToString(), out var stock)) e.AvailableStock = stock;
                if (body.TryGetValue("ImageUrl", out var iu)) e.ImageURL = iu?.ToString() ?? e.ImageURL;
            }

            await table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
            return await HttpJson.OkAsync(req, Map.ToDto(e));
        }
        catch
        {
            return await HttpJson.NotFoundAsync(req, "Product not found");
        }
    }

    [Function("Products_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        await table.DeleteEntityAsync("Product", id);
        return HttpJson.NoContent(req);
    }
}
