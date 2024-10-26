﻿using System.Collections.Concurrent;
using System.Net;

class Program
{
    private static int _port;
    private static string? _originUrl;
    private static readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<string, byte[]> _cache = new();

    static async Task Main(string[] args)
    {
        if (!ParseArguments(args, out _port, out _originUrl, out bool clearCache))
        {
            Console.WriteLine("Usage: caching-proxy --port <number> --origin <url>");
            Console.WriteLine("       caching-proxy --clear-cache");
            return;
        }

        if (clearCache)
        {
            // Send request to clear cache
            await SendClearCacheRequest();
            return;
        }

        // Create the WebApplicationBuilder
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://*:{_port}");

        var app = builder.Build();

        Console.WriteLine($"Caching proxy server started on port {_port}");
        Console.WriteLine($"Forwarding requests to origin: {_originUrl}");

        // Configure the HTTP request pipeline
        app.Map("/admin/clear-cache", ClearCacheEndpoint);

        app.Run(HandleRequestAsync);

        await app.RunAsync();
    }

    static async Task HandleRequestAsync(HttpContext context)
    {
        var requestPath = context.Request.Path + context.Request.QueryString;
        Console.WriteLine($"Received request: {requestPath}");

        if (_cache.TryGetValue(requestPath, out byte[]? cachedResponse))
        {
            Console.WriteLine("Serving from cache");
            context.Response.Headers["X-Cache"] = "HIT";
            await context.Response.Body.WriteAsync(cachedResponse);
            return;
        }

        // Forward the request to the origin server
        var originRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(_originUrl + requestPath),
            Method = new HttpMethod(context.Request.Method)
        };

        // Copy request headers
        foreach (var header in context.Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                originRequest.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
            }
        }

        try
        {
            var originResponse = await _httpClient.SendAsync(originRequest);
            var responseBody = await originResponse.Content.ReadAsByteArrayAsync();

            // Store the response in cache if it's a successful response
            if (originResponse.IsSuccessStatusCode)
            {
                _cache[requestPath] = responseBody;
            }

            // Copy the response headers
            foreach (var header in originResponse.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in originResponse.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.StatusCode = (int)originResponse.StatusCode;
            context.Response.Headers["X-Cache"] = "MISS";
            await context.Response.Body.WriteAsync(responseBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            await context.Response.WriteAsync("Error forwarding request.");
        }
    }

    static void ClearCacheEndpoint(WebApplication app)
    {
        app.Run(context =>
        {
            Console.WriteLine("Cache cleared via administrative endpoint.");
            _cache.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            return context.Response.WriteAsync("Cache cleared.");
        });
    }

    static async Task SendClearCacheRequest()
    {
        try
        {
            Console.WriteLine("Sending request to clear cache...");
            var response = await _httpClient.GetAsync($"http://localhost:{_port}/admin/clear-cache");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Cache successfully cleared.");
            }
            else
            {
                Console.WriteLine("Failed to clear cache. Server responded with status code: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending clear cache request: {ex.Message}");
        }
    }

    static bool ParseArguments(string[] args, out int port, out string originUrl, out bool clearCache)
    {
        port = 0;
        originUrl = null;
        clearCache = false;

        if (args.Length == 1 && args[0] == "--clear-cache")
        {
            // Default port to use when clearing cache
            port = 3000; // You can adjust this default port as needed
            clearCache = true;
            return true;
        }

        if (args.Length != 4)
        {
            return false;
        }

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--port" && int.TryParse(args[i + 1], out port))
            {
                i++;
            }
            else if (args[i] == "--origin")
            {
                originUrl = args[i + 1].TrimEnd('/');
                i++;
            }
            else
            {
                return false;
            }
        }

        return port > 0 && !string.IsNullOrEmpty(originUrl);
    }
}