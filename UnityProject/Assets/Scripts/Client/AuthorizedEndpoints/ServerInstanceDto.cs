using System;

namespace Client.AuthorizedEndpoints
{
    [Serializable]
    public class ServerInstanceDto
    {
        public string id;
        public string containerId;
        public string image;
        public string name;
        public int port;
        public int playerCount;
        public string status;
    }
}
