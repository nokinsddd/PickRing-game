using System;

public interface IPlatformService
{
    bool IsInitialized { get; }

    void Initialize(Action<bool> onComplete);

    void NotifyGameReady();

    void ShowRewardedAd(Action<bool> onRewardReceived);

    void ShowInterstitialAd(Action<bool> onClosed);

    string GetPlayerLanguage();

    void SaveProgress(string key, string value);

    string LoadProgress(string key);
}