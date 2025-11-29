#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoxelRPG.Voxel;
using VoxelRPG.Voxel.Generation;

namespace VoxelRPG.Editor
{
    /// <summary>
    /// Editor utility to create default BlockType and BiomeDefinition assets
    /// for Phase 1.1 World Generation testing.
    /// </summary>
    public static class WorldGenerationAssetCreator
    {
        private const string BlocksPath = "Assets/_Project/ScriptableObjects/Blocks";
        private const string BiomesPath = "Assets/_Project/ScriptableObjects/Biomes";

        [MenuItem("VoxelRPG/Create Default World Generation Assets")]
        public static void CreateAllDefaultAssets()
        {
            CreateDirectories();
            CreateBlockTypes();
            CreateBiomes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WorldGenerationAssetCreator] All default assets created successfully!");
        }

        [MenuItem("VoxelRPG/Create Block Types Only")]
        public static void CreateBlockTypesOnly()
        {
            CreateDirectories();
            CreateBlockTypes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WorldGenerationAssetCreator] Block types created successfully!");
        }

        [MenuItem("VoxelRPG/Create Biomes Only")]
        public static void CreateBiomesOnly()
        {
            CreateDirectories();
            CreateBiomes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WorldGenerationAssetCreator] Biomes created successfully!");
        }

        private static void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            }
            if (!AssetDatabase.IsValidFolder(BlocksPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Blocks");
            }
            if (!AssetDatabase.IsValidFolder(BiomesPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Biomes");
            }
        }

        private static void CreateBlockTypes()
        {
            // Terrain blocks
            CreateBlockType("Grass", new Color(0.3f, 0.7f, 0.2f), true, false, 0.5f);
            CreateBlockType("Dirt", new Color(0.5f, 0.35f, 0.2f), true, false, 0.5f);
            CreateBlockType("Stone", new Color(0.5f, 0.5f, 0.5f), true, false, 1.5f);
            CreateBlockType("Sand", new Color(0.9f, 0.85f, 0.6f), true, false, 0.4f);

            // Water (transparent, non-solid)
            CreateBlockType("Water", new Color(0.2f, 0.4f, 0.8f, 0.5f), false, true, 0f, false);

            // Tree blocks
            CreateBlockType("OakWood", new Color(0.55f, 0.35f, 0.15f), true, false, 1.2f);
            CreateBlockType("OakLeaves", new Color(0.15f, 0.5f, 0.1f), true, true, 0.2f);

            // Ore blocks
            CreateBlockType("CoalOre", new Color(0.2f, 0.2f, 0.2f), true, false, 2f);
            CreateBlockType("IronOre", new Color(0.7f, 0.55f, 0.45f), true, false, 2.5f);
            CreateBlockType("GoldOre", new Color(0.9f, 0.8f, 0.2f), true, false, 3f);
            CreateBlockType("DiamondOre", new Color(0.4f, 0.9f, 0.95f), true, false, 4f);

            Debug.Log("[WorldGenerationAssetCreator] Created block types: Grass, Dirt, Stone, Sand, Water, OakWood, OakLeaves, CoalOre, IronOre, GoldOre, DiamondOre");
        }

        private static BlockType CreateBlockType(string name, Color color, bool isSolid, bool isTransparent, float hardness, bool isPlaceable = true)
        {
            string path = $"{BlocksPath}/{name}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<BlockType>(path);
            if (existing != null)
            {
                Debug.Log($"[WorldGenerationAssetCreator] Block type '{name}' already exists, skipping.");
                return existing;
            }

            var blockType = ScriptableObject.CreateInstance<BlockType>();

            // Use serialized object to set private fields
            var serializedObject = new SerializedObject(blockType);
            serializedObject.FindProperty("_id").stringValue = name.ToLowerInvariant();
            serializedObject.FindProperty("_displayName").stringValue = name;
            serializedObject.FindProperty("_color").colorValue = color;
            serializedObject.FindProperty("_isSolid").boolValue = isSolid;
            serializedObject.FindProperty("_isTransparent").boolValue = isTransparent;
            serializedObject.FindProperty("_hardness").floatValue = hardness;
            serializedObject.FindProperty("_isPlaceable").boolValue = isPlaceable;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(blockType, path);
            Debug.Log($"[WorldGenerationAssetCreator] Created block type: {name}");

            return blockType;
        }

        private static void CreateBiomes()
        {
            // Load block types first
            var grass = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/Grass.asset");
            var dirt = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/Dirt.asset");
            var stone = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/Stone.asset");
            var sand = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/Sand.asset");
            var water = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/Water.asset");
            var oakWood = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/OakWood.asset");
            var oakLeaves = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/OakLeaves.asset");
            var coalOre = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/CoalOre.asset");
            var ironOre = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/IronOre.asset");
            var goldOre = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/GoldOre.asset");
            var diamondOre = AssetDatabase.LoadAssetAtPath<BlockType>($"{BlocksPath}/DiamondOre.asset");

            if (grass == null || dirt == null || stone == null)
            {
                Debug.LogError("[WorldGenerationAssetCreator] Block types not found! Run 'Create Block Types Only' first.");
                return;
            }

            // Create Forest biome
            CreateForestBiome(grass, dirt, stone, sand, water, oakWood, oakLeaves,
                coalOre, ironOre, goldOre, diamondOre);

            // Create Desert biome
            CreateDesertBiome(sand, stone, water, coalOre, ironOre, goldOre);

            // Create Plains biome
            CreatePlainsBiome(grass, dirt, stone, sand, water, oakWood, oakLeaves,
                coalOre, ironOre, goldOre);

            Debug.Log("[WorldGenerationAssetCreator] Created biomes: Forest, Desert, Plains");
        }

        private static void CreateForestBiome(BlockType grass, BlockType dirt, BlockType stone,
            BlockType sand, BlockType water, BlockType oakWood, BlockType oakLeaves,
            BlockType coalOre, BlockType ironOre, BlockType goldOre, BlockType diamondOre)
        {
            string path = $"{BiomesPath}/Forest.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
            if (existing != null)
            {
                Debug.Log("[WorldGenerationAssetCreator] Forest biome already exists, skipping.");
                return;
            }

            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            var serializedObject = new SerializedObject(biome);

            // Identification
            serializedObject.FindProperty("_id").stringValue = "forest";
            serializedObject.FindProperty("_displayName").stringValue = "Forest";

            // Terrain blocks
            serializedObject.FindProperty("_topBlock").objectReferenceValue = grass;
            serializedObject.FindProperty("_fillerBlock").objectReferenceValue = dirt;
            serializedObject.FindProperty("_stoneBlock").objectReferenceValue = stone;
            serializedObject.FindProperty("_waterBlock").objectReferenceValue = water;
            serializedObject.FindProperty("_beachBlock").objectReferenceValue = sand;

            // Terrain shape
            serializedObject.FindProperty("_baseHeight").intValue = 32;
            serializedObject.FindProperty("_heightVariation").intValue = 16;
            serializedObject.FindProperty("_fillerDepth").intValue = 4;
            serializedObject.FindProperty("_stoneStartHeight").intValue = 20;

            // Water settings
            serializedObject.FindProperty("_waterLevel").intValue = 28;
            serializedObject.FindProperty("_beachHeight").intValue = 2;

            // Vegetation
            serializedObject.FindProperty("_treeChance").floatValue = 0.03f;

            // Tree types
            var treeTypesProp = serializedObject.FindProperty("_treeTypes");
            treeTypesProp.arraySize = 1;
            var treeProp = treeTypesProp.GetArrayElementAtIndex(0);
            treeProp.FindPropertyRelative("TrunkBlock").objectReferenceValue = oakWood;
            treeProp.FindPropertyRelative("LeavesBlock").objectReferenceValue = oakLeaves;
            treeProp.FindPropertyRelative("MinTrunkHeight").intValue = 4;
            treeProp.FindPropertyRelative("MaxTrunkHeight").intValue = 7;
            treeProp.FindPropertyRelative("LeafRadius").intValue = 2;

            // Ore configs
            var oreConfigsProp = serializedObject.FindProperty("_oreConfigs");
            oreConfigsProp.arraySize = 4;

            // Coal ore (common, shallow to mid)
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(0), coalOre, 3, 80, 0.08f, 0.12f);

            // Iron ore (common, mid-depth)
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(1), ironOre, 10, 64, 0.06f, 0.10f);

            // Gold ore (rare, deep)
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(2), goldOre, 25, 48, 0.03f, 0.08f);

            // Diamond ore (very rare, very deep)
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(3), diamondOre, 35, 16, 0.01f, 0.05f);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(biome, path);
            Debug.Log("[WorldGenerationAssetCreator] Created Forest biome");
        }

        private static void CreateDesertBiome(BlockType sand, BlockType stone, BlockType water,
            BlockType coalOre, BlockType ironOre, BlockType goldOre)
        {
            string path = $"{BiomesPath}/Desert.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
            if (existing != null)
            {
                Debug.Log("[WorldGenerationAssetCreator] Desert biome already exists, skipping.");
                return;
            }

            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            var serializedObject = new SerializedObject(biome);

            // Identification
            serializedObject.FindProperty("_id").stringValue = "desert";
            serializedObject.FindProperty("_displayName").stringValue = "Desert";

            // Terrain blocks
            serializedObject.FindProperty("_topBlock").objectReferenceValue = sand;
            serializedObject.FindProperty("_fillerBlock").objectReferenceValue = sand;
            serializedObject.FindProperty("_stoneBlock").objectReferenceValue = stone;
            serializedObject.FindProperty("_waterBlock").objectReferenceValue = water;
            serializedObject.FindProperty("_beachBlock").objectReferenceValue = sand;

            // Terrain shape (flatter than forest)
            serializedObject.FindProperty("_baseHeight").intValue = 30;
            serializedObject.FindProperty("_heightVariation").intValue = 8;
            serializedObject.FindProperty("_fillerDepth").intValue = 6;
            serializedObject.FindProperty("_stoneStartHeight").intValue = 18;

            // Water settings (lower water in desert)
            serializedObject.FindProperty("_waterLevel").intValue = 20;
            serializedObject.FindProperty("_beachHeight").intValue = 1;

            // No trees in desert
            serializedObject.FindProperty("_treeChance").floatValue = 0f;

            // Ore configs (more gold in desert)
            var oreConfigsProp = serializedObject.FindProperty("_oreConfigs");
            oreConfigsProp.arraySize = 3;

            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(0), coalOre, 5, 70, 0.05f, 0.12f);
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(1), ironOre, 15, 55, 0.05f, 0.10f);
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(2), goldOre, 20, 40, 0.05f, 0.08f);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(biome, path);
            Debug.Log("[WorldGenerationAssetCreator] Created Desert biome");
        }

        private static void CreatePlainsBiome(BlockType grass, BlockType dirt, BlockType stone,
            BlockType sand, BlockType water, BlockType oakWood, BlockType oakLeaves,
            BlockType coalOre, BlockType ironOre, BlockType goldOre)
        {
            string path = $"{BiomesPath}/Plains.asset";

            var existing = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
            if (existing != null)
            {
                Debug.Log("[WorldGenerationAssetCreator] Plains biome already exists, skipping.");
                return;
            }

            var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
            var serializedObject = new SerializedObject(biome);

            // Identification
            serializedObject.FindProperty("_id").stringValue = "plains";
            serializedObject.FindProperty("_displayName").stringValue = "Plains";

            // Terrain blocks
            serializedObject.FindProperty("_topBlock").objectReferenceValue = grass;
            serializedObject.FindProperty("_fillerBlock").objectReferenceValue = dirt;
            serializedObject.FindProperty("_stoneBlock").objectReferenceValue = stone;
            serializedObject.FindProperty("_waterBlock").objectReferenceValue = water;
            serializedObject.FindProperty("_beachBlock").objectReferenceValue = sand;

            // Terrain shape (very flat)
            serializedObject.FindProperty("_baseHeight").intValue = 34;
            serializedObject.FindProperty("_heightVariation").intValue = 6;
            serializedObject.FindProperty("_fillerDepth").intValue = 4;
            serializedObject.FindProperty("_stoneStartHeight").intValue = 22;

            // Water settings
            serializedObject.FindProperty("_waterLevel").intValue = 28;
            serializedObject.FindProperty("_beachHeight").intValue = 2;

            // Sparse trees
            serializedObject.FindProperty("_treeChance").floatValue = 0.005f;

            // Tree types
            var treeTypesProp = serializedObject.FindProperty("_treeTypes");
            treeTypesProp.arraySize = 1;
            var treeProp = treeTypesProp.GetArrayElementAtIndex(0);
            treeProp.FindPropertyRelative("TrunkBlock").objectReferenceValue = oakWood;
            treeProp.FindPropertyRelative("LeavesBlock").objectReferenceValue = oakLeaves;
            treeProp.FindPropertyRelative("MinTrunkHeight").intValue = 3;
            treeProp.FindPropertyRelative("MaxTrunkHeight").intValue = 5;
            treeProp.FindPropertyRelative("LeafRadius").intValue = 2;

            // Ore configs
            var oreConfigsProp = serializedObject.FindProperty("_oreConfigs");
            oreConfigsProp.arraySize = 3;

            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(0), coalOre, 4, 75, 0.07f, 0.12f);
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(1), ironOre, 12, 60, 0.055f, 0.10f);
            SetOreConfig(oreConfigsProp.GetArrayElementAtIndex(2), goldOre, 28, 44, 0.025f, 0.08f);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(biome, path);
            Debug.Log("[WorldGenerationAssetCreator] Created Plains biome");
        }

        private static void SetOreConfig(SerializedProperty oreProp, BlockType oreBlock,
            int minDepth, int maxDepth, float spawnChance, float noiseScale)
        {
            oreProp.FindPropertyRelative("OreBlock").objectReferenceValue = oreBlock;
            oreProp.FindPropertyRelative("MinDepth").intValue = minDepth;
            oreProp.FindPropertyRelative("MaxDepth").intValue = maxDepth;
            oreProp.FindPropertyRelative("SpawnChance").floatValue = spawnChance;
            oreProp.FindPropertyRelative("NoiseScale").floatValue = noiseScale;
        }
    }
}
#endif
