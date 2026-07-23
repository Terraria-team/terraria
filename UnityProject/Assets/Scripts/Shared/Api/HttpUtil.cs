using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Shared.Api
{
    public static class HttpUtil
    {
        public static async Task<(string content, long statusCode, string error)> 
            SendRawAsync(
                string url,
                string method,
                object jsonBody = null,
                string bearerToken = null,
                Dictionary<string, string> headers = null
            )
        {
            using var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (jsonBody != null)
            {
                string jsonString = JsonConvert.SerializeObject(jsonBody);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
            
            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return (request.downloadHandler.text, request.responseCode, null);
            }

            return (request.downloadHandler.text, request.responseCode, request.error);
        }

        public static async Task<T> 
            SendAsync<T>(
                string url,
                string method,
                object jsonBody = null,
                string bearerToken = null
            ) where T : class
        {
            var (content, statusCode, error) = await SendRawAsync(url, method, jsonBody, bearerToken);
            
            if (error != null || string.IsNullOrEmpty(content))
            {
                Debug.LogError($"[HttpUtil] {method} {url} Failed ({statusCode}): {error} | Payload: {content}");
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HttpUtil] Deserialization Error for {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
    }
}