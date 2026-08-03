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

    [Header("Drag Animation")]
    public float dragScaleMultiplier = 1.15f; // seberapa besar koin membesar pas di-drag
    public float dragScaleAnimDuration = 0.15f; // durasi animasi membesar/mengecil

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 originalAnchoredPos;
    private DropZone currentHoverZone; // zona yang lagi disorot pas drag
    private Coroutine scaleCoroutine; // biar animasi lama ke-cancel kalau ada yang baru

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

    // Animasi scale reusable — dipakai buat efek membesar/mengecil pas drag
    private void ScaleTo(float targetScale, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale, duration));
    }

    private IEnumerator ScaleAnimation(float targetScale, float duration)
    {
        float startScale = rectTransform.localScale.x;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float current = Mathf.Lerp(startScale, targetScale, progress);
            rectTransform.localScale = Vector3.one * current;
            yield return null;
        }

        rectTransform.localScale = Vector3.one * targetScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.IsInputFrozen) return;

        originalAnchoredPos = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false; // biar raycast pas drag bisa "tembus" ke zona di bawahnya

        ScaleTo(dragScaleMultiplier, dragScaleAnimDuration);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameManager.IsInputFrozen) return;

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
        if (GameManager.IsInputFrozen) return;

        canvasGroup.blocksRaycasts = true;

        ScaleTo(1f, dragScaleAnimDuration);

        // Matiin highlight zona terakhir yang di-hover, apapun hasilnya (benar/salah)
        if (currentHoverZone != null)
        {
            currentHoverZone.SetHighlight(false);
            currentHoverZone = null;
        }

        DropZone targetZone = FindZoneUnderPointer(eventData);

        if (targetZone != null && targetZone.IsCorrectCoin(coinType))
        {
            if (targetZone.snapPoint != null)
            {
                // Ada snap point yang di-set manual: koin ketarik pas ke situ
                rectTransform.position = targetZone.snapPoint.position;
            }
            else
            {
                // Gak ada snap point: koin tetap di posisi drop-nya,
                // cuma didorong secukupnya biar gak nyembul keluar kotak zona
                targetZone.ClampCoinInside(rectTransform);
            }

            // Cuma hitung sekali — cegah double-count kalau koin yang UDAH benar di-drag ulang
            if (!IsPlacedCorrectly)
            {
                IsPlacedCorrectly = true;
                GameManager.Instance.OnCoinPlacedCorrectly(this);
            }

            if (GameManager.Instance.audioSource != null && GameManager.Instance.coinDropSound != null)
            {
                GameManager.Instance.audioSource.PlayOneShot(GameManager.Instance.coinDropSound);
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