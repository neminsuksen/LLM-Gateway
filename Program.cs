using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

HttpClient httpClient = new HttpClient();
app.MapPost("/v1/chat/completions", async (HttpRequest request) =>
{
    StreamReader reader = new StreamReader(request.Body);
    string body = await reader.ReadToEndAsync();
    JsonNode requestJson = JsonNode.Parse(body)!;
    string model = requestJson["model"]!.GetValue<string>();

    if (model.StartsWith("claude"))
    {
        JsonNode anthropicRequest = Anthropic.ConvertRequestMessage(requestJson);
        HttpRequestMessage anthropicUpstreamRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        anthropicUpstreamRequest.Content = new StringContent(anthropicRequest.ToJsonString(), Encoding.UTF8, "application/json");
        anthropicUpstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        anthropicUpstreamRequest.Headers.Add("anthropic-version", "2026-10-21");
        HttpResponseMessage anthropicUpstreamResponse = await httpClient.SendAsync(anthropicUpstreamRequest);
        string anthropicLlmResponse = await anthropicUpstreamResponse.Content.ReadAsStringAsync();
        string openAiLlmResponse = anthropicUpstreamResponse.IsSuccessStatusCode ? Anthropic.ConvertResponseMessage(anthropicLlmResponse) : anthropicLlmResponse;
        return Results.Content(openAiLlmResponse, "application/json", statusCode: (int)anthropicUpstreamResponse.StatusCode);
    }

    HttpRequestMessage upstreamRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
    upstreamRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
    upstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    HttpResponseMessage upstreamResponse = await httpClient.SendAsync(upstreamRequest);
    string llmResponse = await upstreamResponse.Content.ReadAsStringAsync();
    return Results.Content(llmResponse, "application/json", statusCode: (int)upstreamResponse.StatusCode);
});

app.Run();
