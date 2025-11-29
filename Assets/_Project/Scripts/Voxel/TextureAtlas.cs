using UnityEngine;

namespace VoxelRPG.Voxel
{
    /// <summary>
    /// Configuration for texture atlas used in voxel rendering.
    /// Phase 0B: Provides UV coordinate calculations for block textures.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Blocks/Texture Atlas", fileName = "TextureAtlas")]
    public class TextureAtlas : ScriptableObject
    {
        [Header("Atlas Settings")]
        [Tooltip("Number of tiles per row in the atlas")]
        [SerializeField] private int _tilesPerRow = 16;

        [Tooltip("Number of tiles per column in the atlas")]
        [SerializeField] private int _tilesPerColumn = 16;

        [Tooltip("Padding between tiles in pixels (to prevent bleeding)")]
        [SerializeField] private float _tilePadding = 0.001f;

        [Header("References")]
        [Tooltip("The texture atlas image")]
        [SerializeField] private Texture2D _atlasTexture;

        /// <summary>
        /// Number of tiles per row.
        /// </summary>
        public int TilesPerRow => _tilesPerRow;

        /// <summary>
        /// Number of tiles per column.
        /// </summary>
        public int TilesPerColumn => _tilesPerColumn;

        /// <summary>
        /// Total number of tiles in the atlas.
        /// </summary>
        public int TotalTiles => _tilesPerRow * _tilesPerColumn;

        /// <summary>
        /// Size of each tile in UV coordinates.
        /// </summary>
        public float TileSize => 1f / _tilesPerRow;

        /// <summary>
        /// The atlas texture.
        /// </summary>
        public Texture2D AtlasTexture => _atlasTexture;

        /// <summary>
        /// Gets the UV coordinates for a tile at the given index.
        /// </summary>
        /// <param name="tileIndex">The tile index (0 to TotalTiles-1).</param>
        /// <returns>UV rect (x=minU, y=minV, width=tileWidth, height=tileHeight).</returns>
        public Rect GetTileUVs(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= TotalTiles)
            {
                Debug.LogWarning($"[TextureAtlas] Invalid tile index: {tileIndex}");
                tileIndex = 0;
            }

            int column = tileIndex % _tilesPerRow;
            int row = tileIndex / _tilesPerRow;

            // UV origin is bottom-left, so we flip the row
            row = _tilesPerColumn - 1 - row;

            float tileWidth = 1f / _tilesPerRow;
            float tileHeight = 1f / _tilesPerColumn;

            float minU = column * tileWidth + _tilePadding;
            float minV = row * tileHeight + _tilePadding;
            float maxU = (column + 1) * tileWidth - _tilePadding;
            float maxV = (row + 1) * tileHeight - _tilePadding;

            return new Rect(minU, minV, maxU - minU, maxV - minV);
        }

        /// <summary>
        /// Gets the four UV corners for a tile (for quad generation).
        /// </summary>
        /// <param name="tileIndex">The tile index.</param>
        /// <returns>Array of 4 Vector2 UVs: [bottomLeft, bottomRight, topRight, topLeft].</returns>
        public Vector2[] GetTileUVCorners(int tileIndex)
        {
            var rect = GetTileUVs(tileIndex);

            return new Vector2[]
            {
                new Vector2(rect.x, rect.y),                        // Bottom-left
                new Vector2(rect.x + rect.width, rect.y),           // Bottom-right
                new Vector2(rect.x + rect.width, rect.y + rect.height), // Top-right
                new Vector2(rect.x, rect.y + rect.height)           // Top-left
            };
        }

        /// <summary>
        /// Converts row and column to tile index.
        /// </summary>
        /// <param name="row">Row (0 = top).</param>
        /// <param name="column">Column (0 = left).</param>
        /// <returns>The tile index.</returns>
        public int GetTileIndex(int row, int column)
        {
            return row * _tilesPerRow + column;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _tilesPerRow = Mathf.Max(1, _tilesPerRow);
            _tilesPerColumn = Mathf.Max(1, _tilesPerColumn);
            _tilePadding = Mathf.Clamp(_tilePadding, 0f, 0.1f);
        }
#endif
    }
}
