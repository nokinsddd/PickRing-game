using UnityEngine;

public enum ZoneType
{
    Success,
    Bad
}

public class Zone : MonoBehaviour
{
    [Header("Position")]
    [SerializeField, Range(0f, 360f)]
    private float angle = 0f;

    [Header("Gameplay")]
    public ZoneType type = ZoneType.Success;

    [Range(0f, 360f)]
    public float arcWidth = 60f;

    [Header("Curved Visual")]
    [SerializeField]
    private Transform visual;

    [SerializeField]
    private string visualName = "ZoneVisual";

    [Tooltip("Радиус кольца, по которому рисуется зона.")]
    [SerializeField]
    private float radius = 3f;

    [Tooltip("Толщина дуги зоны.")]
    [SerializeField]
    private float thickness = 0.35f;

    [Tooltip("Шаг сегмента дуги в градусах. Меньше значение = более гладкая дуга, но больше точек.")]
    [SerializeField, Range(1f, 30f)]
    private float segmentAngle = 6f;

    [SerializeField]
    private int sortingOrder = 5;

    [SerializeField]
    private Material zoneMaterial;

    [SerializeField]
    private Color successColor = new Color(0.24f, 1f, 0.48f, 0.85f);

    [SerializeField]
    private Color badColor = new Color(1f, 0.23f, 0.36f, 0.9f);

    private LineRenderer lineRenderer;

    private Color appliedColor = Color.clear;
    private bool colorApplied;

    public float CenterAngle
    {
        get { return NormalizeAngle(angle); }
    }

    public float HalfWidth
    {
        get { return arcWidth * 0.5f; }
    }

    public bool IsActive
    {
        get { return gameObject.activeSelf; }
    }

    private void Reset()
    {
        angle = NormalizeAngle(transform.localEulerAngles.z);
    }

    private void Awake()
    {
        ApplyAngle();
        RefreshVisual();
    }

    private void OnEnable()
    {
        ApplyAngle();
        RefreshVisual();
    }

    private void OnValidate()
    {
        ApplyAngle();
        RefreshVisual();
    }

    public bool IsHitBy(float pickAngle)
    {
        if (!IsActive)
            return false;

        if (arcWidth <= 0.01f)
            return false;

        float diff = Mathf.DeltaAngle(CenterAngle, pickAngle);

        return Mathf.Abs(diff) <= HalfWidth;
    }

    public void SetAngle(float newAngle)
    {
        angle = NormalizeAngle(newAngle);
        ApplyAngle();
    }

    public void SetArcWidth(float newArcWidth)
    {
        arcWidth = Mathf.Clamp(newArcWidth, 0f, 360f);
        RefreshVisual();
    }

    public void Configure(ZoneType zoneType, float zoneRadius, float zoneArcWidth)
    {
        type = zoneType;
        radius = zoneRadius;
        arcWidth = Mathf.Clamp(zoneArcWidth, 0f, 360f);

        RefreshVisual();
    }

    private void ApplyAngle()
    {
        angle = NormalizeAngle(angle);

        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private void RefreshVisual()
    {
        ResolveVisual();
        ApplyVisual();
    }

    private void ResolveVisual()
    {
        if (visual == null)
        {
            visual = FindChildByName(transform, visualName);
        }

        if (visual == null)
        {
            Debug.LogWarning(
                $"Zone '{name}' не нашёл дочерний объект '{visualName}'. " +
                $"Создай дочерний объект ZoneVisual и добавь на него LineRenderer.",
                this
            );

            return;
        }

        SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (lineRenderer == null)
        {
            lineRenderer = visual.GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            Debug.LogWarning(
                $"У объекта '{visual.name}' нет компонента LineRenderer. " +
                $"Добавь LineRenderer на ZoneVisual, чтобы зона рисовалась дугой.",
                visual
            );
        }
    }

    private void ApplyVisual()
    {
        if (visual == null || lineRenderer == null)
            return;

        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.alignment = LineAlignment.View;

        if (zoneMaterial != null && lineRenderer.sharedMaterial != zoneMaterial)
        {
            lineRenderer.sharedMaterial = zoneMaterial;
            colorApplied = false;
        }

        bool visible = arcWidth > 0.01f;

        lineRenderer.enabled = visible;

        if (!visible)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Color color = type == ZoneType.Success ? successColor : badColor;

        if (!colorApplied || appliedColor != color)
        {
            SetSolidGradient(color);
            appliedColor = color;
            colorApplied = true;
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.alignment = LineAlignment.View;

        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;

        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.widthMultiplier = 1f;

        lineRenderer.sortingOrder = sortingOrder;

        int segments = GetSegmentCount();

        if (segments <= 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = segments + 1;

        float halfWidth = HalfWidth;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float deltaDegrees = Mathf.Lerp(-halfWidth, halfWidth, t);

            Vector3 point = GetArcPoint(deltaDegrees);

            lineRenderer.SetPosition(i, point);
        }
    }

    private void SetSolidGradient(Color color)
    {
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a, 1f)
            }
        );

        lineRenderer.colorGradient = gradient;
    }

    private int GetSegmentCount()
    {
        if (arcWidth <= 0.01f)
            return 0;

        float safeSegmentAngle = Mathf.Max(1f, segmentAngle);

        int segments = Mathf.CeilToInt(arcWidth / safeSegmentAngle);

        return Mathf.Max(2, segments);
    }

    private Vector3 GetArcPoint(float deltaDegrees)
    {
        float rad = deltaDegrees * Mathf.Deg2Rad;

        float x = radius * Mathf.Sin(rad);
        float y = radius * Mathf.Cos(rad);

        return new Vector3(x, y, 0f);
    }

    private float NormalizeAngle(float value)
    {
        value %= 360f;

        if (value < 0f)
            value += 360f;

        return value;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildByName(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (arcWidth <= 0.01f)
            return;

        Gizmos.color = type == ZoneType.Success ? Color.green : Color.red;

        int segments = GetSegmentCount();

        if (segments <= 0)
            return;

        float halfWidth = HalfWidth;

        Vector3 previous = GetArcPoint(-halfWidth);

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float deltaDegrees = Mathf.Lerp(-halfWidth, halfWidth, t);

            Vector3 current = GetArcPoint(deltaDegrees);

            Gizmos.DrawLine(previous, current);

            previous = current;
        }
    }
#endif
}