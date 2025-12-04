#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.Core.Crafting;
using VoxelRPG.Combat;

namespace VoxelRPG.Editor
{
    /// <summary>
    /// Editor utility to create all Phase 1 assets:
    /// - Item definitions (resources, tools, food)
    /// - ItemRegistry
    /// - Recipes
    /// - RecipeRegistry
    /// - Zombie monster prefab
    /// </summary>
    public static class Phase1AssetCreator
    {
        private const string ITEMS_PATH = "Assets/_Project/ScriptableObjects/Items";
        private const string RECIPES_PATH = "Assets/_Project/ScriptableObjects/Recipes";
        private const string MONSTERS_PATH = "Assets/_Project/ScriptableObjects/Monsters";
        private const string PREFABS_PATH = "Assets/_Project/Prefabs/Monsters";

        [MenuItem("VoxelRPG/Phase 1/Create All Phase 1 Assets")]
        public static void CreateAllPhase1Assets()
        {
            CreateDirectories();
            CreateItemDefinitions();
            CreateItemRegistry();
            CreateRecipes();
            CreateRecipeRegistry();
            CreateZombiePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase1AssetCreator] All Phase 1 assets created successfully!");
        }

        [MenuItem("VoxelRPG/Phase 1/Create Items Only")]
        public static void CreateItemsOnly()
        {
            CreateDirectories();
            CreateItemDefinitions();
            CreateItemRegistry();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase1AssetCreator] Items and ItemRegistry created successfully!");
        }

        [MenuItem("VoxelRPG/Phase 1/Create Recipes Only")]
        public static void CreateRecipesOnly()
        {
            CreateDirectories();
            CreateRecipes();
            CreateRecipeRegistry();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase1AssetCreator] Recipes and RecipeRegistry created successfully!");
        }

        [MenuItem("VoxelRPG/Phase 1/Create Zombie Prefab Only")]
        public static void CreateZombiePrefabOnly()
        {
            CreateDirectories();
            CreateZombiePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase1AssetCreator] Zombie prefab created successfully!");
        }

        private static void CreateDirectories()
        {
            EnsureFolderExists("Assets/_Project");
            EnsureFolderExists("Assets/_Project/ScriptableObjects");
            EnsureFolderExists(ITEMS_PATH);
            EnsureFolderExists($"{ITEMS_PATH}/Resources");
            EnsureFolderExists($"{ITEMS_PATH}/Tools");
            EnsureFolderExists($"{ITEMS_PATH}/Food");
            EnsureFolderExists(RECIPES_PATH);
            EnsureFolderExists(MONSTERS_PATH);
            EnsureFolderExists("Assets/_Project/Prefabs");
            EnsureFolderExists(PREFABS_PATH);
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var newPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = newPath;
            }
        }

        #region Item Definitions

        private static void CreateItemDefinitions()
        {
            // Resources
            CreateItem("Wood", "Resources", ItemCategory.Resource, 64,
                "Raw wood harvested from trees. Used for crafting.", false, false, 1);
            CreateItem("Stone", "Resources", ItemCategory.Resource, 64,
                "Rough stone mined from the earth. Used for tools and building.", false, false, 2);
            CreateItem("Coal", "Resources", ItemCategory.Resource, 64,
                "Combustible mineral. Used as fuel for smelting.", false, false, 3);
            CreateItem("IronOre", "Resources", ItemCategory.Resource, 64,
                "Raw iron ore. Smelt with coal to create iron ingots.", false, false, 5);
            CreateItem("IronIngot", "Resources", ItemCategory.Resource, 64,
                "Refined iron. Used for crafting iron tools and equipment.", false, false, 10);
            CreateItem("Planks", "Resources", ItemCategory.Resource, 64,
                "Processed wood planks. Used in many crafting recipes.", false, false, 2);
            CreateItem("Stick", "Resources", ItemCategory.Resource, 64,
                "Simple wooden stick. Used for tool handles.", false, false, 1);
            CreateItem("Dirt", "Resources", ItemCategory.Resource, 64,
                "Common soil. Can be placed for building.", false, true, 1);

            // Tools - Wood tier
            CreateItem("WoodPickaxe", "Tools", ItemCategory.Tool, 1,
                "A basic pickaxe made of wood. Breaks stone slowly.", true, false, 5);
            CreateItem("WoodAxe", "Tools", ItemCategory.Tool, 1,
                "A basic axe made of wood. Chops trees slowly.", true, false, 5);

            // Tools - Stone tier
            CreateItem("StonePickaxe", "Tools", ItemCategory.Tool, 1,
                "A sturdy pickaxe made of stone. Better than wood.", true, false, 10);
            CreateItem("StoneAxe", "Tools", ItemCategory.Tool, 1,
                "A sturdy axe made of stone. Better than wood.", true, false, 10);

            // Tools - Iron tier (IronPickaxe already exists, create IronAxe)
            CreateItem("IronAxe", "Tools", ItemCategory.Tool, 1,
                "A strong axe made of iron. Cuts trees efficiently.", true, false, 25);

            // Food
            CreateItem("Apple", "Food", ItemCategory.Food, 16,
                "A fresh apple. Restores 10 hunger when eaten.", false, false, 2, true);
            CreateItem("CookedMeat", "Food", ItemCategory.Food, 16,
                "Cooked meat. Restores 25 hunger when eaten.", false, false, 5, true);
            CreateItem("RawMeat", "Food", ItemCategory.Food, 16,
                "Raw meat from animals. Should be cooked before eating.", false, false, 2, true);

            Debug.Log("[Phase1AssetCreator] Created item definitions");
        }

        private static ItemDefinition CreateItem(string name, string subfolder, ItemCategory category,
            int maxStack, string description, bool isEquippable, bool isPlaceable, int baseValue,
            bool isConsumable = false)
        {
            string path = $"{ITEMS_PATH}/{subfolder}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                Debug.Log($"[Phase1AssetCreator] Item '{name}' already exists, skipping.");
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();

            var serializedObject = new SerializedObject(item);
            serializedObject.FindProperty("_id").stringValue = name.ToLowerInvariant();
            serializedObject.FindProperty("_displayName").stringValue = FormatDisplayName(name);
            serializedObject.FindProperty("_description").stringValue = description;
            serializedObject.FindProperty("_maxStackSize").intValue = maxStack;
            serializedObject.FindProperty("_category").enumValueIndex = (int)category;
            serializedObject.FindProperty("_isConsumable").boolValue = isConsumable;
            serializedObject.FindProperty("_isEquippable").boolValue = isEquippable;
            serializedObject.FindProperty("_isPlaceable").boolValue = isPlaceable;
            serializedObject.FindProperty("_baseValue").intValue = baseValue;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"[Phase1AssetCreator] Created item: {name}");

            return item;
        }

        private static string FormatDisplayName(string name)
        {
            // Insert space before capital letters (except first)
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]))
                {
                    result.Append(' ');
                }
                result.Append(name[i]);
            }
            return result.ToString();
        }

        private static void CreateItemRegistry()
        {
            string path = $"{ITEMS_PATH}/ItemRegistry.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);
            if (existing != null)
            {
                Debug.Log("[Phase1AssetCreator] ItemRegistry already exists, updating...");
            }

            var registry = existing ?? ScriptableObject.CreateInstance<ItemRegistry>();

            // Load all item definitions
            var items = new System.Collections.Generic.List<ItemDefinition>();

            // Resources
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Wood.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Stone.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Coal.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/IronOre.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/IronIngot.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Planks.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Stick.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Resources/Dirt.asset");

            // Tools
            AddItemIfExists(items, $"{ITEMS_PATH}/Tools/WoodPickaxe.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Tools/WoodAxe.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Tools/StonePickaxe.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Tools/StoneAxe.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/IronPickaxe.asset"); // Already exists in root
            AddItemIfExists(items, $"{ITEMS_PATH}/Tools/IronAxe.asset");

            // Food
            AddItemIfExists(items, $"{ITEMS_PATH}/Food/Apple.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Food/CookedMeat.asset");
            AddItemIfExists(items, $"{ITEMS_PATH}/Food/RawMeat.asset");

            var serializedObject = new SerializedObject(registry);
            var itemsProp = serializedObject.FindProperty("_items");
            itemsProp.arraySize = items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(registry, path);
            }

            Debug.Log($"[Phase1AssetCreator] ItemRegistry created/updated with {items.Count} items");
        }

        private static void AddItemIfExists(System.Collections.Generic.List<ItemDefinition> items, string path)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
            {
                items.Add(item);
            }
        }

        #endregion

        #region Recipes

        private static void CreateRecipes()
        {
            // Load items for recipe creation
            var wood = LoadItem("Resources/Wood");
            var stone = LoadItem("Resources/Stone");
            var coal = LoadItem("Resources/Coal");
            var ironOre = LoadItem("Resources/IronOre");
            var ironIngot = LoadItem("Resources/IronIngot");
            var planks = LoadItem("Resources/Planks");
            var stick = LoadItem("Resources/Stick");

            var woodPickaxe = LoadItem("Tools/WoodPickaxe");
            var woodAxe = LoadItem("Tools/WoodAxe");
            var stonePickaxe = LoadItem("Tools/StonePickaxe");
            var stoneAxe = LoadItem("Tools/StoneAxe");
            var ironPickaxe = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ITEMS_PATH}/IronPickaxe.asset");
            var ironAxe = LoadItem("Tools/IronAxe");

            if (wood == null || planks == null || stick == null)
            {
                Debug.LogError("[Phase1AssetCreator] Required items not found! Run 'Create Items Only' first.");
                return;
            }

            // Basic processing recipes
            CreateRecipe("Planks", new[] { (wood, 1) }, (planks, 4),
                "Process wood into planks");
            CreateRecipe("Stick", new[] { (planks, 2) }, (stick, 4),
                "Create sticks from planks");

            // Smelting
            if (ironOre != null && coal != null && ironIngot != null)
            {
                CreateRecipe("IronIngot", new[] { (ironOre, 1), (coal, 1) }, (ironIngot, 1),
                    "Smelt iron ore into an ingot");
            }

            // Wood tools
            if (woodPickaxe != null)
            {
                CreateRecipe("WoodPickaxe", new[] { (planks, 3), (stick, 2) }, (woodPickaxe, 1),
                    "Craft a basic wooden pickaxe");
            }
            if (woodAxe != null)
            {
                CreateRecipe("WoodAxe", new[] { (planks, 3), (stick, 2) }, (woodAxe, 1),
                    "Craft a basic wooden axe");
            }

            // Stone tools
            if (stone != null && stonePickaxe != null)
            {
                CreateRecipe("StonePickaxe", new[] { (stone, 3), (stick, 2) }, (stonePickaxe, 1),
                    "Craft a sturdy stone pickaxe");
            }
            if (stone != null && stoneAxe != null)
            {
                CreateRecipe("StoneAxe", new[] { (stone, 3), (stick, 2) }, (stoneAxe, 1),
                    "Craft a sturdy stone axe");
            }

            // Iron tools
            if (ironIngot != null && ironPickaxe != null)
            {
                CreateRecipe("IronPickaxe", new[] { (ironIngot, 3), (stick, 2) }, (ironPickaxe, 1),
                    "Craft a strong iron pickaxe");
            }
            if (ironIngot != null && ironAxe != null)
            {
                CreateRecipe("IronAxe", new[] { (ironIngot, 3), (stick, 2) }, (ironAxe, 1),
                    "Craft a strong iron axe");
            }

            Debug.Log("[Phase1AssetCreator] Created recipes");
        }

        private static ItemDefinition LoadItem(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ITEMS_PATH}/{relativePath}.asset");
        }

        private static Recipe CreateRecipe(string name,
            (ItemDefinition item, int amount)[] ingredients,
            (ItemDefinition item, int amount) result,
            string description)
        {
            string path = $"{RECIPES_PATH}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Recipe>(path);
            if (existing != null)
            {
                Debug.Log($"[Phase1AssetCreator] Recipe '{name}' already exists, skipping.");
                return existing;
            }

            var recipe = ScriptableObject.CreateInstance<Recipe>();

            var serializedObject = new SerializedObject(recipe);
            serializedObject.FindProperty("_id").stringValue = name.ToLowerInvariant();
            serializedObject.FindProperty("_displayName").stringValue = FormatDisplayName(name);

            // Set ingredients
            var ingredientsProp = serializedObject.FindProperty("_ingredients");
            ingredientsProp.arraySize = ingredients.Length;

            for (int i = 0; i < ingredients.Length; i++)
            {
                var ingredientProp = ingredientsProp.GetArrayElementAtIndex(i);
                ingredientProp.FindPropertyRelative("Item").objectReferenceValue = ingredients[i].item;
                ingredientProp.FindPropertyRelative("Amount").intValue = ingredients[i].amount;
            }

            // Set output (Recipe uses _outputItem and _outputAmount, not nested _result)
            serializedObject.FindProperty("_outputItem").objectReferenceValue = result.item;
            serializedObject.FindProperty("_outputAmount").intValue = result.amount;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(recipe, path);
            Debug.Log($"[Phase1AssetCreator] Created recipe: {name}");

            return recipe;
        }

        private static void CreateRecipeRegistry()
        {
            string path = $"{RECIPES_PATH}/RecipeRegistry.asset";

            var existing = AssetDatabase.LoadAssetAtPath<RecipeRegistry>(path);
            if (existing != null)
            {
                Debug.Log("[Phase1AssetCreator] RecipeRegistry already exists, updating...");
            }

            var registry = existing ?? ScriptableObject.CreateInstance<RecipeRegistry>();

            // Find all recipes in the folder
            var guids = AssetDatabase.FindAssets("t:Recipe", new[] { RECIPES_PATH });
            var recipes = new System.Collections.Generic.List<Recipe>();

            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var recipe = AssetDatabase.LoadAssetAtPath<Recipe>(assetPath);
                if (recipe != null && !assetPath.Contains("RecipeRegistry"))
                {
                    recipes.Add(recipe);
                }
            }

            var serializedObject = new SerializedObject(registry);
            var recipesProp = serializedObject.FindProperty("_recipes");
            recipesProp.arraySize = recipes.Count;

            for (int i = 0; i < recipes.Count; i++)
            {
                recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(registry, path);
            }

            Debug.Log($"[Phase1AssetCreator] RecipeRegistry created/updated with {recipes.Count} recipes");
        }

        #endregion

        #region Zombie Prefab

        private static void CreateZombiePrefab()
        {
            string prefabPath = $"{PREFABS_PATH}/Zombie.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                Debug.Log("[Phase1AssetCreator] Zombie prefab already exists, skipping.");
                return;
            }

            // Create the zombie game object
            var zombieGO = new GameObject("Zombie");

            // Add capsule mesh for visual representation
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Model";
            capsule.transform.SetParent(zombieGO.transform);
            capsule.transform.localPosition = Vector3.zero;

            // Set material color to dark red/brown for zombie appearance
            var renderer = capsule.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.4f, 0.25f, 0.25f); // Dark reddish-brown
                renderer.material = material;

                // Save material as asset
                string materialPath = $"{PREFABS_PATH}/ZombieMaterial.mat";
                if (!AssetDatabase.LoadAssetAtPath<Material>(materialPath))
                {
                    AssetDatabase.CreateAsset(material, materialPath);
                }
            }

            // Remove collider from visual mesh (we'll add CharacterController instead)
            var capsuleCollider = capsule.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                Object.DestroyImmediate(capsuleCollider);
            }

            // Add CharacterController for movement
            var characterController = zombieGO.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 1f, 0);

            // Add NavMeshAgent for AI pathfinding
            var navAgent = zombieGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.speed = 4f; // Chase speed from Zombie.asset
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = 1.5f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;

            // Add BasicMonsterAI component
            zombieGO.AddComponent<BasicMonsterAI>();

            // Add MonsterHealth component
            zombieGO.AddComponent<MonsterHealth>();

            // Load and assign MonsterDefinition
            var zombieDefinition = AssetDatabase.LoadAssetAtPath<MonsterDefinition>(
                $"{MONSTERS_PATH}/Zombie.asset");

            if (zombieDefinition != null)
            {
                // Wire up the definition via serialized property
                var ai = zombieGO.GetComponent<BasicMonsterAI>();
                var serializedAI = new SerializedObject(ai);
                var definitionProp = serializedAI.FindProperty("_definition");
                if (definitionProp != null)
                {
                    definitionProp.objectReferenceValue = zombieDefinition;
                    serializedAI.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Set layer (use default if Monster layer doesn't exist)
            int monsterLayer = LayerMask.NameToLayer("Monster");
            zombieGO.layer = monsterLayer >= 0 ? monsterLayer : LayerMask.NameToLayer("Default");

            // Try to set tag - use Untagged if Monster tag doesn't exist
            // Note: In a real project, ensure Monster tag exists in Tags & Layers
            zombieGO.tag = "Untagged"; // Safe default; configure in Unity editor

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(zombieGO, prefabPath);

            // Clean up scene object
            Object.DestroyImmediate(zombieGO);

            Debug.Log("[Phase1AssetCreator] Created Zombie prefab");

            // Update MonsterDefinition with prefab reference
            if (zombieDefinition != null)
            {
                var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var serializedDef = new SerializedObject(zombieDefinition);
                var prefabProp = serializedDef.FindProperty("_prefab");
                if (prefabProp != null)
                {
                    prefabProp.objectReferenceValue = loadedPrefab;
                    serializedDef.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[Phase1AssetCreator] Linked Zombie prefab to MonsterDefinition");
                }
            }
        }

        #endregion
    }
}
#endif
