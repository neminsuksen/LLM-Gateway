using System.Text.Json.Nodes;
public static class Anthropic
{
    public static JsonObject ConvertMessage(JsonNode jsonNode)
    {
        JsonObject result = new JsonObject();
        result["model"] = jsonNode["model"]!.GetValue<string>();
        result["max_tokens"] = jsonNode["max_tokens"]?.GetValue<int>() ?? 1024;
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
}