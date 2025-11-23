using UnityEngine;
using Random = UnityEngine.Random;

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance;

    [Header("UI")]
    public RectTransform powerupBar;
    public RectTransform hitZone;
    public Transform moverParent; // parent for moving icon

    [Header("Prefabs")]
    public GameObject powerFreeze;
    public GameObject powerCool;
    public GameObject powerClear;

    [Header("Spawn Chances")]
    [Range(0f,1f)] public float chanceFreeze = 0.5f;
    [Range(0f,1f)] public float chanceCool   = 0.35f;
    [Range(0f,1f)] public float chanceClear  = 0.15f;

    [Header("Movement")]
    public float minSpeed = 300f;
    public float maxSpeed = 520f;

    GameObject currentIcon;
    RectTransform currentRT;
    float speed;
    bool active = false;

    float spawnTimer = 0f;
    float nextSpawnDelay = 7f; // first spawn
    float minDelay = 4f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!active)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnDelay)
                SpawnOne();
            return;
        }

        currentRT.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        // off the left side
        if (currentRT.anchoredPosition.x < -powerupBar.rect.width / 2f - 60f)
        {
            EndIcon(false);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            CheckCatch();
    }

    void SpawnOne()
    {
        spawnTimer = 0f;
        nextSpawnDelay = Random.Range(minDelay, 10f);

        currentIcon = Instantiate(SelectRandomPrefab(), moverParent);
        currentRT = currentIcon.GetComponent<RectTransform>();
        currentRT.localScale = Vector3.one;

        float startX = powerupBar.rect.width / 2f + 60f;
        currentRT.anchoredPosition = new Vector2(startX, 0f);

        speed = Random.Range(minSpeed, maxSpeed);
        active = true;
    }

    GameObject SelectRandomPrefab()
    {
        float r = Random.value;
        if (r < chanceClear) return powerClear;
        if (r < chanceClear + chanceCool) return powerCool;
        return powerFreeze;
    }

    void CheckCatch()
    {
        float x = currentRT.anchoredPosition.x;
        float zoneHalf = hitZone.rect.width / 2f;

        if (Mathf.Abs(x) <= zoneHalf)
            ApplyPowerup(currentIcon.name);

        EndIcon(true);
    }

    void EndIcon(bool triggered)
    {
        active = false;
        if (currentIcon != null)
            Destroy(currentIcon);
    }

    void ApplyPowerup(string name)
    {
        if (GameManager.Instance == null) return;

        // instead of using immediately, add to inventory
        if (name.Contains("Freeze"))
        {
            GameManager.Instance.AddPowerupFreeze(1);
        }
        else if (name.Contains("Cool"))
        {
            GameManager.Instance.AddPowerupCool(1);
        }
        else if (name.Contains("Clear"))
        {
            GameManager.Instance.AddPowerupClear(1);
        }
    }
}
