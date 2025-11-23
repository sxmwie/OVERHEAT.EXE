using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MainMenuBackgroundIcons : MonoBehaviour
{
    [Header("Setup")]
    public RectTransform spawnArea;       // usually the whole canvas or BackgroundIconsRoot
    public Transform iconsParent;         // BackgroundIconsRoot
    public GameObject iconPrefab;         // BGPowerupIcon prefab
    public Sprite[] iconSprites;          // assign your 3 powerup sprites here

    [Header("Spawn Timing")]
    public float minSpawnInterval = 0.4f;
    public float maxSpawnInterval = 1.2f;
    public int maxIconsOnScreen = 25;

    [Header("Movement")]
    public float minSpeed = 30f;
    public float maxSpeed = 90f;
    public float minLifeTime = 6f;
    public float maxLifeTime = 14f;
    public float minAngularSpeed = -40f;
    public float maxAngularSpeed = 40f;

    [Header("Scale / Wobble")]
    public float minScale = 0.6f;
    public float maxScale = 1.2f;
    public float wobbleAmplitude = 0.06f;
    public float wobbleSpeedMin = 0.6f;
    public float wobbleSpeedMax = 1.4f;

    float spawnTimer = 0f;
    float nextSpawnInterval;

    void Start()
    {
        if (spawnArea == null)
            spawnArea = GetComponent<RectTransform>();

        ScheduleNextSpawn();
    }

    void ScheduleNextSpawn()
    {
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
        spawnTimer = 0f;
    }

    void Update()
    {
        spawnTimer += Time.unscaledDeltaTime;
        if (spawnTimer >= nextSpawnInterval)
        {
            TrySpawnIcon();
            ScheduleNextSpawn();
        }
    }

    void TrySpawnIcon()
    {
        if (iconPrefab == null || spawnArea == null || iconSprites == null || iconSprites.Length == 0)
            return;

        if (iconsParent != null && iconsParent.childCount >= maxIconsOnScreen)
            return;

        // random edge spawn
        Rect r = spawnArea.rect;
        Vector2 pos;
        Vector2 vel;

        int edge = Random.Range(0, 4); // 0=left,1=right,2=bottom,3=top

        float speed = Random.Range(minSpeed, maxSpeed);
        switch (edge)
        {
            case 0: // left → right
                pos = new Vector2(r.xMin - 30f, Random.Range(r.yMin, r.yMax));
                vel = new Vector2(speed, Random.Range(-speed * 0.3f, speed * 0.3f));
                break;
            case 1: // right → left
                pos = new Vector2(r.xMax + 30f, Random.Range(r.yMin, r.yMax));
                vel = new Vector2(-speed, Random.Range(-speed * 0.3f, speed * 0.3f));
                break;
            case 2: // bottom → up
                pos = new Vector2(Random.Range(r.xMin, r.xMax), r.yMin - 30f);
                vel = new Vector2(Random.Range(-speed * 0.3f, speed * 0.3f), speed);
                break;
            default: // top → down
                pos = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + 30f);
                vel = new Vector2(Random.Range(-speed * 0.3f, speed * 0.3f), -speed);
                break;
        }

        GameObject go = Instantiate(iconPrefab, iconsParent != null ? iconsParent : spawnArea);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        // choose random sprite
        Image img = go.GetComponent<Image>();
        img.sprite = iconSprites[Random.Range(0, iconSprites.Length)];

        float life = Random.Range(minLifeTime, maxLifeTime);
        float angSpeed = Random.Range(minAngularSpeed, maxAngularSpeed);
        float scale = Random.Range(minScale, maxScale);
        float wobbleSpd = Random.Range(wobbleSpeedMin, wobbleSpeedMax);

        BGIconMover mover = go.GetComponent<BGIconMover>();
        mover.Init(vel, angSpeed, life, scale, wobbleAmplitude, wobbleSpd);
    }
}
