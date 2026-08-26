using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// 4 warna yang dipake buat Mode Gameplay 2 (matching by color, bukan simbol)
public enum CoinColorType
{
    Green,
    Red,
    Yellow,
    Blue
}

// Tempel script ini di tiap GameObject "Place" (IndoPlace, ChinaPlace, USPlace, EuropePlace,
// atau di Mode Gameplay 2: GreenPlace, RedPlace, YellowPlace, BluePlace).
// Set "Zone Type" di Inspector sesuai jenis zonanya (kalau matching by simbol),
// atau centang "Match By Color" dan set "Zone Color" (kalau matching by warna).
[RequireComponent(typeof(Image))]
public class DropZone : MonoBehaviour
{
    [Header("Zone Settings (Mode Normal — matching by simbol)")]
    public CoinType zoneType;

    [Header("Color Mode (Mode Gameplay 2 — matching by warna)")]
    public bool matchByColor = false; // centang ini kalau zona ini buat mode warna
    public CoinColorType zoneColor;

    [Header("Snap Settings")]
    public RectTransform snapPoint; // opsional: kalau mau koin snap ke titik tengah zona, drag RectTransform zona ini ke sini

    [Header("Highlight Settings (Scale Pop)")]
    public float highlightScaleMultiplier = 1.1f; // seberapa besar zona membesar pas koin di-drag di atasnya
    public float highlightAnimDuration = 0.15f;   // durasi animasi membesar/mengecil

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Coroutine scaleCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
    }

    // Dipanggil dari DraggableCoin.cs pas koin di-drop di sini.
    // Otomatis cek berdasarkan simbol ATAU warna, tergantung "Match By Color" di zona ini.
    public bool IsCorrectCoin(DraggableCoin coin)
    {
        if (matchByColor)
        {
            return coin.assignedColor == zoneColor;
        }
        else
        {
            return coin.coinType == zoneType;
        }
    }

    // Bikin zona membesar dikit (pop) pas koin lagi di-drag di atasnya, balik normal pas gak lagi
    public void SetHighlight(bool highlighted)
    {
        float target = highlighted ? highlightScaleMultiplier : 1f;
        ScaleTo(target, highlightAnimDuration);
    }

    private void ScaleTo(float targetScale, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale, duration));
    }

    private IEnumerator ScaleAnimation(float targetScale, float duration)
    {
        float startScale = rectTransform.localScale.x / baseScale.x;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float current = Mathf.Lerp(startScale, targetScale, progress);
            rectTransform.localScale = baseScale * current;
            yield return null;
        }

        rectTransform.localScale = baseScale * targetScale;
    }

    // Dipanggil dari DraggableCoin.cs kalau snapPoint kosong.
    // Koin TETAP di posisi drop-nya, tapi "didorong" secukupnya biar seluruh badannya
    // gak nyembul keluar dari kotak zona ini (bukan ditarik ke 1 titik tengah).
    public void ClampCoinInside(RectTransform coinRect)
    {
        RectTransform zoneRect = GetComponent<RectTransform>();

        Vector3[] zoneCorners = new Vector3[4];
        zoneRect.GetWorldCorners(zoneCorners); // [0]=kiri-bawah, [2]=kanan-atas

        Vector3[] coinCorners = new Vector3[4];
        coinRect.GetWorldCorners(coinCorners);
        float coinHalfWidth = (coinCorners[2].x - coinCorners[0].x) / 2f;
        float coinHalfHeight = (coinCorners[2].y - coinCorners[0].y) / 2f;

        float minX = zoneCorners[0].x + coinHalfWidth;
        float maxX = zoneCorners[2].x - coinHalfWidth;
        float minY = zoneCorners[0].y + coinHalfHeight;
        float maxY = zoneCorners[2].y - coinHalfHeight;

        // Kalau koin lebih besar dari zonanya (jarang, tapi jaga-jaga), paksa ke tengah biar gak kebalik-balik
        if (minX > maxX) { float mid = (minX + maxX) / 2f; minX = maxX = mid; }
        if (minY > maxY) { float mid = (minY + maxY) / 2f; minY = maxY = mid; }

        Vector3 pos = coinRect.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        coinRect.position = pos;
    }

    // Dipanggil dari GameManager.cs pas revalidasi koin abis zona geser (shuffle/flash).
    // Cek apakah suatu titik posisi (world space) lagi ada DI DALAM kotak zona ini.
    public bool ContainsWorldPosition(Vector3 worldPos)
    {
        RectTransform zoneRect = GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        zoneRect.GetWorldCorners(corners); // [0]=kiri-bawah, [2]=kanan-atas

        return worldPos.x >= corners[0].x && worldPos.x <= corners[2].x &&
               worldPos.y >= corners[0].y && worldPos.y <= corners[2].y;
    }
}