using UnityEngine;

public class PickController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 120f; // degrees per second
    public int direction = 1;  // 1 clockwise, -1 counterclockwise

    [Header("State")]
    public bool isBroken;
    public float angle;

    private float breakTimer;
    private float currentCooldown = 2f;

    private void Update()
    {
        if (isBroken)
        {
            breakTimer -= Time.deltaTime;

            if (breakTimer <= 0f)
            {
                Restore();
            }

            return;
        }

        angle += speed * direction * Time.deltaTime;
        angle %= 360f;

        if (angle < 0f)
        {
            angle += 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void Reverse()
    {
        direction *= -1;
    }

    public void Break(float cooldownSeconds)
    {
        if (isBroken)
            return;

        isBroken = true;
        currentCooldown = cooldownSeconds;
        breakTimer = cooldownSeconds;
    }

    public void Restore()
    {
        isBroken = false;
        breakTimer = 0f;
    }

    public void SetAngle(float newAngle)
    {
        angle = newAngle % 360f;

        if (angle < 0f)
        {
            angle += 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}