using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace Server
{
    // separate later for chunks like
    //ChunkNetworkManager: Holds the SyncList and handles Commands.
    // ChunkVisualRenderer: Listens to the SyncList callbacks and manages the actual Unity Tilemap meshes and textures.
    public class BlockWorldManager : NetworkBehaviour
    {
        public static BlockWorldManager Instance;
        
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 30;
        [SerializeField] private int gridHeight = 30;
        
        private SyncList<int> blockGrid = new SyncList<int>();
        
        [Header("Grid Visual")]
        [SerializeField] private Tilemap playerGrid;
        [SerializeField] private List<TileBase> blockTexture =  new List<TileBase>();

        void Awake()
        {
            Instance = this;
        }
        
        // Server
        public override void OnStartServer()
        {
            for (int i = 0; i < gridWidth * gridHeight; i++)
            {
                blockGrid.Add(0);
            }
        }

        // Client
        public override void OnStartClient()
        {
            blockGrid.Callback += OnBlockChanged;

            for (int i = 0; i < blockGrid.Count; i++)
            {
                UpdateTileVisual(i, blockGrid[i]);
            }
        }

        public override void OnStopClient()
        {
            blockGrid.Callback -= OnBlockChanged;
        }


        private void OnBlockChanged(SyncList<int>.Operation op, int index, int oldBlockId, int newBlockId)
        {
            if (op != SyncList<int>.Operation.OP_SET) return;
            
            int gridX = index % gridWidth;
            int gridY = index / gridWidth;

            Debug.Log("Block on coordinates: " + gridX + ", " + gridY +" was changed.");
            UpdateTileVisual(index, newBlockId); 
        }

        [Command(requiresAuthority =  false)]
        public void CmdPlaceBlock(int x, int y, int blockId)
        {
            //if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            int currentBlockId = blockGrid[(y * gridWidth) + x];

            if (currentBlockId != 0)
            {
                return;
            }

            blockGrid[(y * gridWidth) + x] = blockId;
        }
        
        [Command(requiresAuthority =  false)]
        public void CmdBreakBlock(int x, int y)
        {
            //if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
            
            int currentBlockId = blockGrid[(y * gridWidth) + x];

            if (currentBlockId == 0)
            {
                return;
            }

            blockGrid[(y * gridWidth) + x] = 0;
        }

        public void UpdateTileVisual(int index, int blockId)
        {
            int x = index % gridWidth;
            int y = index / gridWidth;
            
            Vector3Int tilePosition = new Vector3Int(x, y, 0);
        
            TileBase tileToSet = (blockId == 0) ? null : blockTexture[blockId];
            playerGrid.SetTile(tilePosition, tileToSet);
        }
    }
}