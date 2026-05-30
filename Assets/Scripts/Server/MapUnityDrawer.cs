using UnityEngine;
using System.Collections.Generic;
using Core.WorldGeneration;
using UnityEngine.UI;

namespace Server
{
    [RequireComponent(typeof(WorldGenerationSettings))]
    public class MapUnityDrawer : MonoBehaviour
    {
        int[,] map;
        public WorldGenerationSettings _settings;

        void Start() {
            _settings = GetComponent<WorldGenerationSettings>();
            if(_settings == null)
            {
                Debug.Log("nuul");
            }
            GenerateMap();
        }
        

        void OnValidate() {
            _settings = GetComponent<WorldGenerationSettings>();
            GenerateMap();
        }

        void GenerateMap()
        {
            Dictionary<BiomeType, IBiomeGenerationConfig> biomeDict = new Dictionary<BiomeType, IBiomeGenerationConfig>();
            foreach (var biomeConfig in _settings.Biomes)
            {
                if (biomeConfig != null && !biomeDict.ContainsKey(biomeConfig.BiomeType))
                {
                    biomeDict.Add(biomeConfig.BiomeType, biomeConfig);
                }
            }
            
            MapGenerator generator = new MapGenerator(_settings, biomeDict);
            BlockType[,] testWorld = generator.Generate();
            
            map = new int[_settings.Width, _settings.Height];
            for (int x = 0; x < _settings.Width; x++) {
                for (int y = 0; y < _settings.Height; y++) {
                    map[x, y] = (testWorld[x, y] != BlockType.Air) ? 1 : 0;
                }
            }
        }
        
        void Update() {
            if (Input.GetKeyDown(KeyCode.Space)) {
                GenerateMap();
            }
        }

        void OnDrawGizmos() {
            if (map != null && _settings != null) {
                for (int x = 0; x < _settings.Width; x++) {
                    for (int y = 0; y < _settings.Height; y++) {
                   
                        Gizmos.color = (map[x,y] == 1) ? Color.black : Color.white;
           
                        Vector3 pos = new Vector3(-_settings.Width / 2f + x + 0.5f, -_settings.Height / 2f + y + 0.5f, 0);
                
                        Gizmos.DrawCube(pos, Vector3.one);
                    }
                }
            }
        }
    }
}