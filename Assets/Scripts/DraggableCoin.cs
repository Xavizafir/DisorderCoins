using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

// Tempel script ini di tiap koin. Set "Coin Type" di Inspector sesuai jenis koinnya.
// Butuh: Image component + CanvasGroup component di GameObject yang sama.
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class DraggableCoin : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Coin Settings")]
    public CoinType coinType;

    [Header("Spawn Animation")]
    public float spawnAnimDuration = 0.25f; // durasi animasi pop-in

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 originalAnchoredPos;
    private DropZone currentHoverZone; // zona yang lagi disorot pas drag

    // Status ini yang dicek GameManager buat tau apakah semua koin udah benar
    public bool IsPlacedCorrectly { get; private set; } = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        // Animasi pop-in: mulai dari scale 0, membesar ke 1 dengan sedikit overshoot (efek "pantul")
        rectTransform.localScale = Vector3.zero;
        StartCoroutine(SpawnAnimation());
    }

    private IEnumerator SpawnAnimation()
    {
        float t = 0f;
        while (t < spawnAnimDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / spawnAnimDuration);
            float scale = EaseOutBack(progress);
            rectTransform.localScale = Vector3.one * scale;
            yield return null;
        }
        rectTransform.localScale = Vector3.one; // pastiin pas 1 di akhir, gak kelebihan dari overshoot
    }

    // Formula easing "back" — bikin efek sedikit "kelewat gede" dulu baru pas ke ukuran normal
    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalAnchoredPos = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false; // biar raycast pas drag bisa "tembus" ke zona di bawahnya
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Gerakin koin ikutin mouse/finger, disesuaikan scale canvas
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        // Cek zona di bawah pointer tiap frame, nyalain highlight kalau posisinya berubah
        DropZone zoneUnderPointer = FindZoneUnderPointer(eventData);

        if (zoneUnderPointer != currentHoverZone)
        {
            if (currentHoverZone != null) currentHoverZone.SetHighlight(false);
            if (zoneUnderPointer != null) zoneUnderPointer.SetHighlight(true);
            currentHoverZone = zoneUnderPointer;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Matiin highlight zona terakhir yang di-hover, apapun hasilnya (benar/salah)
        if (currentHoverZone != null)
        {
            currentHoverZone.SetHighlight(false);
            currentHoverZone = null;
        }

        DropZone targetZone = FindZoneUnderPointer(eventData);

        if (targetZone != null && targetZone.IsCorrectCoin(coinType))
        {
            // Benar: snap ke titik zona (kalau ada) dan lock
            if (targetZone.snapPoint != null)
                rectTransform.position = targetZone.snapPoint.position;

            // Cuma hitung sekali — cegah double-count kalau koin yang UDAH benar di-drag ulang
            if (!IsPlacedCorrectly)
            {
                IsPlacedCorrectly = true;
                GameManager.Instance.OnCoinPlacedCorrectly(this);
            }
        }
        else
        {
            // Salah taruh (atau di-drop di area kosong): koin TETAP di posisi drop terakhir,
            // TIDAK game over, player bisa pindahin lagi kapan aja.
            if (IsPlacedCorrectly)
            {
                // kalau sebelumnya udah benar terus dipindah ke tempat salah, batalin status benar-nya
                IsPlacedCorrectly = false;
                GameManager.Instance.OnCoinRemovedFromCorrectPlace(this);
            }
        }
    }

    private DropZone FindZoneUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            DropZone zone = result.gameObject.GetComponent<DropZone>();
            if (zone != null) return zone;
        }
        return null;
    }
}