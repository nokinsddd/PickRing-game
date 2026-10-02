using UnityEngine;
using System;

public static class SaveService
{
    private const string SaveKey = "ring_lock_save_v1";

    public static SaveData Load()
    {
        if (PlatformBootstrap.Platform == null)
        {
            Debug.LogWarning("Platform is not initialized yet. Using default save.");
            return new SaveData();
        }

        string json = PlatformBootstrap.Platform.LoadProgress(SaveKey);

        if (string.IsNullOrEmpty(json))
        {
            return new SaveData();
        }

        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Save corrupted: " + e.Message);
            return new SaveData();
        }
    }

    public static void Save(SaveData data)
    {
        data.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonUtility.ToJson(data);

        if (PlatformBootstrap.Platform != null)
        {
            PlatformBootstrap.Platform.SaveProgress(SaveKey, json);
        }
        else
        {
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }
    }
}