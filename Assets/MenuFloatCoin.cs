using UnityEngine;

// Tempel script ini di TIAP koin dekoratif di menu (Clover, Square, Love, Line).
// Gak perlu setup apapun di Inspector, defaultnya udah oke buat langsung dipakai.
[RequireComponent(typeof(RectTransform))]
public class MenuFloatCoin : MonoBehaviour
{
    [Header("Gerakan Melayang")]
    public float moveRadius = 15f;   // seberapa jauh dia geser dari posisi awal (pixel)
    public float moveSpeed = 1f;     // kecepatan dasar gerakannya

    [Header("Variasi Acak (biar tiap koin gak gerak bareng/serempak)")]
    public float speedVariance = 0.4f;   // 0.4 = kecepatan bisa beda -40% s/d +40% dari base
    public float radiusVariance = 6f;    // variasi jarak geser antar koin

    [Header("Goyangan Rotasi (opsional)")]
    public bool enableRotationWobble = true;
    public float rotateAmount = 6f; // derajat maksimal goyang rotasi

    private RectTransform rectTransform;
    private Vector2 originalPos;

    private float actualSpeed;
    private float actualRadius;
    private float phaseX;
    private float phaseY;
    private float phaseRot;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        originalPos = rectTransform.anchoredPosition;

        // Tiap koin dapet kecepatan & radius yang sedikit beda-beda, biar geraknya gak seragam
        actualSpeed = moveSpeed * Random.Range(1f - speedVariance, 1f + speedVariance);
        actualRadius = moveRadius + Random.Range(-radiusVariance, radiusVariance);

        // Phase offset acak, biar tiap koin mulai dari titik gelombang yang beda
        // (kalau semua phase-nya 0, semua koin bakal gerak SAMA PERSIS walau speednya beda dikit)
        phaseX = Random.Range(0f, Mathf.PI * 2f);
        phaseY = Random.Range(0f, Mathf.PI * 2f);
        phaseRot = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // Dua sumbu pakai frekuensi beda (0.8x di Y) biar pola geraknya "wavy"/acak,
        // bukan muter rapi bentuk lingkaran
        float offsetX = Mathf.Sin(Time.time * actualSpeed + phaseX) * actualRadius;
        float offsetY = Mathf.Cos(Time.time * actualSpeed * 0.8f + phaseY) * actualRadius;

        rectTransform.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

        if (enableRotationWobble)
        {
            float rot = Mathf.Sin(Time.time * actualSpeed * 0.6f + phaseRot) * rotateAmount;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}