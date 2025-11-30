using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using VoxelRPG.Core;

namespace VoxelRPG.UI
{
    /// <summary>
    /// Controller for the pause menu screen.
    /// Handles resume, settings, save, and quit actions.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _settingsBackButton;

        [Header("Audio Settings")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        [SerializeField] private TMP_Dropdown _resolutionDropdown;

        [Header("Gameplay Settings")]
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private Toggle _invertYToggle;
        [SerializeField] private Slider _fovSlider;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject _confirmationDialog;
        [SerializeField] private TextMeshProUGUI _confirmationText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;

        [Header("Status Text")]
        [SerializeField] private TextMeshProUGUI _saveStatusText;

        private UIManager _uiManager;
        private System.Action _pendingAction;

        private void Awake()
        {
            // Setup button listeners
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_saveButton != null) _saveButton.onClick.AddListener(OnSaveClicked);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
            if (_settingsBackButton != null) _settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
            if (_confirmYesButton != null) _confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (_confirmNoButton != null) _confirmNoButton.onClick.AddListener(OnConfirmNo);

            // Setup settings listeners
            if (_masterVolumeSlider != null) _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (_musicVolumeSlider != null) _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            // Hide panels initially
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_confirmationDialog != null) _confirmationDialog.SetActive(false);
        }

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _uiManager);
            LoadCurrentSettings();
            ClearSaveStatus();
        }

        private void LoadCurrentSettings()
        {
            // Load audio settings
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            }

            // Load graphics settings
            if (_qualityDropdown != null)
            {
                _qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = Screen.fullScreen;
            }

            // Load gameplay settings
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
            }
            if (_invertYToggle != null)
            {
                _invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            }
            if (_fovSlider != null)
            {
                _fovSlider.value = PlayerPrefs.GetFloat("FOV", 60f);
            }
        }

        #region Main Menu Actions

        private void OnResumeClicked()
        {
            if (_uiManager != null)
            {
                _uiManager.ClosePauseMenu();
            }
        }

        private void OnSettingsClicked()
        {
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(true);
            }
        }

        private void OnSaveClicked()
        {
            // Try to save
            if (ServiceLocator.TryGet<SaveManager>(out var saveManager))
            {
                bool success = saveManager.SaveGame();
                ShowSaveStatus(success ? "Game Saved!" : "Save Failed!");
            }
            else
            {
                ShowSaveStatus("Save system not available");
            }
        }

        private void OnMainMenuClicked()
        {
            ShowConfirmation("Return to main menu?\nUnsaved progress will be lost.", () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            });
        }

        private void OnQuitClicked()
        {
            ShowConfirmation("Quit to desktop?\nUnsaved progress will be lost.", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        #endregion

        #region Settings Actions

        private void OnSettingsBackClicked()
        {
            SaveSettings();
            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            // Would apply to music audio source
            PlayerPrefs.SetFloat("MusicVolume", value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            // Would apply to SFX audio sources
            PlayerPrefs.SetFloat("SFXVolume", value);
        }

        private void OnMouseSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            // Would apply to camera controller
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        #endregion

        #region Confirmation Dialog

        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            _pendingAction = onConfirm;

            if (_confirmationDialog != null)
            {
                _confirmationDialog.SetActive(true);
            }

            if (_confirmationText != null)
            {
                _confirmationText.text = message;
            }
        }

        private void OnConfirmYes()
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.SetActive(false);
            }

            _pendingAction?.Invoke();
            _pendingAction = null;
        }

        private void OnConfirmNo()
        {
            if (_confirmationDialog != null)
            {
                _confirmationDialog.SetActive(false);
            }

            _pendingAction = null;
        }

        #endregion

        #region Status Messages

        private void ShowSaveStatus(string message)
        {
            if (_saveStatusText != null)
            {
                _saveStatusText.text = message;
                CancelInvoke(nameof(ClearSaveStatus));
                Invoke(nameof(ClearSaveStatus), 3f);
            }
        }

        private void ClearSaveStatus()
        {
            if (_saveStatusText != null)
            {
                _saveStatusText.text = "";
            }
        }

        #endregion
    }
}
