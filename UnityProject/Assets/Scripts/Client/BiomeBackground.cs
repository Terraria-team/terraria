using Core.WorldGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class BiomeBackground : MonoBehaviour
{
    [SerializeField] private float transitionSpeed = 2f;
    [SerializeField] private Color undergroundColor = new Color(0.25f, 0.16f, 0.10f);
    private int surfaceDepthBuffer = 14;

    private Camera _camera;
    private Color _targetColor;
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

        BiomeGenerationData data = DataManager.Biomes[biome];
        if (data == null)
        {
            return;
        }

        bool underground = data.usesSurface && player.position.y < data.BaseSurfaceLevel - surfaceDepthBuffer;

        _targetColor = underground ? undergroundColor : data.associatedColor;
        _hasTarget = true;
    }
}
