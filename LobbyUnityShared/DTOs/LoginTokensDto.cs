using System;

namespace LobbyUnityShared.DTOs
{
    [Serializable]
    public class LoginTokensDto
    {
        public string sessiontoken = string.Empty;

        public string accesstoken = string.Empty;
    }
}