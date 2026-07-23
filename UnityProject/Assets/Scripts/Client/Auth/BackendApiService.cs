using System;
using System.Threading.Tasks;
using Client.Config;
using Shared.Api;
using UnityEngine;
using UnityEngine.Networking;

namespace Client.Auth
{
    public class BackendApiService
    {
        private static BackendApiService _instance;
        public static BackendApiService Instance => _instance ??= new BackendApiService();

        private readonly string _baseUrl;
        public event Action OnSessionExpired;

        private BackendApiService()
        {
            var config = Resources.Load<ClientConfig>("ClientConfig");
            _baseUrl = config != null ? config.baseUrl : "http://localhost:5241";
        }
        
        public async Task<string> GetAsync(string endpoint) => await ExecuteRawAsync(endpoint, UnityWebRequest.kHttpVerbGET);
        
        public async Task<string> PostAsync(string endpoint, object jsonBody = null) => await ExecuteRawAsync(endpoint, UnityWebRequest.kHttpVerbPOST, jsonBody);

        private async Task<string> ExecuteRawAsync(string endpoint, string method, object jsonBody = null, bool isRetry = false)
        {
            string fullUrl = endpoint.StartsWith("http") ? endpoint : _baseUrl + endpoint;
            string token = AuthService.Instance.CurrentAccessToken;
            
            var (content, statusCode, error) = await HttpUtil.SendRawAsync(fullUrl, method, jsonBody, token);

            if (error == null) return content;

            if (statusCode == 401 && !isRetry)
            {
                bool refreshed = await AuthService.Instance.TrySilentRefreshAsync();

                if (refreshed) return await ExecuteRawAsync(endpoint, method, jsonBody, true);
                
                OnSessionExpired?.Invoke();
                return null;
                
            }

            Debug.LogError($"[API Error] {method} {fullUrl} ({statusCode}): {error} | Response: {content}");
            return null;
        }
    }
}