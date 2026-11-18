using System.Text;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

HttpClient httpClient = new HttpClient();
Dictionary<string, ModelInfo> models = new()
{
    ["gpt-5-nano"] = new("OpenAI", 0.05m, 0.40m),
    ["gpt-6-luna"] = new("OpenAI", 0.10m, 0.50m),
    ["gpt-6.1-terra"] = new("OpenAI", 1.00m, 5.00m),
    ["claude-haiku-5-5"] = new("Anthropic", 0.10m, 0.50m),
    ["claude-sonnet-5-5"] = new("Anthropic", 2.00m, 10.00m)
};

app.MapPost("/v1/chat/completions", async (HttpRequest request) =>
{
    StreamReader reader = new StreamReader(request.Body);
    string body = await reader.ReadToEndAsync();
    JsonNode requestJson = JsonNode.Parse(body)!;

    string model = requestJson["model"]!.GetValue<string>() switch
    {
        "auto-cheap" => models.MinBy(m => m.Value.InputPrice).Key,
        "auto-smart" => models.MaxBy(m => m.Value.InputPrice).Key,
        var name => name
    };
    requestJson["model"] = model;
    if (!models.TryGetValue(model, out ModelInfo? info))
    {
        return Results.Content(new JsonObject
        {
            ["error"] = new JsonObject
            {
                ["message"] = $"The model {model} does not exist or you do not have access to it.",
                ["type"] = "invalid_request_error",
                ["param"] = null,
                ["code"] = "model_not_found"
            }
        }.ToJsonString(), "application/json", statusCode: 404);
    }
    bool isAnthropic = info.Provider == "Anthropic";
    string url = isAnthropic ? "https://api.anthropic.com/v1/messages" : "https://api.openai.com/v1/chat/completions";
    string upstreamBody = isAnthropic ? Anthropic.ConvertRequestMessage(requestJson).ToJsonString() : requestJson.ToJsonString();
    string keyName = isAnthropic ? "ANTHROPIC_API_KEY" : "OPENAI_API_KEY";

    HttpRequestMessage upstreamRequest = new HttpRequestMessage(HttpMethod.Post, url);
    upstreamRequest.Content = new StringContent(upstreamBody, Encoding.UTF8, "application/json");
    upstreamRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable(keyName));
    if (isAnthropic)
    {
        upstreamRequest.Headers.Add("anthropic-version", "2026-10-21");
    }
    HttpResponseMessage upstreamResponse = await httpClient.SendAsync(upstreamRequest);
    string llmResponseRaw = await upstreamResponse.Content.ReadAsStringAsync();
    string llmResponseOpenAiFormat = upstreamResponse.IsSuccessStatusCode && isAnthropic ? Anthropic.ConvertResponseMessage(llmResponseRaw) : llmResponseRaw;
    if (upstreamResponse.IsSuccessStatusCode)
    {
        JsonNode responseJson = JsonNode.Parse(llmResponseOpenAiFormat)!;
        JsonNode usage = responseJson["usage"]!;
        int promptTokens = usage["prompt_tokens"]!.GetValue<int>();
        int completionTokens = usage["completion_tokens"]!.GetValue<int>();
        decimal promptTokenCost = promptTokens * info.InputPrice / 1_000_000m;
        decimal completionTokenCost = completionTokens * info.OutputPrice / 1_000_000m;
        Console.WriteLine($"Model:{model} prompt: {promptTokens} completion: {completionTokens} cost: {promptTokenCost + completionTokenCost}");
    }
    return Results.Content(llmResponseOpenAiFormat, "application/json", statusCode: (int)upstreamResponse.StatusCode);
});

app.Run();

record ModelInfo(string Provider, decimal InputPrice, decimal OutputPrice);