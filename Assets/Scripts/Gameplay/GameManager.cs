using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PickController pick;
    [SerializeField] private TapInput tapInput;

    [Tooltip("Центр кольца. Зоны будут спавниться как дети этого объекта.")]
    [SerializeField] private Transform ringCenter;

    [Tooltip("Визуальное кольцо. От него будет получен радиус.")]
    [SerializeField] private Transform ringVisual;

    [Header("Zone Prefabs")]
    [SerializeField] private GameObject successZonePrefab;
    [SerializeField] private GameObject badZonePrefab;

    [Header("Zone Limits")]
    [SerializeField] private int maxSuccessZones = 2;
    [SerializeField] private int maxBadZones = 2;

    [SerializeField] private int startingSuccessZones = 2;
    [SerializeField] private int startingBadZones = 2;

    [Header("Success Zone Random Parameters")]
    [SerializeField] private float minSuccessArcWidth = 45f;
    [SerializeField] private float maxSuccessArcWidth = 75f;

    [Header("Bad Zone Random Parameters")]
    [SerializeField] private float minBadArcWidth = 25f;
    [SerializeField] private float maxBadArcWidth = 45f;

    [SerializeField] private float badZoneLifetime = 5f;
    [SerializeField] private float badZoneRespawnDelay = 3f;

    [Header("Dynamic Zone Behavior")]
    [SerializeField] private bool successShrinksToZero = true;

    [Tooltip("На сколько градусов в секунду сужается успешная зона.")]
    [SerializeField] private float successShrinkSpeed = 10f;

    [Tooltip("Когда ширина успешной зоны становится меньше или равной этому значению, она исчезает и спавнится новая.")]
    [SerializeField] private float successDisappearWidth = 0.5f;

    [SerializeField] private bool badGrowsUntilSuccessIntersection = true;

    [Tooltip("На сколько градусов в секунду растёт плохая зона.")]
    [SerializeField] private float badGrowSpeed = 8f;

    [Tooltip("Максимальная ширина плохой зоны, если рядом нет успешных зон.")]
    [SerializeField] private float badMaxArcWidth = 170f;

    [Tooltip("Минимальный зазор, чтобы плохая зона не влезала в успешную вплотную.")]
    [SerializeField] private float zoneTouchSafetyGap = 0.5f;

    [Tooltip("Нужно ли плохим зонам исчезать по таймеру. Для новой механики обычно лучше false.")]
    [SerializeField] private bool badZonesExpire = false;

    [Header("Spawn Safety")]
    [Tooltip("Минимальное расстояние от спавна зоны до текущей отмычки в градусах.")]
    [SerializeField] private float minDistanceFromPickDegrees = 20f;

    [Tooltip("Если не удалось получить радиус из RingVisual, использовать этот.")]
    [SerializeField] private float fallbackRadius = 3f;

    [Tooltip("Минимальный зазор между зонами в градусах. 0 = зоны не пересекаются, но могут касаться.")]
    [SerializeField] private float minZoneGapDegrees = 15f;

    [Tooltip("Сколько раз пробовать найти свободный угол.")]
    [SerializeField] private int spawnAttempts = 120;

    [Tooltip("Если свободное место не найдено, через сколько секунд попробовать заспавнить зону снова.")]
    [SerializeField] private float spawnRetryDelay = 0.2f;

    [Header("UI")]
    [SerializeField] private Text timerText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text coinsText;

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitleText;
    [SerializeField] private Text resultRewardText;

    [Header("Level")]
    [SerializeField] private int targetSuccess = 10;
    [SerializeField] private float levelDuration = 30f;

    [Header("Balance")]
    [SerializeField] private int baseCoins = 10;
    [SerializeField] private int brokenPickPenalty = 3;


    private SaveData save;

    private GameState state;
    private float timeLeft;
    private int successCount;
    private int brokenPicks;

    private float ringRadius;

    private bool successSpawnScheduled;
    private bool badSpawnScheduled;

    private readonly List<Zone> activeSuccessZones = new List<Zone>();
    private readonly List<Zone> activeBadZones = new List<Zone>();

    private readonly Queue<Zone> successPool = new Queue<Zone>();
    private readonly Queue<Zone> badPool = new Queue<Zone>();

    private enum GameState
    {
        Playing,
        Win,
        Lose
    }

    private void Awake()
    {
        save = SaveService.Load();
    }

    private void Start()
    {
        if (tapInput != null)
        {
            tapInput.OnTap += HandleTap;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        ringRadius = ResolveRingRadius();

        StartLevel();

        if (PlatformBootstrap.Instance != null)
        {
            PlatformBootstrap.Instance.NotifyGameReady();
        }
    }

    private void OnDestroy()
    {
        if (tapInput != null)
        {
            tapInput.OnTap -= HandleTap;
        }
    }

   private void Update()
    {
        if (state != GameState.Playing)
            return;

        UpdateDynamicZones(Time.deltaTime);

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            Lose();
        }

        UpdateHUD();
    }

    public void StartLevel()
    {
        StopAllCoroutines();
        ClearAllZones();
        
        successSpawnScheduled = false;
        badSpawnScheduled = false;

        state = GameState.Playing;
        timeLeft = levelDuration;
        successCount = 0;
        brokenPicks = 0;

        ringRadius = ResolveRingRadius();

        if (pick != null)
        {
            pick.Restore();
            pick.direction = 1;
            pick.SetAngle(0f);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SpawnInitialZones();
        UpdateHUD();
    }

    public void Restart()
    {
        StartLevel();
    }

    private void SpawnInitialZones()
    {
        int successToSpawn = Mathf.Clamp(startingSuccessZones, 0, maxSuccessZones);
        int badToSpawn = Mathf.Clamp(startingBadZones, 0, maxBadZones);

        for (int i = 0; i < successToSpawn; i++)
        {
            SpawnSuccessZone();
        }

        for (int i = 0; i < badToSpawn; i++)
        {
            SpawnBadZone();
        }
    }

    private void HandleTap()
    {
        if (state != GameState.Playing)
            return;

        if (pick == null || pick.isBroken)
            return;

        Zone hitZone = GetHitZone();

        if (hitZone == null)
        {
            Miss();
            return;
        }

        if (hitZone.type == ZoneType.Bad)
        {
            HitBadZone(hitZone);
            return;
        }

        HitSuccessZone(hitZone);
    }

    private Zone GetHitZone()
    {
        if (pick == null)
            return null;

        // Сначала проверяем плохие зоны.
        // Если плохая и успешная зона случайно пересеклись, плохая имеет приоритет.
        for (int i = 0; i < activeBadZones.Count; i++)
        {
            Zone zone = activeBadZones[i];

            if (zone != null && zone.IsHitBy(pick.angle))
            {
                return zone;
            }
        }

        for (int i = 0; i < activeSuccessZones.Count; i++)
        {
            Zone zone = activeSuccessZones[i];

            if (zone != null && zone.IsHitBy(pick.angle))
            {
                return zone;
            }
        }

        return null;
    }

    private void HitSuccessZone(Zone zone)
    {
        successCount++;
        pick.Reverse();

        RemoveZone(zone);

        if (successCount >= targetSuccess)
        {
            Win();
            return;
        }

        // Успешная зона появляется сразу после успешного нажатия.
        SpawnSuccessZone();
    }

    private void HitBadZone(Zone zone)
    {
        successCount = Mathf.Max(0, successCount - 1);

        BreakPick();

        RemoveZone(zone);

        // Плохая зона исчезает после попадания и появляется позже.
        StartCoroutine(ScheduleBadZoneRespawn());
    }

    private void Miss()
    {
        BreakPick();
    }

    private void BreakPick()
    {
        if (pick == null || pick.isBroken)
            return;

        brokenPicks++;

        float cooldown = save != null
            ? save.GetPickCooldownSeconds()
            : 2f;

        pick.Break(cooldown);
    }

    private void SpawnSuccessZone()
    {
        if (state != GameState.Playing)
            return;

        if (activeSuccessZones.Count >= maxSuccessZones)
            return;

        if (!TryGetValidSuccessZoneAngle(out float angle, out float width))
        {
            ScheduleSuccessZoneRetry();
            return;
        }

        Zone zone = GetZoneFromPool(isBad: false);

        if (zone == null)
            return;

        zone.Configure(ZoneType.Success, ringRadius, width);
        zone.SetAngle(angle);

        activeSuccessZones.Add(zone);
    }

    private void SpawnBadZone()
    {
        if (state != GameState.Playing)
            return;

        if (activeBadZones.Count >= maxBadZones)
            return;

        if (!TryGetValidBadZoneAngle(out float angle, out float width))
        {
            ScheduleBadZoneRetry();
            return;
        }

        Zone zone = GetZoneFromPool(isBad: true);

        if (zone == null)
            return;

        zone.Configure(ZoneType.Bad, ringRadius, width);
        zone.SetAngle(angle);

        activeBadZones.Add(zone);

        if (badZonesExpire)
        {
            StartCoroutine(BadZoneLifecycle(zone, badZoneLifetime));
        }
    }
    private void ScheduleSuccessZoneRetry()
    {
        if (successSpawnScheduled)
            return;

        successSpawnScheduled = true;
        StartCoroutine(RetrySpawnSuccessZoneRoutine());
    }

    private void ScheduleBadZoneRetry()
    {
        if (badSpawnScheduled)
            return;

        badSpawnScheduled = true;
        StartCoroutine(RetrySpawnBadZoneRoutine());
    }

    private IEnumerator RetrySpawnSuccessZoneRoutine()
    {
        yield return new WaitForSeconds(spawnRetryDelay);

        successSpawnScheduled = false;

        if (state == GameState.Playing)
        {
            SpawnSuccessZone();
        }
    }

    private IEnumerator RetrySpawnBadZoneRoutine()
    {
        yield return new WaitForSeconds(spawnRetryDelay);

        badSpawnScheduled = false;

        if (state == GameState.Playing)
        {
            SpawnBadZone();
        }
    }

    private Zone GetZoneFromPool(bool isBad)
    {
        GameObject prefab = isBad ? badZonePrefab : successZonePrefab;
        Queue<Zone> pool = isBad ? badPool : successPool;

        if (prefab == null)
        {
            Debug.LogError($"Prefab for {(isBad ? "bad" : "success")} zone is not assigned.");
            return null;
        }

        if (ringCenter == null)
        {
            Debug.LogError("RingCenter is not assigned in GameManager.");
            return null;
        }

        Zone zone = null;

        if (pool.Count > 0)
        {
            zone = pool.Dequeue();

            if (zone == null || zone.gameObject == null)
            {
                zone = null;
            }
        }

        if (zone == null)
        {
            GameObject instance = Instantiate(prefab, ringCenter);

            zone = instance.GetComponent<Zone>();

            if (zone == null)
            {
                zone = instance.GetComponentInChildren<Zone>(true);
            }

            if (zone == null)
            {
                Debug.LogError($"Prefab '{prefab.name}' does not contain Zone component.");
                Destroy(instance);
                return null;
            }
        }

        zone.gameObject.SetActive(true);
        zone.transform.SetParent(ringCenter, false);
        zone.transform.localPosition = Vector3.zero;
        zone.transform.localScale = Vector3.one;

        return zone;
    }

    private void RemoveZone(Zone zone)
    {
        if (zone == null)
            return;

        bool isBad = zone.type == ZoneType.Bad;

        if (isBad)
        {
            if (activeBadZones.Remove(zone))
            {
                DespawnZone(zone, true);
            }
        }
        else
        {
            if (activeSuccessZones.Remove(zone))
            {
                DespawnZone(zone, false);
            }
        }
    }

    private void DespawnZone(Zone zone, bool isBad)
    {
        if (zone == null)
            return;

        zone.gameObject.SetActive(false);

        if (isBad)
        {
            badPool.Enqueue(zone);
        }
        else
        {
            successPool.Enqueue(zone);
        }
    }

    private void ClearAllZones()
    {
        while (activeSuccessZones.Count > 0)
        {
            Zone zone = activeSuccessZones[0];
            activeSuccessZones.RemoveAt(0);
            DespawnZone(zone, false);
        }

        while (activeBadZones.Count > 0)
        {
            Zone zone = activeBadZones[0];
            activeBadZones.RemoveAt(0);
            DespawnZone(zone, true);
        }
    }

    private IEnumerator BadZoneLifecycle(Zone zone, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (state != GameState.Playing)
            yield break;

        if (zone == null)
            yield break;

        if (!activeBadZones.Contains(zone))
            yield break;

        RemoveZone(zone);

        yield return new WaitForSeconds(badZoneRespawnDelay);

        if (state == GameState.Playing && activeBadZones.Count < maxBadZones)
        {
            SpawnBadZone();
        }
    }

    private IEnumerator ScheduleBadZoneRespawn()
    {
        yield return new WaitForSeconds(badZoneRespawnDelay);

        if (state == GameState.Playing && activeBadZones.Count < maxBadZones)
        {
            SpawnBadZone();
        }
    }

    private bool TryGetValidSuccessZoneAngle(out float angle, out float width)
    {
        return TryGetValidZoneAngle(ZoneType.Success, out angle, out width);
    }

    private bool TryGetValidBadZoneAngle(out float angle, out float width)
    {
        return TryGetValidZoneAngle(ZoneType.Bad, out angle, out width);
    }

    private bool TryGetValidZoneAngle(ZoneType type, out float angle, out float width)
    {
        angle = 0f;
        width = 0f;

        float minWidth = type == ZoneType.Success
            ? minSuccessArcWidth
            : minBadArcWidth;

        float maxWidth = type == ZoneType.Success
            ? maxSuccessArcWidth
            : maxBadArcWidth;

        for (int attempt = 0; attempt < spawnAttempts; attempt++)
        {
            float candidateWidth = Random.Range(minWidth, maxWidth);
            float candidateAngle = Random.Range(0f, 360f);

            if (IsZoneAngleValid(candidateAngle, candidateWidth))
            {
                angle = NormalizeAngle(candidateAngle);
                width = candidateWidth;
                return true;
            }
        }

        return false;
    }

    private bool IsZoneAngleValid(float candidateAngle, float candidateWidth)
    {
        float halfWidth = candidateWidth * 0.5f;

        // Не спавним слишком близко к текущей позиции отмычки.
        if (pick != null && minDistanceFromPickDegrees > 0f)
        {
            float requiredPickDistance = halfWidth + minDistanceFromPickDegrees;
            float actualPickDistance = Mathf.Abs(Mathf.DeltaAngle(candidateAngle, pick.angle));

            if (actualPickDistance < requiredPickDistance)
                return false;
        }

        // Проверяем пересечение с активными успешными зонами.
        for (int i = 0; i < activeSuccessZones.Count; i++)
        {
            Zone zone = activeSuccessZones[i];

            if (zone == null)
                continue;

            if (ZonesOverlap(
                    candidateAngle,
                    candidateWidth,
                    zone.CenterAngle,
                    zone.arcWidth,
                    minZoneGapDegrees))
            {
                return false;
            }
        }

        // Проверяем пересечение с активными плохими зонами.
        for (int i = 0; i < activeBadZones.Count; i++)
        {
            Zone zone = activeBadZones[i];

            if (zone == null)
                continue;

            if (ZonesOverlap(
                    candidateAngle,
                    candidateWidth,
                    zone.CenterAngle,
                    zone.arcWidth,
                    minZoneGapDegrees))
            {
                return false;
            }
        }

        return true;
    }

    private bool ZonesOverlap(
        float angle1,
        float width1,
        float angle2,
        float width2,
        float gap)
    {
        float distance = Mathf.Abs(Mathf.DeltaAngle(angle1, angle2));

        float requiredDistance =
            width1 * 0.5f +
            width2 * 0.5f +
            gap;

        return distance < requiredDistance;
    }

    private float ResolveRingRadius()
    {
        if (ringVisual == null)
        {
            return fallbackRadius;
        }

        // Вариант 1: если на RingVisual есть CircleCollider2D.
        CircleCollider2D circleCollider = ringVisual.GetComponent<CircleCollider2D>();

        if (circleCollider != null && circleCollider.radius > 0.001f)
        {
            return circleCollider.radius * ringVisual.lossyScale.y;
        }

        // Вариант 2: если на RingVisual или его детях есть SpriteRenderer.
        SpriteRenderer spriteRenderer = ringVisual.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = ringVisual.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null)
        {
            // bounds — мировые границы спрайта.
            // extents.y — половина высоты, то есть радиус для круга/кольца.
            float radius = spriteRenderer.bounds.extents.y;

            if (radius > 0.001f)
            {
                return radius;
            }
        }

        // Вариант 3: если RingVisual просто масштабирован как радиус.
        if (ringVisual.lossyScale.y > 0.001f)
        {
            return ringVisual.lossyScale.y;
        }

        return fallbackRadius;
    }

    private float NormalizeAngle(float value)
    {
        value %= 360f;

        if (value < 0f)
        {
            value += 360f;
        }

        return value;
    }

    private void Win()
    {
        state = GameState.Win;

        int reward = CalculateReward();

        if (save != null)
        {
            save.coins += reward;
            SaveService.Save(save);
        }

        ShowResult("Замок открыт", reward);
    }

    private void Lose()
    {
        state = GameState.Lose;
        ShowResult("Время вышло", 0);
    }

    private int CalculateReward()
    {
        int timeBonus = Mathf.FloorToInt(timeLeft);
        int penalty = brokenPicks * brokenPickPenalty;

        int reward = baseCoins + timeBonus - penalty;

        return Mathf.Max(1, reward);
    }

    private void ShowResult(string title, int reward)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = title;
        }

        if (resultRewardText != null)
        {
            resultRewardText.text = $"Награда: {reward}";
        }

        UpdateHUD();
    }

    private void UpdateHUD()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timeLeft:F1}";
        }

        if (progressText != null)
        {
            progressText.text = $"Progress: {successCount}/{targetSuccess}";
        }

        if (coinsText != null && save != null)
        {
            coinsText.text = $"Coins: {save.coins}";
        }
    }

    private void UpdateDynamicZones(float deltaTime)
    {
        UpdateSuccessZones(deltaTime);
        UpdateBadZones(deltaTime);
    }

    private void UpdateSuccessZones(float deltaTime)
    {
        for (int i = activeSuccessZones.Count - 1; i >= 0; i--)
        {
            Zone zone = activeSuccessZones[i];

            if (zone == null)
            {
                activeSuccessZones.RemoveAt(i);
                continue;
            }

            if (!successShrinksToZero)
                continue;

            float newWidth = zone.arcWidth - successShrinkSpeed * deltaTime;

            if (newWidth <= successDisappearWidth)
            {
                RemoveZone(zone);

                if (state == GameState.Playing && successCount < targetSuccess)
                {
                    SpawnSuccessZone();
                }

                continue;
            }

            zone.SetArcWidth(newWidth);
        }
    }

    private void UpdateBadZones(float deltaTime)
    {
        for (int i = activeBadZones.Count - 1; i >= 0; i--)
        {
            Zone zone = activeBadZones[i];

            if (zone == null)
            {
                activeBadZones.RemoveAt(i);
                continue;
            }

            if (!badGrowsUntilSuccessIntersection)
                continue;

            float maxWidth = CalculateMaxBadWidth(zone);

            float grownWidth = zone.arcWidth + badGrowSpeed * deltaTime;

            float newWidth = Mathf.Min(grownWidth, maxWidth);
            newWidth = Mathf.Min(newWidth, badMaxArcWidth);

            // Не даём плохой зоне уменьшаться, если вдруг maxWidth стал меньше текущего.
            // Она просто перестаёт расти.
            if (newWidth < zone.arcWidth)
            {
                newWidth = zone.arcWidth;
            }

            if (newWidth > 0.01f)
            {
                zone.SetArcWidth(newWidth);
            }
        }
    }

    private float CalculateMaxBadWidth(Zone badZone)
    {
        if (badZone == null)
        {
            return 0f;
        }

        float maxWidth = badMaxArcWidth;

        for (int i = 0; i < activeSuccessZones.Count; i++)
        {
            Zone successZone = activeSuccessZones[i];

            if (successZone == null)
            {
                continue;
            }

            float distance = Mathf.Abs(
                Mathf.DeltaAngle(badZone.CenterAngle, successZone.CenterAngle)
            );

            float allowedFullWidth =
                2f *
                (
                    distance -
                    successZone.HalfWidth -
                    zoneTouchSafetyGap
                );

            if (allowedFullWidth < maxWidth)
            {
                maxWidth = allowedFullWidth;
            }
        }

        for (int i = 0; i < activeBadZones.Count; i++)
        {
            Zone otherBadZone = activeBadZones[i];

            if (otherBadZone == null || otherBadZone == badZone)
            {
                continue;
            }

            float distance = Mathf.Abs(
                Mathf.DeltaAngle(badZone.CenterAngle, otherBadZone.CenterAngle)
            );

            float allowedFullWidth =
                2f *
                (
                    distance -
                    otherBadZone.HalfWidth -
                    zoneTouchSafetyGap
                );

            if (allowedFullWidth < maxWidth)
            {
                maxWidth = allowedFullWidth;
            }
        }

        return Mathf.Max(0f, maxWidth);
    }
}