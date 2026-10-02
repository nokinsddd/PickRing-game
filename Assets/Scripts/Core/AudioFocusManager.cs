using UnityEngine;

public class AudioFocusManager : MonoBehaviour
{
    public static AudioFocusManager Instance { get; private set; }

    private bool pausedByAd;
    private bool pausedByFocus;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PauseForAd()
    {
        pausedByAd = true;
        AudioListener.pause = true;
    }

    public void ResumeAfterAd()
    {
        pausedByAd = false;

        if (!pausedByFocus)
        {
            AudioListener.pause = false;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            pausedByFocus = true;
            AudioListener.pause = true;
        }
        else
        {
            pausedByFocus = false;

            if (!pausedByAd)
            {
                AudioListener.pause = false;
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            pausedByFocus = true;
            AudioListener.pause = true;
        }
        else
        {
            pausedByFocus = false;

            if (!pausedByAd)
            {
                AudioListener.pause = false;
            }
        }
    }
}