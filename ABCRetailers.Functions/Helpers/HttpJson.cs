using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABCRetailers.Functions.Helpers;

public static class HttpJson
{
    // Deserialize request body to type T
    public static async Task<T> ReadRequestBodyAsync<T>(HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    // Write an object as JSON to response
    public static HttpResponseData WriteJsonResponse<T>(HttpResponseData res, T obj, System.Net.HttpStatusCode statusCode)
    {
        res.StatusCode = statusCode;
        res.Headers.Add("Content-Type", "application/json");
        res.WriteString(JsonSerializer.Serialize(obj));
        return res;
    }
}