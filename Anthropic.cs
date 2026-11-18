using System.Text.Json.Nodes;
public static class Anthropic
{
    public static JsonObject ConvertRequestMessage(JsonNode jsonNode)
    {
        JsonObject result = new JsonObject();
        result["model"] = jsonNode["model"]!.GetValue<string>();
        result["max_tokens"] = jsonNode["max_completion_tokens"]?.GetValue<int>() ?? jsonNode["max_tokens"]?.GetValue<int>() ?? 1024;
        JsonArray messages = new JsonArray();
        foreach (JsonNode? message in jsonNode["messages"]!.AsArray())
        {
            string role = message!["role"]!.GetValue<string>();
            if (role == "system")
            {
                result["system"] = message!["content"]!.GetValue<string>();
            }
            else
            {
                messages.Add(message.DeepClone());
            }
        }
        result["messages"] = messages;

        return result;
    }

    public static string ConvertResponseMessage(string jsonString)
    {
        JsonObject result = new JsonObject();
        JsonNode jsonNode = JsonNode.Parse(jsonString)!;
        result["id"] = jsonNode["id"]!.GetValue<string>();
        result["model"] = jsonNode["model"]!.GetValue<string>();
        result["object"] = "chat.completion";
        result["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        JsonArray choices = new JsonArray();
        string text = string.Empty;
        foreach (JsonNode? content in jsonNode["content"]!.AsArray())
        {
            string type = content!["type"]!.GetValue<string>();
            if (type == "text")
            {
                text += content["text"]!.GetValue<string>();
            }
        }
        string finishReason = jsonNode["stop_reason"]!.GetValue<string>() switch
        {
            "max_tokens" => "length",
            "tool_use" => "tool_calls",
            _ => "stop"
        };
        JsonObject choice = new JsonObject
        {
            ["index"] = 0,
            ["message"] = new JsonObject
            {
                ["role"] = jsonNode["role"]!.GetValue<string>(),
                ["content"] = text
            },
            ["finish_reason"] = finishReason
        };
        choices.Add(choice);
        result["choices"] = choices;
        JsonNode usage = jsonNode["usage"]!;
        result["usage"] = new JsonObject
        {
            ["prompt_tokens"] = usage["input_tokens"]!.GetValue<int>(),
            ["completion_tokens"] = usage["output_tokens"]!.GetValue<int>(),
            ["total_tokens"] = usage["input_tokens"]!.GetValue<int>() + usage["output_tokens"]!.GetValue<int>()
        };

        return result.ToJsonString();
    }
}