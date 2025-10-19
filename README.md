### Concurrent Futures

This project was born as an attempt to mimic the [`concurrent.futures` python package.](https://docs.python.org/3/library/concurrent.futures.html).

If you're new to futures or curious about their inner workings, take a look at the [future.ipynb](./future.ipynb) file — it contains extra technical explanations and examples.

#### Example
```cs
using System.Net;
using Futures;


var sources = new[]
{
    "http://www.python.org/",
    "https://www.ruby-lang.org/",
    "https://dotty.epfl.ch",
    "https://ocaml.org/",
    "http://nonexistant-subdomain.python.org/"
};

using var executor = new ThreadPoolExecutor();
using var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(4) };
var futures = sources
    .Select(x => executor.Submit<HttpStatusCode>(s => Do(httpClient, (string)s!), x))
    .ToArray();
var done = Future.AsCompleted(futures);
foreach (var future in done)
{
    try
    {
        var result = future.GetResult();
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Future completed with exception: {ex.Message}");
    }
}

static HttpStatusCode Do(HttpClient httpClient, string requestUri)
{
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
    var responseMessage = httpClient.Send(requestMessage);
    return responseMessage.StatusCode;
}
```
