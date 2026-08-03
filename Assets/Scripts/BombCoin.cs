using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Tempel di prefab koin PALSU (visual sama kayak koin asli, tapi komponennya BombCoin, BUKAN DraggableCoin).
// Butuh: Image + CanvasGroup component di GameObject yang sama (sama kayak DraggableCoin).
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class BombCoin : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Timing")]
    public float armDuration = 3f;      // total waktu dari mulai diangkat sampai meledak
    public float freezeDuration = 5f;   // durasi cursor freeze pas meledak

    [Header("Efek Membesar & Bergetar")]
    public float maxScaleMultiplier = 1.6f;
    public float maxShakeStrength = 12f; // dalam pixel

    [Header("Efek Warna")]
    public Color explodeColor = Color.red; // warna pas mendekati meledak

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Image image;
    private Color originalColor;
    private Vector3 baseScale;
    private Vector2 dragPosition;   // posisi "asli" tanpa shake, di-update pas drag
    private bool isArmed = false;
    private bool isResolved = false; // udah meledak ATAU udah aman (defused)
    private float armTimer = 0f;
    private Coroutine armCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        image = GetComponent<Image>();
        baseScale = rectTransform.localScale;
        if (image != null) originalColor = image.color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.IsInputFrozen || isResolved) return;

        canvasGroup.blocksRaycasts = false;
        dragPosition = rectTransform.anchoredPosition;

        if (!isArmed)
        {
            isArmed = true;
            armTimer = 0f;
            armCoroutine = StartCoroutine(ArmRoutine());
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameManager.IsInputFrozen || isResolved) return;
        dragPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (GameManager.IsInputFrozen) return;
        if (isResolved) return;
        canvasGroup.blocksRaycasts = true;

        bool droppedInsideZone = FindZoneUnderPointer(eventData) != null;

        if (!droppedInsideZone)
        {
            // Aman: dilepas di luar zona manapun sebelum meledak
            Defuse();
        }
        // Kalau didrop DI DALAM zona: sengaja gak di-apa2in di sini,
        // armTimer tetap lanjut jalan di ArmRoutine sampai meledak
    }

    private IEnumerator ArmRoutine()
    {
        while (armTimer < armDuration && !isResolved)
        {
            armTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(armTimer / armDuration);

            // Membesar bertahap
            float scale = Mathf.Lerp(1f, maxScaleMultiplier, progress);
            rectTransform.localScale = baseScale * scale;

            // Getaran, makin kuat makin deket waktu meledak
            float shakeStrength = Mathf.Lerp(0f, maxShakeStrength, progress);
            Vector2 shakeOffset = new Vector2(
                Random.Range(-shakeStrength, shakeStrength),
                Random.Range(-shakeStrength, shakeStrength)
            );
            rectTransform.anchoredPosition = dragPosition + shakeOffset;

            // Transisi warna dari warna asli (disguise) ke merah, makin deket meledak makin merah
            if (image != null)
            {
                image.color = Color.Lerp(originalColor, explodeColor, progress);
            }

            yield return null;
        }

        if (!isResolved)
        {
            Explode();
        }
    }

    private void Defuse()
    {
        isResolved = true;
        if (armCoroutine != null) StopCoroutine(armCoroutine);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayBombDefuseSound();
        }

        Destroy(gameObject);
    }

    private void Explode()
    {
        isResolved = true;
        if (armCoroutine != null) StopCoroutine(armCoroutine);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayBombExplodeSound();
            GameManager.Instance.FreezeInput(freezeDuration);
            GameManager.Instance.ScreenShake();
        }

        Destroy(gameObject);
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