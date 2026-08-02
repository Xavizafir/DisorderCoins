using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Coin Prefabs (1 per jenis)")]
    public GameObject indoCoinPrefab;
    public GameObject chinaCoinPrefab;
    public GameObject usCoinPrefab;
    public GameObject europeCoinPrefab;

    [Header("Spawn Area")]
    public RectTransform spawnArea; // drag kotak gelap (area tengah) ke sini

    [Header("Stage Settings")]
    public int baseTotalCoins = 4;     // total semua koin di stage 1 (gabungan semua jenis)
    public int coinsIncrementPerStage = 1;
    public float baseTimeLimit = 15f;
    public float timeBonusPerStage = 10f;
    public float totalSpawnDuration = 2.5f; // semua koin harus muncul dalam durasi ini

    [Header("UI (opsional, isi kalau sudah ada)")]
    public TMP_Text timerText;
    public TMP_Text stageText;

    private int currentStage = 1;
    private float timeRemaining;
    private bool isSpawning = false;
    private bool isGameOver = false;

    private List<DraggableCoin> activeCoins = new List<DraggableCoin>();
    private int correctCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartStage();
    }

    void Update()
    {
        if (isGameOver || isSpawning) return;

        timeRemaining -= Time.deltaTime;
        if (timerText != null) timerText.text = Mathf.Max(0, Mathf.CeilToInt(timeRemaining)).ToString();

        if (timeRemaining <= 0f)
        {
            GameOver();
        }
    }

    void StartStage()
    {
        // Hapus koin dari stage sebelumnya biar gak numpuk
        foreach (DraggableCoin coin in activeCoins)
        {
            if (coin != null) Destroy(coin.gameObject);
        }

        correctCount = 0;
        activeCoins.Clear();

        // Stage 1: mulai dari base time limit.
        // Stage selanjutnya: SISA waktu + bonus (bukan reset dari rumus)
        if (currentStage == 1)
        {
            timeRemaining = baseTimeLimit;
        }
        else
        {
            timeRemaining += timeBonusPerStage;
        }

        if (stageText != null) stageText.text = "Stage " + currentStage;

        StartCoroutine(SpawnCoinsRoutine());
    }

    IEnumerator SpawnCoinsRoutine()
    {
        isSpawning = true;

        int totalCoins = baseTotalCoins + (currentStage - 1) * coinsIncrementPerStage;
        List<GameObject> allPrefabs = new List<GameObject> { indoCoinPrefab, chinaCoinPrefab, usCoinPrefab, europeCoinPrefab };
        List<GameObject> spawnQueue = new List<GameObject>();

        // Tiap slot dipilih random dari 4 jenis, jadi komposisinya bisa timpang
        // (misal 3 biru 1 kuning, atau 4-4-nya jenis yang sama)
        for (int i = 0; i < totalCoins; i++)
        {
            spawnQueue.Add(allPrefabs[Random.Range(0, allPrefabs.Count)]);
        }

        float delayBetweenSpawns = totalSpawnDuration / spawnQueue.Count;

        foreach (GameObject prefab in spawnQueue)
        {
            SpawnCoin(prefab);
            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        isSpawning = false;
    }

    void SpawnCoin(GameObject prefab)
    {
        GameObject coinObj = Instantiate(prefab, spawnArea);
        RectTransform rt = coinObj.GetComponent<RectTransform>();

        // Posisi acak di dalam spawnArea (dengan sedikit margin biar ga nempel tepi)
        float margin = 40f;
        float halfW = spawnArea.rect.width / 2f - margin;
        float halfH = spawnArea.rect.height / 2f - margin;
        Vector2 randomPos = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
        rt.anchoredPosition = randomPos;

        DraggableCoin coin = coinObj.GetComponent<DraggableCoin>();
        activeCoins.Add(coin);
    }

    public void OnCoinPlacedCorrectly(DraggableCoin coin)
    {
        correctCount++;
        CheckStageComplete();
    }

    public void OnCoinRemovedFromCorrectPlace(DraggableCoin coin)
    {
        correctCount--;
    }

    void CheckStageComplete()
    {
        // Cuma bisa complete kalau semua koin udah selesai spawn DAN semua benar
        if (!isSpawning && correctCount >= activeCoins.Count)
        {
            currentStage++;
            StartStage();
        }
    }

    void GameOver()
    {
        isGameOver = true;

        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver(currentStage);
        }
        else
        {
            Debug.LogWarning("GameOverUI belum ke-setup di scene! Score: " + currentStage);
        }
    }
}