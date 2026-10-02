using System.Collections.Generic;
using UnityEngine;

public static class LocalizationService
{
    private static string currentLanguage = "ru";

    private static readonly Dictionary<string, Dictionary<string, string>> translations =
        new Dictionary<string, Dictionary<string, string>>
        {
            ["ru"] = new Dictionary<string, string>
            {
                ["menu_title"] = "Ring Lock",
                ["menu_progress"] = "Уровень {0}",
                ["play"] = "Играть",
                ["settings"] = "Настройки",
                ["shop"] = "Магазин",
                ["shop_locked"] = "Магазин откроется после режима очков",
                ["back"] = "Назад",
                ["quit"] = "Выход",
                ["music"] = "Музыка",
                ["sfx"] = "Звуки"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["menu_title"] = "Ring Lock",
                ["menu_progress"] = "Level {0}",
                ["play"] = "Play",
                ["settings"] = "Settings",
                ["shop"] = "Shop",
                ["shop_locked"] = "Shop unlocks after score mode",
                ["back"] = "Back",
                ["quit"] = "Quit",
                ["music"] = "Music",
                ["sfx"] = "SFX"
            }
        };

    public static string CurrentLanguage => currentLanguage;

    public static void SetLanguage(string lang)
    {
        if (string.IsNullOrEmpty(lang))
        {
            currentLanguage = "ru";
            return;
        }

        lang = lang.ToLowerInvariant();

        if (lang.StartsWith("ru"))
        {
            currentLanguage = "ru";
        }
        else if (lang.StartsWith("en"))
        {
            currentLanguage = "en";
        }
        else
        {
            // Для российского рынка fallback делаем на русский.
            currentLanguage = "ru";
        }
    }

    public static string Get(string key)
    {
        if (translations.TryGetValue(currentLanguage, out var dict) &&
            dict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (translations.TryGetValue("ru", out var fallback) &&
            fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}