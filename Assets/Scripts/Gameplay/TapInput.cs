using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TapInput : MonoBehaviour
{
    public event Action OnTap;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUI(touch.fingerId))
                    return;

                OnTap?.Invoke();
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI(-1))
                return;

            OnTap?.Invoke();
        }
#endif
    }

    private bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null)
            return false;

#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        return EventSystem.current.IsPointerOverGameObject(fingerId);
#endif
    }
}