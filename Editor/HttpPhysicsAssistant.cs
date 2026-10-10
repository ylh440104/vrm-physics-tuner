using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Neko.VrmPhysicsTuner.Editor
{
    public class HttpPhysicsAssistant
    {
        private readonly AssistantEndpoint _endpoint;
        private UnityWebRequest _request;
        private SuggestionBatch _batch;

        public HttpPhysicsAssistant(AssistantEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public bool IsPending { get { return _request != null; } }

        public bool Begin(VrmPhysicsConfig config, string intent)
        {
            Cancel();

            if (_endpoint == null || !_endpoint.IsUsable()) return false;

            string payload;
            try
            {
                payload = ChatRequestBuilder.Build(_endpoint, config, intent);
            }
            catch (Exception)
            {
                return false;
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

            try
            {
                request.SendWebRequest();
            }
            catch (Exception)
            {
                request.Dispose();
                return false;
            }

            _request = request;
            _batch = null;
            return true;
        }

        public SuggestionBatch Poll()
        {
            if (_request == null) return _batch;

            if (!_request.isDone) return null;

            var request = _request;
            _request = null;

            var responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
            var failed = request.result != UnityWebRequest.Result.Success;
            var transportError = request.error;
            request.Dispose();

            if (failed)
            {
                var batch = new SuggestionBatch();
                batch.raw = responseText;
                var parsedError = ChatResponseReader.ExtractContent(responseText);
                batch.error = string.IsNullOrEmpty(parsedError) ? transportError : parsedError;
                _batch = batch;
                return _batch;
            }

            try
            {
                var content = ChatResponseReader.ExtractContent(responseText);
                var parsed = AssistantResponseParser.Parse(content);
                parsed.raw = responseText;
                _batch = parsed;
            }
            catch (Exception e)
            {
                var batch = new SuggestionBatch();
                batch.raw = responseText;
                batch.error = e.Message;
                _batch = batch;
            }

            return _batch;
        }

        public void Cancel()
        {
            if (_request == null) return;
            try
            {
                _request.Abort();
            }
            catch (Exception)
            {
            }
            _request.Dispose();
            _request = null;
        }
    }
}