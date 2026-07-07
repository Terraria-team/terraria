using System;
using System.Collections.Generic;
using Client.Api;
using Client.Auth;
using Client.Config;
using LobbyUnityShared.DTOs;
using UnityEngine;

namespace Client.AuthorizedEndpoints
{
    public class AuthorizedEndpointsPresenter : IDisposable
    {
        private readonly IAuthorizedEndpointsView _view;
        private readonly BackendApiService _apiService;
        private readonly AuthService _authService;
        private readonly string _serverInstancesUrl;
        private readonly string _logoutAllUrl;

        public AuthorizedEndpointsPresenter(IAuthorizedEndpointsView view, BackendApiService apiService, AuthService authService)
        {
            _view = view;
            _apiService = apiService;
            _authService = authService;
            
            var config = Resources.Load<ClientConfig>("ClientConfig");
            _serverInstancesUrl = config != null ? config.serverInstancesEndpoint : "/api/server-instances";
            _logoutAllUrl = config != null ? config.logoutAllEndpoint : "/api/auth/logoutAll";
            
            _view.OnGetServersClicked += HandleGetServers;
            _view.OnCreateServerClicked += HandleCreateServer;
            _view.OnLogoutClicked += HandleLogout;
            _view.OnLogoutAllClicked += HandleLogoutAll;
        }

        private async void HandleGetServers()
        {
            _view.SetLoadingState(true);
            _view.UpdateStatus("Fetching active server instances...");

            string jsonResult = await _apiService.GetAsync(_serverInstancesUrl);

            _view.SetLoadingState(false);
            if (jsonResult != null)
            {
                _view.UpdateStatus("Server list retrieved successfully.");
                _view.DisplayServerResponse(jsonResult);
                
                try 
                {
                    var servers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ServerInstanceDto>>(jsonResult);
                    if (servers != null)
                        _view.ShowRetrievedServerElements(servers);
                } 
                catch(Exception ex) 
                {
                    Debug.LogError("Failed to parse servers: " + ex.Message);
                }
            }
            else
            {
                _view.ShowError("Failed to fetch servers or session expired.");
            }
        }

        private async void HandleCreateServer()
        {
            _view.SetLoadingState(true);
            _view.UpdateStatus("Requesting new server instance...");

            string jsonResult = await _apiService.PostAsync(_serverInstancesUrl);

            _view.SetLoadingState(false);
            if (jsonResult != null)
            {
                _view.UpdateStatus("New server instance created!");
                _view.DisplayServerResponse(jsonResult);
            }
            else
            {
                _view.ShowError("Failed to create server instance.");
            }
        }

        private async void HandleLogout()
        {
            _view.SetLoadingState(true);
            _view.UpdateStatus("Logging out...");
            await _authService.LogoutAsync();
            _view.SetLoadingState(false);
            _view.UpdateStatus("Logged out successfully.");
        }

        private async void HandleLogoutAll()
        {
            _view.SetLoadingState(true);
            _view.UpdateStatus("Revoking all active sessions...");
            await _apiService.PostAsync(_logoutAllUrl);
            await _authService.LogoutAsync();
            _view.SetLoadingState(false);
            _view.UpdateStatus("All sessions revoked.");
        }

        public void Dispose()
        {
            _view.OnGetServersClicked -= HandleGetServers;
            _view.OnCreateServerClicked -= HandleCreateServer;
            _view.OnLogoutClicked -= HandleLogout;
            _view.OnLogoutAllClicked -= HandleLogoutAll;
        }
    }
}
