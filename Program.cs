using System.Text;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

HttpClient httpClient = new HttpClient();
app.MapPost("/v1/chat/completions", async (HttpRequest request) =>
{
    StreamReader reader = new StreamReader(request.Body);
    string body = await reader.ReadToEndAsync();
    HttpRequestMessage upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
    upstreamRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
    upstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    HttpResponseMessage upstreamResponse = await httpClient.SendAsync(upstreamRequest);
    string llmResponse = await upstreamResponse.Content.ReadAsStringAsync();
    return Results.Content(llmResponse, "application/json", statusCode: (int)upstreamResponse.StatusCode);
});

app.Run();
