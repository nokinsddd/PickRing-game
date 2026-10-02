using System;
using UnityEngine;

public class MockPlatformService : IPlatformService
{
    public bool IsInitialized { get; private set; }

    public void Initialize(Action<bool> onComplete)
    {
        IsInitialized = true;
        Debug.Log("[MockPlatform] Initialized");
        onComplete?.Invoke(true);
    }

    public void NotifyGameReady()
    {
        Debug.Log("[MockPlatform] Game Ready");
    }

    public void ShowRewardedAd(Action<bool> onRewardReceived)
    {
        Debug.Log("[MockPlatform] Rewarded ad: success");
        onRewardReceived?.Invoke(true);
    }

    public void ShowInterstitialAd(Action<bool> onClosed)
    {
        Debug.Log("[MockPlatform] Interstitial ad: closed");
        onClosed?.Invoke(true);
    }

    public string GetPlayerLanguage()
    {
        // Позже заменим на реальный язык из SDK Яндекс Игр.
        return "ru";
    }

    public void SaveProgress(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string LoadProgress(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }
}