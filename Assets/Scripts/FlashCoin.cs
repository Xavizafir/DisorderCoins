using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class FlashCoin : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Timing")]
    public float armDuration = 3f;      // Total waktu diangkat sampai meledak

    [Header("Efek Membesar & Bergetar")]
    public float maxScaleMultiplier = 1.6f;
    public float maxShakeStrength = 12f; // Dalam pixel

    [Header("Efek Warna & Kelap-Kelip (Flash)")]
    public Color flashColor = Color.white; // Warna kilauan putih
    public float flashSpeed = 15f;         // Kecepatan kedipan putih

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Image image;
    private Color originalColor;
    private Vector3 baseScale;
    private Vector2 dragPosition;   // Posisi asli tanpa shake
    private bool isArmed = false;
    private bool isResolved = false; // Udah meledak ATAU udah defused
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

    // Dipanggil dari GameManager.cs (khusus Mode Gameplay 2) buat nge-tint disguise-nya
    // biar nyamar sama kayak koin normal, DAN update originalColor-nya juga
    // biar animasi kelap-kelip gak "bocor" balik ke warna asli pas mulai di-drag
    public void SetVisualColor(Color color)
    {
        originalColor = color;
        if (image != null) image.color = color;
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
        canvasGroup.blocksRaycasts = true; // WAJIB selalu di-restore duluan

        if (GameManager.IsInputFrozen || isResolved) return;

        bool droppedInsideZone = FindZoneUnderPointer(eventData) != null;

        if (!droppedInsideZone)
        {
            // Aman: dilepas di luar zona manapun sebelum meledak
            Defuse();
        }
    }

    private IEnumerator ArmRoutine()
    {
        while (armTimer < armDuration && !isResolved)
        {
            armTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(armTimer / armDuration);

            // 1. Membesar bertahap
            float scale = Mathf.Lerp(1f, maxScaleMultiplier, progress);
            rectTransform.localScale = baseScale * scale;

            // 2. Getaran, makin kuat mendekati waktu meledak
            float shakeStrength = Mathf.Lerp(0f, maxShakeStrength, progress);
            Vector2 shakeOffset = new Vector2(
                Random.Range(-shakeStrength, shakeStrength),
                Random.Range(-shakeStrength, shakeStrength)
            );
            rectTransform.anchoredPosition = dragPosition + shakeOffset;

            // 3. Efek Kelap-Kelip Putih (Flash Strobe)
            // Kedipan makin cepat seiring mendekati waktu meledak
            if (image != null)
            {
                float currentFlashSpeed = flashSpeed * (1f + progress);
                float flashPingPong = Mathf.PingPong(Time.time * currentFlashSpeed, 1f);
                image.color = Color.Lerp(originalColor, flashColor, flashPingPong);
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
        Destroy(gameObject);
    }

    private void Explode()
    {
        isResolved = true;
        if (armCoroutine != null) StopCoroutine(armCoroutine);

        if (GameManager.Instance != null)
        {
            // Panggil efek layar putih + dizzy effect di GameManager
            GameManager.Instance.TriggerFlashbang();
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