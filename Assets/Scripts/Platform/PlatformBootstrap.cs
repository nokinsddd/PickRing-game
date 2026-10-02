using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatformBootstrap : MonoBehaviour
{
    public static PlatformBootstrap Instance { get; private set; }

    public static IPlatformService Platform => Instance != null ? Instance.platform : null;

    public static bool IsPlatformReady =>
        Instance != null &&
        Instance.platform != null &&
        Instance.platform.IsInitialized;

    [Header("Settings")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool blockWebGLContextMenu = true;

    private IPlatformService platform;

    private bool initializing;
    private bool initialized;
    private bool gameReadyNotified;

    private readonly List<Action<bool>> initializationCallbacks = new List<Action<bool>>();

    public event Action<bool> OnInitialized;
    public event Action OnGameReadyNotified;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (blockWebGLContextMenu)
        {
            //WebGLContextMenuBlocker.Disable();
        }

        if (autoInitialize)
        {
            Initialize();
        }
    }

    public void Initialize(Action<bool> onComplete = null)
    {
        if (initialized)
        {
            onComplete?.Invoke(true);
            return;
        }

        if (onComplete != null)
        {
            initializationCallbacks.Add(onComplete);
        }

        if (initializing)
        {
            return;
        }

        initializing = true;

        platform = CreatePlatformService();

        if (platform == null)
        {
            initializing = false;
            initialized = false;

            CompleteInitialization(false);
            return;
        }

        platform.Initialize(success =>
        {
            initializing = false;
            initialized = success;

            CompleteInitialization(success);
        });
    }

    public void NotifyGameReady()
    {
        if (gameReadyNotified)
        {
            return;
        }

        if (!initialized)
        {
            Debug.LogWarning("Platform is not initialized yet. Trying to initialize before Game Ready.");

            Initialize(success =>
            {
                if (success)
                {
                    SendGameReady();
                }
                else
                {
                    Debug.LogError("Platform initialization failed. Cannot send Game Ready.");
                }
            });

            return;
        }

        SendGameReady();
    }

    private void SendGameReady()
    {
        if (gameReadyNotified)
        {
            return;
        }

        if (platform == null)
        {
            Debug.LogError("Platform service is null. Cannot send Game Ready.");
            return;
        }

        platform.NotifyGameReady();
        gameReadyNotified = true;
        OnGameReadyNotified?.Invoke();
    }

    private void CompleteInitialization(bool success)
    {
        var callbacks = initializationCallbacks.ToArray();
        initializationCallbacks.Clear();

        foreach (var callback in callbacks)
        {
            callback?.Invoke(success);
        }

        OnInitialized?.Invoke(success);
    }

    private IPlatformService CreatePlatformService()
    {
#if UNITY_EDITOR
        return new MockPlatformService();
#else
        return new YandexPlatformService();
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}