using System;
using System.Text;

namespace VrmPhysicsTuner
{
    public class AssistantEndpoint
    {
        public string url = "";
        public string apiKey = "";
        public string model = "";

        public bool IsUsable()
        {
            return !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(model);
        }
    }

    public static class ChatRequestBuilder
    {
        public static string Build(AssistantEndpoint endpoint, VrmPhysicsConfig config, string intent)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var w = new JsonWriter();
            w.BeginObject();
            w.Key("model").Value(endpoint.model ?? "");
            w.Key("temperature").Value(0.2f);
            w.Key("messages").BeginArray();

            w.BeginObject();
            w.Key("role").Value("system");
            w.Key("content").Value(AssistantPrompt.BuildSystemPrompt());
            w.EndObject();

            w.BeginObject();
            w.Key("role").Value("user");
            w.Key("content").Value(AssistantPrompt.BuildUserPrompt(config, intent));
            w.EndObject();

            w.EndArray();
            w.EndObject();
            return w.ToString();
        }
    }

    public static class ChatResponseReader
    {
        public static string ExtractContent(string body)
        {
            if (string.IsNullOrEmpty(body)) return "";
            try
            {
                var root = JsonParser.Parse(body).AsObject();

                var error = root.Get("error");
                if (error != null)
                {
                    var message = error.AsObject().Get("message").AsString("");
                    if (!string.IsNullOrEmpty(message)) throw new InvalidOperationException(message);
                }

                var choices = root.Get("choices");
                if (!(choices is System.Collections.Generic.List<object>)) return "";

                var list = choices.AsArray();
                if (list.Count == 0) return "";

                var first = list[0].AsObject();

                var messageNode = first.Get("message");
                if (messageNode != null)
                {
                    var content = messageNode.AsObject().Get("content").AsString("");
                    if (!string.IsNullOrEmpty(content)) return content;
                }

                return first.Get("text").AsString("");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}