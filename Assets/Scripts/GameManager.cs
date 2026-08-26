using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

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

    [Header("Audio Settings (SFX only — BGM dipisah jadi GameObject sendiri)")]
    public AudioSource sfxAudioSource;
    public AudioClip popOutSound;
    public AudioClip coinDropSound; // disimpan di sini agar mudah diakses dari DraggableCoin
    public AudioClip zoneShuffleSound; //buat zona coin geser
    public AudioClip flashExplodeSound;
    public AudioClip buttonClickSound; // dipake tombol Restart & Menu lewat PlayButtonClickSound()
    public AudioClip bombExplodeSound; // dipake pas bomb coin meledak
    public AudioClip bombDefuseSound;  // dipake pas bomb coin berhasil dijinakin (aman)

    [Header("Stage Settings")]
    public int baseTotalCoins = 4;     // total semua koin di stage 1 (gabungan semua jenis)
    public int coinsIncrementPerStage = 1;
    public float baseTimeLimit = 15f;
    public float totalSpawnDuration = 2.5f; // semua koin harus muncul dalam durasi ini

    [Header("Dynamic Time Bonus (gantiin flat +10 detik)")]
    public float secondsPerCoin = 2.5f;      // waktu tambahan PER KOIN di stage baru itu
    public float bombPenaltyBuffer = 3f;     // ekstra detik kalau stage ini udah ada bomb coin
    public float shufflePenaltyBuffer = 2f;  // ekstra detik kalau stage ini zona lagi diacak

    [Header("Color Match Mode (khusus Mode Gameplay 2 — matching by warna, bukan simbol)")]
    public bool colorMatchMode = false; // centang ini kalau scene ini pakai mode warna
    public Color greenTint = new Color(0.25f, 0.75f, 0.25f);
    public Color redTint = new Color(0.85f, 0.2f, 0.2f);
    public Color yellowTint = new Color(0.9f, 0.85f, 0.2f);
    public Color blueTint = new Color(0.2f, 0.4f, 0.9f);

    [Header("Scoring")]
    public int pointsPerCorrectPlacement = 10; // nambah tiap kali koin ditaruh BENAR

    [Header("Zone Shuffle")]
    public List<DropZone> zones; // drag IndoPlace, ChinaPlace, USPlace, EuropePlace ke sini
    public int shuffleStartStage = 4; // mulai stage berapa posisi zona diacak
    public float zoneMoveDuration = 0.6f; // durasi animasi geser posisi zona

    [Header("Bomb Coin (Decoy)")]
    public List<GameObject> bombCoinPrefabs; // drag prefab koin PALSU (visual disguise) ke sini
    public int bombStartStage = 3; // mulai stage berapa bomb coin muncul
    public int bombCoinsPerStage = 1; // jumlah bomb coin pas PERTAMA kali muncul (di bombStartStage)
    public int bombCoinsIncrementPerStage = 1; // nambah berapa tiap stage abis bombStartStage

    [Header("Flash Coin Settings")]
    public List<GameObject> flashCoinPrefabs; // drag prefab koin flash di sini
    public int flashStartStage = 6;           // mulai muncul di stage 6
    public int flashCoinsPerStage = 2;        // jumlah flash coin pas PERTAMA kali muncul (di flashStartStage)
    public int flashCoinsIncrementPerStage = 1; // nambah berapa tiap stage abis flashStartStage
    public UnityEngine.UI.Image flashOverlayImage;          // Panel UI warna putih penuh
    public float flashDuration = 2.5f;

    [Header("UI (opsional, isi kalau sudah ada)")]
    public TMP_Text timerText;
    public TMP_Text stageText;
    public TMP_Text scoreText;

    [Header("Freeze Feedback (opsional)")]
    public GameObject freezeOverlay; // panel/UI apapun yang muncul pas input lagi freeze (misal tint merah + teks "FROZEN")

    [Header("Screen Shake")]
    public RectTransform screenShakeTarget; // drag Canvas (RectTransform) ke sini
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 25f; // seberapa jauh geser tiap frame (pixel)

    public static bool IsInputFrozen = false; // dicek DraggableCoin & BombCoin buat block drag

    private int currentStage = 1;
    private int score = 0;
    private float timeRemaining;
    private bool isSpawning = false;
    private bool isGameOver = false;

    // Dicek dari PauseManager.cs, biar ESC gak bisa dipencet lagi pas udah Game Over
    public bool IsGameOver => isGameOver;

    private List<DraggableCoin> activeCoins = new List<DraggableCoin>();
    private List<GameObject> activeBombCoins = new List<GameObject>();
    private int correctCount = 0;
    private List<Vector2> zoneSlotPositions = new List<Vector2>(); // posisi asli tiap slot zona

    // Baseline TETAP buat screenShakeTarget — disimpen SEKALI di awal, bukan tiap kali
    // shake/dizzy dipanggil. Ini yang bikin efek bomb+flash yang numpuk bareng gak saling
    // korupsi "posisi/rotasi asli" satu sama lain
    private Vector2 shakeBaselinePos;
    private Quaternion shakeBaselineRot;
    private Vector3 shakeBaselineScale;

    // Penghitung berapa banyak sumber yang lagi minta freeze bersamaan (bomb + flash bisa numpuk)
    private static int freezeRequestCount = 0;

    void Awake()
    {
        Instance = this;

        // PENTING: reset paksa tiap scene baru dimulai. freezeRequestCount & IsInputFrozen
        // itu static, jadi bisa "kebawa" nilai sisa dari scene sebelumnya kalau ada
        // coroutine freeze yang keputus di tengah jalan (misal gara-gara Restart/Menu
        // dipencet pas lagi ada bomb/flash yang lagi ngefreeze). Tanpa reset ini,
        // freeze bisa numpuk jadi permanen gak pernah kebuka lagi.
        freezeRequestCount = 0;
        IsInputFrozen = false;
    }

    void Start()
    {
        // Simpen posisi asli tiap zona sebagai "slot" yang nanti bakal ditukar-tukar
        foreach (DropZone zone in zones)
        {
            zoneSlotPositions.Add(zone.GetComponent<RectTransform>().anchoredPosition);
        }

        if (screenShakeTarget != null)
        {
            shakeBaselinePos = screenShakeTarget.anchoredPosition;
            shakeBaselineRot = screenShakeTarget.localRotation;
            shakeBaselineScale = screenShakeTarget.localScale;
        }

        UpdateScoreDisplay();
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

        // Hapus bomb coin yang belum sempet di-resolve
        foreach (GameObject bomb in activeBombCoins)
        {
            if (bomb != null) Destroy(bomb);
        }

        correctCount = 0;
        activeCoins.Clear();
        activeBombCoins.Clear();

        if (currentStage == 1)
        {
            timeRemaining = baseTimeLimit;
        }
        else
        {
            timeRemaining += CalculateDynamicTimeBonus();
        }

        if (stageText != null) stageText.text = "Stage " + currentStage;

        if (currentStage >= shuffleStartStage)
        {
            ShuffleZonePositions();
        }

        StartCoroutine(SpawnCoinsRoutine());
    }

    // Ganti flat +10 detik jadi dinamis: disesuaikan JUMLAH KOIN baru di stage ini,
    // plus buffer ekstra kalau stage ini udah ada bomb dan/atau zona diacak
    private float CalculateDynamicTimeBonus()
    {
        int totalCoinsThisStage = baseTotalCoins + (currentStage - 1) * coinsIncrementPerStage;
        float bonus = totalCoinsThisStage * secondsPerCoin;

        if (currentStage >= bombStartStage)
        {
            bonus += bombPenaltyBuffer;
        }

        if (currentStage >= shuffleStartStage)
        {
            bonus += shufflePenaltyBuffer;
        }

        return bonus;
    }

    void ShuffleZonePositions()
    {
        if (sfxAudioSource != null && zoneShuffleSound != null)
        {
            sfxAudioSource.PlayOneShot(zoneShuffleSound);
        }

        // Kocok urutan posisi slot
        List<Vector2> shuffledSlots = new List<Vector2>(zoneSlotPositions);
        for (int i = shuffledSlots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledSlots[i], shuffledSlots[j]) = (shuffledSlots[j], shuffledSlots[i]);
        }

        // Pasang tiap zona ke posisi slot hasil kocokan, dengan animasi geser (bukan instant)
        for (int i = 0; i < zones.Count; i++)
        {
            RectTransform zoneRect = zones[i].GetComponent<RectTransform>();
            StartCoroutine(MoveZoneSmooth(zoneRect, shuffledSlots[i], zoneMoveDuration));
        }

        // Setelah semua zona selesai geser, cek ulang penempatan tiap koin yang UDAH ADA
        // (kalau kejadian di tengah stage, misal dari efek flash) — biar koin yang tadinya
        // bener tapi zonanya udah pindah, otomatis dianggap gak bener lagi
        StartCoroutine(RevalidateCoinsAfterShuffle());
    }

    private IEnumerator RevalidateCoinsAfterShuffle()
    {
        yield return new WaitForSeconds(zoneMoveDuration);

        foreach (DraggableCoin coin in activeCoins)
        {
            if (coin != null)
            {
                coin.RevalidateAgainstZones(zones);
            }
        }
    }

    IEnumerator MoveZoneSmooth(RectTransform zoneRect, Vector2 targetPos, float duration)
    {
        Vector2 startPos = zoneRect.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float eased = EaseInOutQuad(progress);
            zoneRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            yield return null;
        }

        zoneRect.anchoredPosition = targetPos; // pastiin pas di posisi akhir
    }

    // Easing halus: pelan di awal, cepat di tengah, pelan lagi di akhir
    private float EaseInOutQuad(float x)
    {
        return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2) / 2f;
    }

    // Struct kecil buat nampung 1 slot spawn: prefab-nya apa, dan apakah dia bomb/flash/koin asli
    [System.Serializable]
    public struct SpawnItem
    {
        public GameObject prefab;
        public bool isBomb;
        public bool isFlash;
    }

    IEnumerator SpawnCoinsRoutine()
    {
        isSpawning = true;

        int totalCoins = baseTotalCoins + (currentStage - 1) * coinsIncrementPerStage;
        List<GameObject> allPrefabs = new List<GameObject> { indoCoinPrefab, chinaCoinPrefab, usCoinPrefab, europeCoinPrefab };
        List<SpawnItem> spawnQueue = new List<SpawnItem>();

        // 1. Koin Normal
        for (int i = 0; i < totalCoins; i++)
        {
            spawnQueue.Add(new SpawnItem { prefab = allPrefabs[Random.Range(0, allPrefabs.Count)], isBomb = false, isFlash = false });
        }

        // 2. Bomb Coin — jumlahnya nambah tiap stage abis bombStartStage
        if (currentStage >= bombStartStage && bombCoinPrefabs != null && bombCoinPrefabs.Count > 0)
        {
            int bombCount = bombCoinsPerStage + (currentStage - bombStartStage) * bombCoinsIncrementPerStage;

            for (int i = 0; i < bombCount; i++)
            {
                GameObject bombPrefab = bombCoinPrefabs[Random.Range(0, bombCoinPrefabs.Count)];
                spawnQueue.Add(new SpawnItem { prefab = bombPrefab, isBomb = true, isFlash = false });
            }
        }

        // 3. Flash Coin — jumlahnya nambah tiap stage abis flashStartStage
        if (currentStage >= flashStartStage && flashCoinPrefabs != null && flashCoinPrefabs.Count > 0)
        {
            int flashCount = flashCoinsPerStage + (currentStage - flashStartStage) * flashCoinsIncrementPerStage;

            for (int i = 0; i < flashCount; i++)
            {
                GameObject flashPrefab = flashCoinPrefabs[Random.Range(0, flashCoinPrefabs.Count)];
                spawnQueue.Add(new SpawnItem { prefab = flashPrefab, isBomb = false, isFlash = true });
            }
        }

        // Kocok urutan seluruh queue
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (spawnQueue[i], spawnQueue[j]) = (spawnQueue[j], spawnQueue[i]);
        }

        float delayBetweenSpawns = totalSpawnDuration / spawnQueue.Count;

        foreach (SpawnItem item in spawnQueue)
        {
            if (item.isFlash)
            {
                Debug.Log("[SPAWN] Flash Coin Muncul!");
                SpawnBombCoin(item.prefab);
            }
            else if (item.isBomb)
            {
                Debug.Log("[SPAWN] Bomb Coin Muncul!");
                SpawnBombCoin(item.prefab);
            }
            else
            {
                Debug.Log("[SPAWN] Koin Normal Muncul");
                SpawnCoin(item.prefab);
            }

            yield return new WaitForSeconds(delayBetweenSpawns);
        }

        isSpawning = false;
    }

    // Mapping CoinColorType ke warna asli buat tint visual koin di Mode Gameplay 2
    private Color GetTintForColor(CoinColorType type)
    {
        switch (type)
        {
            case CoinColorType.Green: return greenTint;
            case CoinColorType.Red: return redTint;
            case CoinColorType.Yellow: return yellowTint;
            case CoinColorType.Blue: return blueTint;
            default: return Color.white;
        }
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

        // Mode Gameplay 2: tiap koin dikasih warna RANDOM (independen dari simbolnya),
        // dan visualnya di-tint sesuai warna itu, dicocokinnya nanti berdasarkan warna
        if (colorMatchMode)
        {
            System.Array allColors = System.Enum.GetValues(typeof(CoinColorType));
            CoinColorType randomColor = (CoinColorType)allColors.GetValue(Random.Range(0, allColors.Length));
            coin.assignedColor = randomColor;

            UnityEngine.UI.Image img = coinObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.color = GetTintForColor(randomColor);
            }
        }

        if (sfxAudioSource != null && popOutSound != null)
        {
            sfxAudioSource.PlayOneShot(popOutSound);
        }
    }

    void SpawnBombCoin(GameObject prefab)
    {
        GameObject bombObj = Instantiate(prefab, spawnArea);
        RectTransform rt = bombObj.GetComponent<RectTransform>();

        float margin = 40f;
        float halfW = spawnArea.rect.width / 2f - margin;
        float halfH = spawnArea.rect.height / 2f - margin;
        Vector2 randomPos = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
        rt.anchoredPosition = randomPos;

        // SENGAJA gak dimasukin ke activeCoins — bomb coin gak dihitung buat syarat lolos stage
        activeBombCoins.Add(bombObj);

        // Mode Gameplay 2: bomb/flash coin juga HARUS ikut ke-tint biar nyamar
        // sama kayak koin normal, gak keliatan beda sendiri
        if (colorMatchMode)
        {
            System.Array allColors = System.Enum.GetValues(typeof(CoinColorType));
            CoinColorType randomColor = (CoinColorType)allColors.GetValue(Random.Range(0, allColors.Length));
            Color tint = GetTintForColor(randomColor);

            BombCoin bombComp = bombObj.GetComponent<BombCoin>();
            if (bombComp != null) bombComp.SetVisualColor(tint);

            FlashCoin flashComp = bombObj.GetComponent<FlashCoin>();
            if (flashComp != null) flashComp.SetVisualColor(tint);
        }

        if (sfxAudioSource != null && popOutSound != null)
        {
            sfxAudioSource.PlayOneShot(popOutSound);
        }
    }

    // Dipanggil dari BombCoin.cs pas meledak
    public void FreezeInput(float duration)
    {
        StartCoroutine(FreezeInputRoutine(duration));
    }

    private IEnumerator FreezeInputRoutine(float duration)
    {
        freezeRequestCount++;
        IsInputFrozen = true;
        if (freezeOverlay != null) freezeOverlay.SetActive(true);

        yield return new WaitForSeconds(duration);

        freezeRequestCount = Mathf.Max(0, freezeRequestCount - 1);
        if (freezeRequestCount == 0)
        {
            IsInputFrozen = false;
            if (freezeOverlay != null) freezeOverlay.SetActive(false);
        }
    }

    // Dipanggil dari BombCoin.cs pas meledak
    public void ScreenShake()
    {
        if (screenShakeTarget != null)
        {
            StartCoroutine(ScreenShakeRoutine());
        }
    }

    private IEnumerator ScreenShakeRoutine()
    {
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float progress = t / shakeDuration;

            // Kekuatan shake makin ngecil seiring waktu, biar berhentinya smooth bukan mendadak
            float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, progress);
            Vector2 offset = new Vector2(
                Random.Range(-currentMagnitude, currentMagnitude),
                Random.Range(-currentMagnitude, currentMagnitude)
            );

            // Pakai baseline TETAP (shakeBaselinePos), bukan posisi "sekarang" —
            // biar aman walau ada shake lain yang numpuk bersamaan
            screenShakeTarget.anchoredPosition = shakeBaselinePos + offset;
            yield return null;
        }

        screenShakeTarget.anchoredPosition = shakeBaselinePos; // pastiin balik pas ke posisi baseline (bukan "originalPos" lokal)
    }

    public void TriggerFlashbang()
    {
        StartCoroutine(FlashbangRoutine());
    }

    private IEnumerator FlashbangRoutine()
    {
        freezeRequestCount++;
        IsInputFrozen = true;

        if (sfxAudioSource != null && flashExplodeSound != null)
        {
            sfxAudioSource.PlayOneShot(flashExplodeSound);
        }

        if (flashOverlayImage != null)
        {
            flashOverlayImage.gameObject.SetActive(true);

            Color c = flashOverlayImage.color;
            c.a = 1f; // Layar langsung putih penuh
            flashOverlayImage.color = c;

            // Acak posisi zona di balik layar putih
            ShuffleZonePositions();

            // Efek Pusing (Layar bergoyang memutar & zoom)
            StartCoroutine(DizzyEffectRoutine(flashDuration));

            // Efek memudar dari putih ke transparan
            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / flashDuration);
                flashOverlayImage.color = c;
                yield return null;
            }

            flashOverlayImage.gameObject.SetActive(false);
        }
        else
        {
            ShuffleZonePositions();
            StartCoroutine(DizzyEffectRoutine(flashDuration));
            yield return new WaitForSeconds(flashDuration);
        }

        freezeRequestCount = Mathf.Max(0, freezeRequestCount - 1);
        if (freezeRequestCount == 0)
        {
            IsInputFrozen = false;
        }
    }

    // Coroutine untuk Efek Pusing/Goyangan Layar
    private IEnumerator DizzyEffectRoutine(float duration)
    {
        if (screenShakeTarget == null) yield break;

        float t = 0f;
        float frequency = 12f; // Kecepatan goyangan
        float maxAngle = 5f;    // Kemiringan rotasi (derajat)
        float maxScale = 0.05f; // Efek zoom in-out tipis

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            // Kekuatan pusing makin berkurang seiring waktu (fading)
            float currentAngle = Mathf.Sin(t * frequency) * maxAngle * (1f - progress);
            float currentScaleOffset = Mathf.Cos(t * frequency * 0.5f) * maxScale * (1f - progress);

            // Pakai baseline TETAP (shakeBaselineRot/Scale), bukan capture ulang tiap kali —
            // biar 2 dizzy effect yang numpuk gak saling ngerusak titik "netral" satu sama lain
            screenShakeTarget.localRotation = shakeBaselineRot * Quaternion.Euler(0, 0, currentAngle);
            screenShakeTarget.localScale = shakeBaselineScale + new Vector3(currentScaleOffset, currentScaleOffset, 0);

            yield return null;
        }

        // Kembalikan ke baseline yang bener, bukan "originalRotation" lokal yang bisa udah korup
        screenShakeTarget.localRotation = shakeBaselineRot;
        screenShakeTarget.localScale = shakeBaselineScale;
    }

    public void OnCoinPlacedCorrectly(DraggableCoin coin)
    {
        correctCount++;
        score += pointsPerCorrectPlacement;
        UpdateScoreDisplay();
        CheckStageComplete();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    // Dipanggil dari ButtonClickSFX.cs pas tombol (Restart/Menu/dll) di-klik
    public void PlayButtonClickSound()
    {
        if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    // Dipanggil dari BombCoin.cs pas bom meledak
    public void PlayBombExplodeSound()
    {
        if (sfxAudioSource != null && bombExplodeSound != null)
        {
            sfxAudioSource.PlayOneShot(bombExplodeSound);
        }
    }

    // Dipanggil dari BombCoin.cs pas bom berhasil dijinakin (dilepas di luar zona sebelum meledak)
    public void PlayBombDefuseSound()
    {
        if (sfxAudioSource != null && bombDefuseSound != null)
        {
            sfxAudioSource.PlayOneShot(bombDefuseSound);
        }
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
            GameOverUI.Instance.ShowGameOver(currentStage, score);
        }
        else
        {
            Debug.LogWarning("GameOverUI belum ke-setup di scene! Stage: " + currentStage + " Score: " + score);
        }
    }
}