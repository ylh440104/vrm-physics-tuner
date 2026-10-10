using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace VrmPhysicsTuner.Editor
{
    public class HttpPhysicsAssistant : IPhysicsAssistant
    {
        private readonly AssistantEndpoint _endpoint;

        public HttpPhysicsAssistant(AssistantEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public SuggestionBatch RequestSuggestions(VrmPhysicsConfig config, string intent)
        {
            var batch = new SuggestionBatch();
            if (_endpoint == null || !_endpoint.IsUsable())
            {
                batch.error = "endpoint not configured";
                return batch;
            }

            string payload;
            try
            {
                payload = ChatRequestBuilder.Build(_endpoint, config, intent);
            }
            catch (Exception e)
            {
                batch.error = e.Message;
                return batch;
            }

            var body = Encoding.UTF8.GetBytes(payload);
            var request = new UnityWebRequest(_endpoint.url, "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(_endpoint.apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + _endpoint.apiKey);
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                System.Threading.Thread.Sleep(10);
            }

            var responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
            var failed = request.result != UnityWebRequest.Result.Success;

            if (failed)
            {
                var parsedError = ChatResponseReader.ExtractContent(responseText);
                batch.raw = responseText;
                batch.error = string.IsNullOrEmpty(parsedError) ? request.error : parsedError;
                request.Dispose();
                return batch;
            }

            request.Dispose();

            try
            {
                var content = ChatResponseReader.ExtractContent(responseText);
                var parsed = AssistantResponseParser.Parse(content);
                parsed.raw = responseText;
                return parsed;
            }
            catch (Exception e)
            {
                batch.raw = responseText;
                batch.error = e.Message;
                return batch;
            }
        }
    }
}