using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Player;

namespace VoxelRPG.UI
{
    /// <summary>
    /// Creates the game UI programmatically at runtime.
    /// Generates all screens (HUD, Pause, Inventory, Crafting, Death) with simple styling.
    /// </summary>
    public class RuntimeUIBuilder : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color _panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color _healthColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _hungerColor = new Color(0.9f, 0.6f, 0.2f);
        [SerializeField] private Color _buttonColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _buttonHoverColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _slotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _slotSelectedColor = new Color(0.8f, 0.8f, 0.2f, 1f);

        private Canvas _canvas;
        private GameObject _hudScreen;
        private GameObject _pauseScreen;
        private GameObject _inventoryScreen;
        private GameObject _craftingScreen;
        private GameObject _characterScreen;
        private GameObject _deathScreen;

        // HUD references
        private Slider _healthSlider;
        private Slider _hungerSlider;
        private TextMeshProUGUI _healthText;
        private TextMeshProUGUI _hungerText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _dayText;
        private HotbarSlotUI[] _hotbarSlots;

        public void BuildUI()
        {
            CreateCanvas();
            CreateHUDScreen();
            CreatePauseScreen();
            CreateInventoryScreen();
            CreateCraftingScreen();
            CreateCharacterScreen();
            CreateDeathScreen();
            WireUIManager();

            Debug.Log("[RuntimeUIBuilder] UI created successfully.");
        }

        private void CreateCanvas()
        {
            var canvasObject = new GameObject("GameUI");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private void CreateHUDScreen()
        {
            _hudScreen = CreateScreen("HUDScreen");

            // Health bar (top left)
            var healthContainer = CreatePanel(_hudScreen.transform, "HealthContainer",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(250, 30));

            _healthSlider = CreateSlider(healthContainer.transform, "HealthSlider", _healthColor);
            _healthText = CreateText(healthContainer.transform, "HealthText", "100/100",
                TextAlignmentOptions.Center, 16);
            _healthText.rectTransform.anchorMin = Vector2.zero;
            _healthText.rectTransform.anchorMax = Vector2.one;
            _healthText.rectTransform.offsetMin = Vector2.zero;
            _healthText.rectTransform.offsetMax = Vector2.zero;

            // Hunger bar (below health)
            var hungerContainer = CreatePanel(_hudScreen.transform, "HungerContainer",
                new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -60), new Vector2(250, 30));

            _hungerSlider = CreateSlider(hungerContainer.transform, "HungerSlider", _hungerColor);
            _hungerText = CreateText(hungerContainer.transform, "HungerText", "100/100",
                TextAlignmentOptions.Center, 16);
            _hungerText.rectTransform.anchorMin = Vector2.zero;
            _hungerText.rectTransform.anchorMax = Vector2.one;
            _hungerText.rectTransform.offsetMin = Vector2.zero;
            _hungerText.rectTransform.offsetMax = Vector2.zero;

            // Time display (top right)
            var timeContainer = CreatePanel(_hudScreen.transform, "TimeContainer",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(150, 50));
            timeContainer.GetComponent<Image>().color = _panelColor;

            _dayText = CreateText(timeContainer.transform, "DayText", "Day 1",
                TextAlignmentOptions.Center, 18);
            _dayText.rectTransform.anchorMin = new Vector2(0, 0.5f);
            _dayText.rectTransform.anchorMax = new Vector2(1, 1);
            _dayText.rectTransform.offsetMin = new Vector2(5, 0);
            _dayText.rectTransform.offsetMax = new Vector2(-5, -5);

            _timeText = CreateText(timeContainer.transform, "TimeText", "06:00",
                TextAlignmentOptions.Center, 24);
            _timeText.rectTransform.anchorMin = new Vector2(0, 0);
            _timeText.rectTransform.anchorMax = new Vector2(1, 0.5f);
            _timeText.rectTransform.offsetMin = new Vector2(5, 5);
            _timeText.rectTransform.offsetMax = new Vector2(-5, 0);

            // Hotbar (bottom center)
            var hotbarContainer = CreatePanel(_hudScreen.transform, "HotbarContainer",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 20), new Vector2(468, 52));
            hotbarContainer.GetComponent<Image>().color = _panelColor;

            var hotbarLayout = hotbarContainer.AddComponent<HorizontalLayoutGroup>();
            hotbarLayout.spacing = 4;
            hotbarLayout.padding = new RectOffset(2, 2, 2, 2);
            hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
            hotbarLayout.childControlWidth = false;
            hotbarLayout.childControlHeight = false;
            hotbarLayout.childForceExpandWidth = false;
            hotbarLayout.childForceExpandHeight = false;

            _hotbarSlots = new HotbarSlotUI[9];
            for (int i = 0; i < 9; i++)
            {
                var slot = CreateHotbarSlot(hotbarContainer.transform, i);
                _hotbarSlots[i] = slot;
            }

            // Add HUDController
            var hudController = _hudScreen.AddComponent<HUDController>();
            SetPrivateField(hudController, "_healthSlider", _healthSlider);
            SetPrivateField(hudController, "_healthText", _healthText);
            SetPrivateField(hudController, "_hungerSlider", _hungerSlider);
            SetPrivateField(hudController, "_hungerText", _hungerText);
            SetPrivateField(hudController, "_timeText", _timeText);
            SetPrivateField(hudController, "_dayText", _dayText);
            SetPrivateField(hudController, "_hotbarSlots", _hotbarSlots);
        }

        private void CreatePauseScreen()
        {
            _pauseScreen = CreateScreen("PauseScreen");
            _pauseScreen.SetActive(false);

            // Dark overlay
            var overlay = _pauseScreen.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.7f);

            // Center panel
            var panel = CreatePanel(_pauseScreen.transform, "PausePanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300, 250));
            panel.GetComponent<Image>().color = _panelColor;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 15;
            panelLayout.padding = new RectOffset(20, 20, 30, 20);
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            var title = CreateText(panel.transform, "Title", "PAUSED",
                TextAlignmentOptions.Center, 32);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            // Resume button
            var resumeBtn = CreateButton(panel.transform, "ResumeButton", "Resume", 40);

            // Quit button
            var quitBtn = CreateButton(panel.transform, "QuitButton", "Quit", 40);

            // Add PauseMenu component
            var pauseMenu = _pauseScreen.AddComponent<PauseMenu>();
            SetPrivateField(pauseMenu, "_resumeButton", resumeBtn);
            SetPrivateField(pauseMenu, "_quitButton", quitBtn);
        }

        private void CreateInventoryScreen()
        {
            _inventoryScreen = CreateScreen("InventoryScreen");
            _inventoryScreen.SetActive(false);

            // Semi-transparent overlay
            var overlay = _inventoryScreen.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

            // Center panel
            var panel = CreatePanel(_inventoryScreen.transform, "InventoryPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 350));
            panel.GetComponent<Image>().color = _panelColor;

            // Title
            var title = CreateText(panel.transform, "Title", "INVENTORY",
                TextAlignmentOptions.Center, 24);
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -10);
            title.rectTransform.sizeDelta = new Vector2(0, 40);

            // Grid container
            var gridContainer = CreatePanel(panel.transform, "GridContainer",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -20), new Vector2(360, 260));
            gridContainer.GetComponent<Image>().color = Color.clear;

            var grid = gridContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(48, 48);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(5, 5, 5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;

            // Create 28 inventory slots (7 columns x 4 rows)
            var inventorySlots = new InventorySlotUI[28];
            for (int i = 0; i < 28; i++)
            {
                var slot = CreateInventorySlot(gridContainer.transform, i);
                inventorySlots[i] = slot;
            }

            // Add InventoryUI component
            var inventoryUI = _inventoryScreen.AddComponent<InventoryUI>();
            SetPrivateField(inventoryUI, "_preCreatedSlots", inventorySlots);
        }

        private void CreateCraftingScreen()
        {
            _craftingScreen = CreateScreen("CraftingScreen");
            _craftingScreen.SetActive(false);

            // Semi-transparent overlay
            var overlay = _craftingScreen.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

            // Center panel
            var panel = CreatePanel(_craftingScreen.transform, "CraftingPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700, 500));
            panel.GetComponent<Image>().color = _panelColor;

            // Title
            var title = CreateText(panel.transform, "Title", "CRAFTING",
                TextAlignmentOptions.Center, 24);
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -10);
            title.rectTransform.sizeDelta = new Vector2(0, 40);

            // Left side: Recipe list
            var listContainer = new GameObject("ListContainer");
            listContainer.transform.SetParent(panel.transform, false);
            var listRect = listContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0.5f, 1);
            listRect.offsetMin = new Vector2(10, 50);
            listRect.offsetMax = new Vector2(-10, -50);
            var listImage = listContainer.AddComponent<Image>();
            listImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Recipe list with scroll rect
            var scrollRectObj = new GameObject("RecipeListScroll");
            scrollRectObj.transform.SetParent(listContainer.transform, false);
            var scrollRect = scrollRectObj.AddComponent<ScrollRect>();
            var scrollRectRect = scrollRectObj.AddComponent<RectTransform>();
            scrollRectRect.anchorMin = Vector2.zero;
            scrollRectRect.anchorMax = Vector2.one;
            scrollRectRect.offsetMin = Vector2.zero;
            scrollRectRect.offsetMax = Vector2.zero;

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollRectObj.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>();

            // Content area for recipes
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = new Vector2(5, 0);
            contentRect.offsetMax = new Vector2(-5, 0);
            contentRect.sizeDelta = new Vector2(0, 0);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var contentFitter = content.AddComponent<LayoutElement>();
            contentFitter.preferredHeight = 200;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            // Right side: Recipe details
            var detailsPanel = new GameObject("DetailsPanel");
            detailsPanel.transform.SetParent(panel.transform, false);
            var detailsRect = detailsPanel.AddComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.5f, 0);
            detailsRect.anchorMax = new Vector2(1, 1);
            detailsRect.offsetMin = new Vector2(10, 50);
            detailsRect.offsetMax = new Vector2(-10, -50);
            var detailsImage = detailsPanel.AddComponent<Image>();
            detailsImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Recipe name
            var recipeName = CreateText(detailsPanel.transform, "RecipeName", "Select a recipe",
                TextAlignmentOptions.TopLeft, 18);
            recipeName.rectTransform.anchorMin = new Vector2(0, 1);
            recipeName.rectTransform.anchorMax = new Vector2(1, 1);
            recipeName.rectTransform.pivot = new Vector2(0, 1);
            recipeName.rectTransform.offsetMin = new Vector2(5, -30);
            recipeName.rectTransform.offsetMax = new Vector2(-5, 0);

            // Result icon and quantity
            var resultContainer = new GameObject("ResultContainer");
            resultContainer.transform.SetParent(detailsPanel.transform, false);
            var resultRect = resultContainer.AddComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0, 0.7f);
            resultRect.anchorMax = new Vector2(0.3f, 1);
            resultRect.offsetMin = new Vector2(5, 5);
            resultRect.offsetMax = new Vector2(-5, -5);
            var resultImage = resultContainer.AddComponent<Image>();
            resultImage.color = Color.clear;

            var resultIcon = new GameObject("ResultIcon");
            resultIcon.transform.SetParent(resultContainer.transform, false);
            var iconRect = resultIcon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = resultIcon.AddComponent<Image>();
            iconImage.color = Color.clear;

            var resultQuantity = CreateText(resultContainer.transform, "ResultQuantity", "x1",
                TextAlignmentOptions.BottomRight, 14);
            resultQuantity.rectTransform.anchorMin = Vector2.one;
            resultQuantity.rectTransform.anchorMax = Vector2.one;
            resultQuantity.rectTransform.pivot = Vector2.one;
            resultQuantity.rectTransform.offsetMin = Vector2.zero;
            resultQuantity.rectTransform.offsetMax = new Vector2(-2, -2);

            // Ingredients list
            var ingredientsLabel = CreateText(detailsPanel.transform, "IngredientsLabel", "Ingredients:",
                TextAlignmentOptions.TopLeft, 14);
            ingredientsLabel.rectTransform.anchorMin = new Vector2(0.3f, 1);
            ingredientsLabel.rectTransform.anchorMax = new Vector2(1, 1);
            ingredientsLabel.rectTransform.pivot = new Vector2(0, 1);
            ingredientsLabel.rectTransform.offsetMin = new Vector2(5, -50);
            ingredientsLabel.rectTransform.offsetMax = new Vector2(-5, -65);

            var ingredientsContainer = new GameObject("IngredientsContainer");
            ingredientsContainer.transform.SetParent(detailsPanel.transform, false);
            var ingredientsRect = ingredientsContainer.AddComponent<RectTransform>();
            ingredientsRect.anchorMin = new Vector2(0.3f, 0.5f);
            ingredientsRect.anchorMax = new Vector2(1, 0.9f);
            ingredientsRect.offsetMin = new Vector2(5, 5);
            ingredientsRect.offsetMax = new Vector2(-5, -5);
            var ingredientsLayout = ingredientsContainer.AddComponent<VerticalLayoutGroup>();
            ingredientsLayout.spacing = 4;
            ingredientsLayout.padding = new RectOffset(2, 2, 2, 2);
            ingredientsLayout.childControlWidth = true;
            ingredientsLayout.childControlHeight = false;
            ingredientsLayout.childForceExpandWidth = true;
            ingredientsLayout.childForceExpandHeight = false;

            // Craft button
            var craftBtn = CreateButton(panel.transform, "CraftButton", "Craft", 40);
            craftBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
            craftBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            craftBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            craftBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
            craftBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            var craftButtonText = craftBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Add CraftingUI component and wire references
            var craftingUI = _craftingScreen.AddComponent<CraftingUI>();
            SetPrivateField(craftingUI, "_recipeListContainer", content);
            SetPrivateField(craftingUI, "_recipeScrollRect", scrollRect);
            SetPrivateField(craftingUI, "_detailsPanel", detailsPanel);
            SetPrivateField(craftingUI, "_recipeName", recipeName);
            SetPrivateField(craftingUI, "_resultIcon", iconImage);
            SetPrivateField(craftingUI, "_resultQuantity", resultQuantity);
            SetPrivateField(craftingUI, "_ingredientsContainer", ingredientsContainer);
            SetPrivateField(craftingUI, "_craftButton", craftBtn);
            SetPrivateField(craftingUI, "_craftButtonText", craftButtonText);
        }

        private void CreateDeathScreen()
        {
            _deathScreen = CreateScreen("DeathScreen");
            _deathScreen.SetActive(false);

            // Red-tinted overlay
            var overlay = _deathScreen.AddComponent<Image>();
            overlay.color = new Color(0.3f, 0, 0, 0.8f);

            // Center panel
            var panel = CreatePanel(_deathScreen.transform, "DeathPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300, 200));
            panel.GetComponent<Image>().color = Color.clear;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 30;
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            var title = CreateText(panel.transform, "Title", "YOU DIED",
                TextAlignmentOptions.Center, 48);
            title.color = Color.red;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;

            // Respawn button
            var respawnBtn = CreateButton(panel.transform, "RespawnButton", "Respawn", 50);
            respawnBtn.GetComponent<LayoutElement>().preferredWidth = 200;

            // Wire respawn button to DeathHandler
            respawnBtn.onClick.AddListener(() =>
            {
                if (ServiceLocator.TryGet<DeathHandler>(out var deathHandler))
                {
                    deathHandler.ForceRespawn();
                }
                if (ServiceLocator.TryGet<UIManager>(out var uiManager))
                {
                    uiManager.ReturnToGameplay();
                }
            });
        }

        private void WireUIManager()
        {
            var uiManager = _canvas.gameObject.AddComponent<UIManager>();

            SetPrivateField(uiManager, "_hudScreen", _hudScreen);
            SetPrivateField(uiManager, "_pauseScreen", _pauseScreen);
            SetPrivateField(uiManager, "_inventoryScreen", _inventoryScreen);
            SetPrivateField(uiManager, "_craftingScreen", _craftingScreen);
            SetPrivateField(uiManager, "_deathScreen", _deathScreen);
        }

        private void CreateCharacterScreen()
        {
            _characterScreen = CreateScreen("CharacterScreen");
            _characterScreen.SetActive(false);

            // Semi-transparent overlay
            var overlay = _characterScreen.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

            // Center panel
            var panel = CreatePanel(_characterScreen.transform, "CharacterPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400, 350));
            panel.GetComponent<Image>().color = _panelColor;

            var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.spacing = 10;
            panelLayout.padding = new RectOffset(20, 20, 20, 20);
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            var title = CreateText(panel.transform, "Title", "CHARACTER",
                TextAlignmentOptions.Center, 24);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            // Stats content area
            var statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(panel.transform, false);
            var statsRect = statsContainer.AddComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero;
            statsRect.anchorMax = Vector2.one;
            var statsLayout = statsContainer.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 8;
            statsLayout.padding = new RectOffset(10, 10, 10, 10);
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = false;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;

            // Health stat
            var healthContainer = new GameObject("HealthStat");
            healthContainer.transform.SetParent(statsContainer.transform, false);
            healthContainer.AddComponent<LayoutElement>().preferredHeight = 30;
            var healthLabel = CreateText(healthContainer.transform, "Label", "Health:",
                TextAlignmentOptions.Left, 14);
            healthLabel.rectTransform.anchorMin = Vector2.zero;
            healthLabel.rectTransform.anchorMax = new Vector2(0.3f, 1);
            var healthValue = CreateText(healthContainer.transform, "Value", "100/100",
                TextAlignmentOptions.Right, 14);
            healthValue.rectTransform.anchorMin = new Vector2(0.3f, 0);
            healthValue.rectTransform.anchorMax = Vector2.one;

            // Hunger stat
            var hungerContainer = new GameObject("HungerStat");
            hungerContainer.transform.SetParent(statsContainer.transform, false);
            hungerContainer.AddComponent<LayoutElement>().preferredHeight = 30;
            var hungerLabel = CreateText(hungerContainer.transform, "Label", "Hunger:",
                TextAlignmentOptions.Left, 14);
            hungerLabel.rectTransform.anchorMin = Vector2.zero;
            hungerLabel.rectTransform.anchorMax = new Vector2(0.3f, 1);
            var hungerValue = CreateText(hungerContainer.transform, "Value", "100/100",
                TextAlignmentOptions.Right, 14);
            hungerValue.rectTransform.anchorMin = new Vector2(0.3f, 0);
            hungerValue.rectTransform.anchorMax = Vector2.one;

            // Inventory count stat
            var inventoryContainer = new GameObject("InventoryStat");
            inventoryContainer.transform.SetParent(statsContainer.transform, false);
            inventoryContainer.AddComponent<LayoutElement>().preferredHeight = 30;
            var inventoryLabel = CreateText(inventoryContainer.transform, "Label", "Inventory:",
                TextAlignmentOptions.Left, 14);
            inventoryLabel.rectTransform.anchorMin = Vector2.zero;
            inventoryLabel.rectTransform.anchorMax = new Vector2(0.3f, 1);
            var inventoryValue = CreateText(inventoryContainer.transform, "Value", "0/28",
                TextAlignmentOptions.Right, 14);
            inventoryValue.rectTransform.anchorMin = new Vector2(0.3f, 0);
            inventoryValue.rectTransform.anchorMax = Vector2.one;

            // Close button
            var closeBtn = CreateButton(panel.transform, "CloseButton", "Close", 40);
            closeBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        }

        #region Helper Methods

        private GameObject CreateScreen(string name)
        {
            var screen = new GameObject(name);
            screen.transform.SetParent(_canvas.transform, false);

            var rect = screen.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return screen;
        }

        private GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = panel.AddComponent<Image>();
            image.color = Color.clear;

            return panel;
        }

        private Slider CreateSlider(Transform parent, string name, Color fillColor)
        {
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            var rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2, 2);
            fillAreaRect.offsetMax = new Vector2(-2, -2);

            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.fillRect = fillRect;
            slider.value = 1f;

            return slider;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            TextAlignmentOptions alignment, float fontSize)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string text, float height)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            var layout = btnObj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            var image = btnObj.AddComponent<Image>();
            image.color = _buttonColor;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = image;

            var colors = btn.colors;
            colors.normalColor = _buttonColor;
            colors.highlightedColor = _buttonHoverColor;
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            btn.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private HotbarSlotUI CreateHotbarSlot(Transform parent, int index)
        {
            var slotObj = new GameObject($"HotbarSlot_{index}");
            slotObj.transform.SetParent(parent, false);

            var rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(48, 48);

            var image = slotObj.AddComponent<Image>();
            image.color = _slotColor;

            var slotUI = slotObj.AddComponent<HotbarSlotUI>();

            // Selection border
            var border = new GameObject("Border");
            border.transform.SetParent(slotObj.transform, false);
            var borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImage = border.AddComponent<Image>();
            borderImage.color = _slotSelectedColor;
            border.SetActive(false);

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(slotObj.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.clear;

            // Amount text
            var amount = new GameObject("Amount");
            amount.transform.SetParent(slotObj.transform, false);
            var amountRect = amount.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0.5f, 0);
            amountRect.anchorMax = new Vector2(1, 0.4f);
            amountRect.offsetMin = Vector2.zero;
            amountRect.offsetMax = Vector2.zero;
            var amountText = amount.AddComponent<TextMeshProUGUI>();
            amountText.text = "";
            amountText.fontSize = 14;
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.color = Color.white;

            // Slot number
            var number = new GameObject("Number");
            number.transform.SetParent(slotObj.transform, false);
            var numberRect = number.AddComponent<RectTransform>();
            numberRect.anchorMin = new Vector2(0, 0.6f);
            numberRect.anchorMax = new Vector2(0.4f, 1);
            numberRect.offsetMin = Vector2.zero;
            numberRect.offsetMax = Vector2.zero;
            var numberText = number.AddComponent<TextMeshProUGUI>();
            numberText.text = (index + 1).ToString();
            numberText.fontSize = 12;
            numberText.alignment = TextAlignmentOptions.TopLeft;
            numberText.color = new Color(0.6f, 0.6f, 0.6f);

            SetPrivateField(slotUI, "_backgroundImage", image);
            SetPrivateField(slotUI, "_selectionHighlight", border);
            SetPrivateField(slotUI, "_iconImage", iconImage);
            SetPrivateField(slotUI, "_quantityText", amountText);
            SetPrivateField(slotUI, "_normalColor", _slotColor);
            SetPrivateField(slotUI, "_selectedColor", _slotSelectedColor);
            SetPrivateField(slotUI, "_slotNumber", index);

            return slotUI;
        }

        private InventorySlotUI CreateInventorySlot(Transform parent, int index)
        {
            var slotObj = new GameObject($"InventorySlot_{index}");
            slotObj.transform.SetParent(parent, false);

            var image = slotObj.AddComponent<Image>();
            image.color = _slotColor;

            var slotUI = slotObj.AddComponent<InventorySlotUI>();

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(slotObj.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.clear;

            // Amount text
            var amount = new GameObject("Amount");
            amount.transform.SetParent(slotObj.transform, false);
            var amountRect = amount.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0.5f, 0);
            amountRect.anchorMax = new Vector2(1, 0.4f);
            amountRect.offsetMin = Vector2.zero;
            amountRect.offsetMax = Vector2.zero;
            var amountText = amount.AddComponent<TextMeshProUGUI>();
            amountText.text = "";
            amountText.fontSize = 14;
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.color = Color.white;

            SetPrivateField(slotUI, "_backgroundImage", image);
            SetPrivateField(slotUI, "_iconImage", iconImage);
            SetPrivateField(slotUI, "_quantityText", amountText);

            return slotUI;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[RuntimeUIBuilder] Field '{fieldName}' not found on {obj.GetType().Name}");
            }
        }

        #endregion
    }
}
