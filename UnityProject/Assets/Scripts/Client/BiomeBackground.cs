using Core.WorldGeneration;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BiomeBackground : MonoBehaviour
{
    [SerializeField] private float transitionSpeed = 2f;

    private Camera _camera;
    private Color _targetColor;
    private BiomeType _lastBiome;
    private bool _hasTarget;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _targetColor = _camera.backgroundColor;
    }

    private void Update()
    {
        RefreshTargetColor();

        if (_hasTarget)
        {
            _camera.backgroundColor = Color.Lerp(
                _camera.backgroundColor, _targetColor, Time.deltaTime * transitionSpeed
                );
        }
    }

    private void RefreshTargetColor()
    {
        Transform player = PlayerController.LocalPlayerTransform;
        if (player == null)
        {
            return;
        }

        Vector2Int chunkCoord = ChunkUtils.ChunkCoordsAtWorldPosition(player.position);
        BiomeType biome = MapGenerator.GetBiomeTypeAt(chunkCoord);

        if (_hasTarget && biome == _lastBiome)
        {
            return;
        }

        BiomeGenerationData data = DataManager.Biomes[biome];
        if (data == null)
        {
            return;
        }
        
        _targetColor = data.associatedColor;
        _lastBiome = biome;
        _hasTarget = true;
    }
}
