using System;
using System.Collections.Generic;
using LobbyUnityShared.DTOs;

namespace Client.AuthorizedEndpoints
{
    public interface IAuthorizedEndpointsView
    {
        event Action OnGetServersClicked;
        event Action OnCreateServerClicked;
        event Action OnLogoutClicked;
        event Action OnLogoutAllClicked;

        void DisplayServerResponse(string rawJson);
        void UpdateStatus(string message);
        void ShowError(string errorMessage);
        void SetLoadingState(bool isLoading);
        void ShowRetrievedServerElements(List<ServerInstanceDto> servers);
    }
}
