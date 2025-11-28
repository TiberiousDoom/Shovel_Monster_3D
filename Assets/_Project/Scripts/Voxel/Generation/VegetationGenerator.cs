using UnityEngine;

namespace VoxelRPG.Voxel.Generation
{
    /// <summary>
    /// Generates vegetation (trees, plants) in the world.
    /// </summary>
    public class VegetationGenerator
    {
        private readonly int _seed;

        public VegetationGenerator(int seed)
        {
            _seed = seed;
        }

        /// <summary>
        /// Generates a tree at the specified position.
        /// </summary>
        /// <param name="world">The voxel world to place blocks in.</param>
        /// <param name="position">Base position of the tree (where trunk starts).</param>
        /// <param name="treeType">Tree type definition.</param>
        /// <param name="random">Random instance for variation.</param>
        public void GenerateTree(VoxelWorld world, Vector3Int position, TreeType treeType, System.Random random)
        {
            if (world == null || treeType == null)
            {
                return;
            }

            if (treeType.TrunkBlock == null || treeType.LeavesBlock == null)
            {
                Debug.LogWarning("[VegetationGenerator] Tree type missing trunk or leaves block.");
                return;
            }

            // Determine trunk height
            int trunkHeight = random.Next(treeType.MinTrunkHeight, treeType.MaxTrunkHeight + 1);

            // Generate trunk
            for (int y = 0; y < trunkHeight; y++)
            {
                var trunkPos = new Vector3Int(position.x, position.y + y, position.z);
                if (world.IsPositionValid(trunkPos))
                {
                    world.RequestBlockChange(trunkPos, treeType.TrunkBlock);
                }
            }

            // Generate leaves
            int leafRadius = treeType.LeafRadius;
            int leafStartY = position.y + trunkHeight - leafRadius;
            int leafEndY = position.y + trunkHeight + leafRadius;

            for (int y = leafStartY; y <= leafEndY; y++)
            {
                // Radius decreases at top and bottom
                int distFromCenter = Mathf.Abs(y - (position.y + trunkHeight));
                int currentRadius = leafRadius - (distFromCenter / 2);

                if (currentRadius < 1)
                {
                    currentRadius = 1;
                }

                for (int x = -currentRadius; x <= currentRadius; x++)
                {
                    for (int z = -currentRadius; z <= currentRadius; z++)
                    {
                        // Skip corners for more natural shape
                        if (Mathf.Abs(x) == currentRadius && Mathf.Abs(z) == currentRadius)
                        {
                            // Random chance to include corners
                            if (random.NextDouble() > 0.5)
                            {
                                continue;
                            }
                        }

                        // Skip trunk position
                        if (x == 0 && z == 0 && y < position.y + trunkHeight)
                        {
                            continue;
                        }

                        var leafPos = new Vector3Int(position.x + x, y, position.z + z);
                        if (world.IsPositionValid(leafPos))
                        {
                            // Only place leaves where there's air
                            var existingBlock = world.GetBlock(leafPos);
                            if (existingBlock == null || existingBlock == BlockType.Air)
                            {
                                world.RequestBlockChange(leafPos, treeType.LeavesBlock);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a simple bush/shrub at the specified position.
        /// </summary>
        /// <param name="world">The voxel world to place blocks in.</param>
        /// <param name="position">Position of the bush.</param>
        /// <param name="leafBlock">Block type for leaves.</param>
        /// <param name="random">Random instance for variation.</param>
        public void GenerateBush(VoxelWorld world, Vector3Int position, BlockType leafBlock, System.Random random)
        {
            if (world == null || leafBlock == null)
            {
                return;
            }

            // Simple 3x3x2 bush
            for (int y = 0; y < 2; y++)
            {
                int radius = y == 0 ? 1 : 0;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        // Skip corners on bottom layer
                        if (y == 0 && Mathf.Abs(x) == 1 && Mathf.Abs(z) == 1)
                        {
                            if (random.NextDouble() > 0.3)
                            {
                                continue;
                            }
                        }

                        var bushPos = new Vector3Int(position.x + x, position.y + y, position.z + z);
                        if (world.IsPositionValid(bushPos))
                        {
                            var existingBlock = world.GetBlock(bushPos);
                            if (existingBlock == null || existingBlock == BlockType.Air)
                            {
                                world.RequestBlockChange(bushPos, leafBlock);
                            }
                        }
                    }
                }
            }
        }
    }
}
