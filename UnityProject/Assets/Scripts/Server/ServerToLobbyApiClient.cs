using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LobbyUnityShared;
using LobbyUnityShared.DTOs;
using Server.Config;
using Shared.Api;

namespace Server
{
    public class ServerToLobbyApiClient
    {
        private static ServerToLobbyApiClient _instance;
        public static ServerToLobbyApiClient Instance => _instance ??= new ServerToLobbyApiClient();

        private readonly string _baseUrl;
        private readonly string _serverInstancesUrl;
        private readonly string _serverApiKey;
        private readonly string _serverInstanceId;

        private ServerToLobbyApiClient()
        {
            var config = Resources.Load<ServerAsLobbyClientConfig>("ServerAsLobbyClientConfig");
            if (config != null)
            {
                _baseUrl = config.baseUrl;
                _serverInstancesUrl = config.serverInstancesEndpoint;
            }
            else
            {
                Debug.LogError("[AuthService] CRITICAL: ServerAsLobbyClientConfig not found in Resources folder!");
            }
            
            _serverApiKey = Environment.GetEnvironmentVariable(EnvVariables.ServerApiKey);
            _serverInstanceId = Environment.GetEnvironmentVariable(EnvVariables.ServerInstanceId);
        }

        public async Task<string> PostPlayerCountAsync(int count)
        {
            if (string.IsNullOrEmpty(_serverApiKey))
            {
                Debug.LogError("No Server API Key found! Cannot authenticate with Lobby.");
                return null;
            }

            var endpoint = $"{_baseUrl}{_serverInstancesUrl}{_serverInstanceId}/player-count";
            var payload = new UpdatePlayerCountDto { PlayerCount = count };
            
            var (content, statusCode, error) = await HttpUtil.SendRawAsync(
                endpoint, 
                UnityEngine.Networking.UnityWebRequest.kHttpVerbPOST, 
                payload, 
                null,
                new Dictionary<string, string>{{ApiConstants.ServerApiKeyHeader, _serverApiKey}});

            if (error != null)
            {
                Debug.LogError($"Server update failed: {error}");
            }
            return content;
        }
    }
}