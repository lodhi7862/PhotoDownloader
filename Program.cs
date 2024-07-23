using HtmlAgilityPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        await context.Response.SendFileAsync("index.cshtml");
    });

    endpoints.MapGet("/fetch-images", async context =>
    {
        var url = context.Request.Query["url"].ToString();
        if (string.IsNullOrEmpty(url))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("URL parameter is missing.");
            return;
        }

        try
        {
            var imageUrls = await FetchImageUrls(url);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(imageUrls);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"An error occurred: {ex.Message}");
        }
    });
});

await app.RunAsync();

static async Task<List<string>> FetchImageUrls(string url)
{
    using HttpClient client = new HttpClient();
    string htmlContent = await client.GetStringAsync(url);

    HtmlDocument document = new HtmlDocument();
    document.LoadHtml(htmlContent);

    var imageNodes = document.DocumentNode.SelectNodes("//img");
    List<string> imageUrls = new List<string>();
    HashSet<string> validExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    if (imageNodes != null)
    {
        foreach (var node in imageNodes)
        {
            string src = node.GetAttributeValue("src", null);
            if (!string.IsNullOrEmpty(src))
            {
                Uri baseUri = new Uri(url);
                Uri imageUri = new Uri(baseUri, src);

                string extension = Path.GetExtension(imageUri.LocalPath).ToLower();
                if (validExtensions.Contains(extension))
                {
                    imageUrls.Add(imageUri.ToString());
                }
            }
        }
    }

    return imageUrls;
}
