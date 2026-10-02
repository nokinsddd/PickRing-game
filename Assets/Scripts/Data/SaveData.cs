using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public long updatedAt;

    public int coins;
    public int currentLevel = 1;
    public int bestScore;

    public int cooldownUpgradeLevel; // 0, 1, 2

    public bool tutorialCompleted;
    public bool musicEnabled = true;
    public bool sfxEnabled = true;

    public float GetPickCooldownSeconds()
    {
        switch (cooldownUpgradeLevel)
        {
            case 1:
                return 1.5f;
            case 2:
                return 1.0f;
            default:
                return 2.0f;
        }
    }
}