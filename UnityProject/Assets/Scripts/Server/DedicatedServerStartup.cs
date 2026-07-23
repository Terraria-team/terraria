using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace Server
{
    public class DedicatedServerStartup : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("Starting game component");
            if (Application.isBatchMode)
            {
                Debug.Log("Starting dedicated server...");
                NetworkManager.singleton.StartServer();
        
                Application.wantsToQuit += OnServerShutdown;
        
                PlayerCountPollingLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
            }
        }
    
    
        private async UniTaskVoid PlayerCountPollingLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(60), cancellationToken: token);

                int count = NetworkServer.connections.Count;
            
                await ServerToLobbyApiClient.Instance.PostPlayerCountAsync(count); 
            }
        }
    
        private bool OnServerShutdown()
        {
            Debug.Log("SIGTERM received. Starting graceful shutdown...");

            // when serialisation available it should here and possibly spawn some other coroutine to do it every 30 minutes for example
            // SaveWorld(); 

        
            if (Mirror.NetworkServer.active)
            {
                Mirror.NetworkManager.singleton.StopServer();
            }
        
            return true; 
        }
    }
}
