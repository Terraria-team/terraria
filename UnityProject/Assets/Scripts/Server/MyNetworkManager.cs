using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector3 spawnPos = ChunkManager.Instance.GetCastPositionFromChunk(new Vector2Int(
            ChunkManager.Instance.WorldSize.x / 2, ChunkManager.Instance.WorldSize.y - 1
        ));
        
        GameObject player = Instantiate(
            playerPrefab,
            spawnPos,
            Quaternion.identity
        );

        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
