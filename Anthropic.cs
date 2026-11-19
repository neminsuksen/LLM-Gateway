using System.Text.Json.Nodes;
public static class Anthropic
{
    public static JsonObject ConvertRequestMessage(JsonNode jsonNode)
    {
        JsonObject result = new JsonObject();
        result["model"] = jsonNode["model"]!.GetValue<string>();
        result["max_tokens"] = jsonNode["max_completion_tokens"]?.GetValue<int>() ?? jsonNode["max_tokens"]?.GetValue<int>() ?? 1024;

        /* Messages Convert */
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

        /* Tools Convert*/
        if (jsonNode["tools"] != null)
        {
            JsonArray tools = new JsonArray();
            foreach (JsonNode? tool in jsonNode["tools"]!.AsArray())
            {
                JsonNode function = tool!["function"]!;
                string toolName = function["name"]!.GetValue<string>();
                string toolDescription = function["description"]?.GetValue<string>() ?? "";
                JsonNode toolParameters = function["parameters"]!.DeepClone();
                tools.Add(new JsonObject
                {
                    ["name"] = toolName,
                    ["description"] = toolDescription,
                    ["input_schema"] = toolParameters
                });
            }
            result["tools"] = tools;
        }

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
        JsonArray toolCalls = new JsonArray();
        string? text = null;
        foreach (JsonNode? content in jsonNode["content"]!.AsArray())
        {
            string type = content!["type"]!.GetValue<string>();
            if (type == "text")
            {
                text += content["text"]!.GetValue<string>();
            }
            else if (type == "tool_use")
            {
                toolCalls.Add(new JsonObject
                {
                    ["id"] = content["id"]!.GetValue<string>(),
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = content["name"]!.GetValue<string>(),
                        ["arguments"] = content["input"]!.ToJsonString()
                    }
                });
            }
        }
        string finishReason = jsonNode["stop_reason"]!.GetValue<string>() switch
        {
            "max_tokens" => "length",
            "tool_use" => "tool_calls",
            _ => "stop"
        };

        JsonObject message = new JsonObject
        {
            ["role"] = jsonNode["role"]!.GetValue<string>(),
            ["content"] = text
        };
        if (toolCalls.Count > 0)
        {
            message["tool_calls"] = toolCalls;
        }
        choices.Add(new JsonObject
        {
            ["index"] = 0,
            ["message"] = message,
            ["finish_reason"] = finishReason
        });
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