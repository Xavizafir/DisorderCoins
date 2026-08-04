using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// Tempel script ini di GameObject Button manapun (Play, Quit, Restart, dll).
// Tombol bakal membesar dikit pas di-hover (mouse masuk), balik normal pas mouse keluar.
[RequireComponent(typeof(RectTransform))]
public class ButtonHoverPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Pop Settings")]
    public float hoverScaleMultiplier = 1.1f; // seberapa besar membesar pas di-hover
    public float animDuration = 0.15f;        // kecepatan animasi membesar/mengecil

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Coroutine scaleCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ScaleTo(hoverScaleMultiplier);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(1f);
    }

    private void ScaleTo(float targetScale)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale));
    }

    private IEnumerator ScaleAnimation(float targetScale)
    {
        float startScale = rectTransform.localScale.x / baseScale.x;
        float t = 0f;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime; // biar tetep jalan walau Time.timeScale = 0 (pause/tutorial)
            float progress = Mathf.Clamp01(t / animDuration);
            float current = Mathf.Lerp(startScale, targetScale, progress);
            rectTransform.localScale = baseScale * current;
            yield return null;
        }

        rectTransform.localScale = baseScale * targetScale;
    }
}