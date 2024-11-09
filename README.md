# Caching Proxy

This project is a simple caching HTTP proxy server. It forwards requests to a specified origin server, caches the responses, and serves cached responses for subsequent requests. It also includes an administrative endpoint to clear the cache.

## Features

- Forward HTTP requests to an origin server.
- Cache responses and serve them for subsequent requests.
- Administrative endpoint to clear the cache.
- Support for dynamic port and origin URL through command-line arguments.

## Prerequisites

- .NET 6.0 SDK or later
- An origin server to forward requests to.

## Installation

1. Clone the repository:

```
git clone <repository-url>
```

2. Navigate to the project directory:

```
cd <project-directory>
```

3. Build the project:

```
dotnet build
```

## Usage
To start the caching proxy server, you need to provide the port and the origin URL as arguments:

```
dotnet run --port <number> --origin <url>
```

For example:
```
dotnet run --port 3000 --origin https://example.com
```

Arguments
- --port <number>: The port on which the proxy server will listen.
- --origin <url>: The origin server to forward the requests to.
- --clear-cache: Clears the cache without starting the server.

## Administrative Endpoints
You can clear the cache via an HTTP request:
```
GET /admin/clear-cache
```

To manually clear the cache via the command line:
```
dotnet run --clear-cache
```
This will send a request to http://localhost:<port>/admin/clear-cache to clear the cache.

## Example
1. Start the proxy server on port 3000, forwarding requests to https://example.com:
```
dotnet run --port 3000 --origin https://example.com
```
2. Make requests to http://localhost:3000/<path> to get cached responses from the origin.
3. Clear the cache by sending a request to:
```
curl http://localhost:3000/admin/clear-cache
```

## Cache Behavior
- If a request is cached, the proxy will respond with a cached copy and include the header X-Cache: HIT.
- If a request is not cached, it will be forwarded to the origin server, and the response will be cached if the origin server responds successfully. The response will include the header X-Cache: MISS.

## Error Handling
If an error occurs while forwarding the request, the server will return a 502 Bad Gateway status code and an error message.

## License
This project is licensed under the MIT License.

```
This `README.md` provides a clear overview of the project, usage instructions, and examples to help users understand how to work with the caching proxy.
```
