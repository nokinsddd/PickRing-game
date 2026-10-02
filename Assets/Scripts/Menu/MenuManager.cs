using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Menu UI")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text progressText;

    [Header("Settings UI")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    [Header("Toast")]
    [SerializeField] private Text toastText;

    private SaveData save;

    private void Awake()
    {
        save = SaveService.Load();
    }

    private void Start()
    {
        ShowMainMenu();
        BindSettings();
        ApplyLocalization();
        UpdateProgressText();

        SubscribeToPlatform();
    }

    private void OnDestroy()
    {
        if (PlatformBootstrap.Instance != null)
        {
            PlatformBootstrap.Instance.OnInitialized -= HandlePlatformInitialized;
        }
    }

    private void SubscribeToPlatform()
    {
        if (PlatformBootstrap.Instance == null)
        {
            Debug.LogError("PlatformBootstrap not found in Menu scene.");
            return;
        }

        PlatformBootstrap.Instance.OnInitialized += HandlePlatformInitialized;

        // Если платформа уже инициализирована до подписки, обрабатываем сразу.
        if (PlatformBootstrap.IsPlatformReady)
        {
            HandlePlatformInitialized(true);
        }
    }

    private void HandlePlatformInitialized(bool success)
    {
        if (!success)
        {
            ShowToast("Платформа недоступна. Игра работает в локальном режиме.");
            Debug.LogError("Platform initialization failed.");
            return;
        }

        ApplyLanguageFromPlatform();

        // Game Ready отправляем только когда меню реально готово.
        PlatformBootstrap.Instance.NotifyGameReady();
    }

    private void ApplyLanguageFromPlatform()
    {
        string language = PlatformBootstrap.Platform?.GetPlayerLanguage();

        LocalizationService.SetLanguage(language);
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        if (titleText != null)
        {
            titleText.text = LocalizationService.Get("menu_title");
        }

        if (PlayButtonText() != null)
        {
            PlayButtonText().text = LocalizationService.Get("play");
        }

        UpdateProgressText();
    }

    private Text PlayButtonText()
    {
        // Можно заменить на прямую ссылку в Inspector, если хочешь.
        return null;
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = LocalizationService.Format("menu_progress", save.currentLevel);
        }
    }

    private void BindSettings()
    {
        if (musicToggle != null)
        {
            musicToggle.isOn = save.musicEnabled;
            musicToggle.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sfxToggle != null)
        {
            sfxToggle.isOn = save.sfxEnabled;
            sfxToggle.onValueChanged.AddListener(OnSfxChanged);
        }
    }

    private void OnMusicChanged(bool value)
    {
        save.musicEnabled = value;
        SaveService.Save(save);

        // Временное решение до нормального AudioManager.
        AudioListener.volume = value ? 1f : 0f;
    }

    private void OnSfxChanged(bool value)
    {
        save.sfxEnabled = value;
        SaveService.Save(save);

        // Позже здесь будем включать/выключать игровые эффекты.
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("Game");
    }

    public void OnSettingsClicked()
    {
        mainMenuPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
    }

    public void OnBackClicked()
    {
        settingsPanel?.SetActive(false);
        mainMenuPanel?.SetActive(true);
    }

    public void OnShopClicked()
    {
        ShowToast(LocalizationService.Get("shop_locked"));
    }

    public void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        settingsPanel?.SetActive(false);
    }

    private void ShowToast(string message)
    {
        if (toastText == null)
        {
            Debug.Log(message);
            return;
        }

        toastText.text = message;
        toastText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), 2f);
    }

    private void HideToast()
    {
        if (toastText != null)
        {
            toastText.gameObject.SetActive(false);
        }
    }
}