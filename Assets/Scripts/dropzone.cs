using UnityEngine;
using UnityEngine.UI;

// Tempel script ini di tiap GameObject "Place" (IndoPlace, ChinaPlace, USPlace, EuropePlace)
// Set "Zone Type" di Inspector sesuai jenis zonanya.
[RequireComponent(typeof(Image))]
public class DropZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public CoinType zoneType;

    [Header("Snap Settings")]
    public RectTransform snapPoint; // opsional: kalau mau koin snap ke titik tengah zona, drag RectTransform zona ini ke sini

    [Header("Highlight Settings")]
    public float highlightBrightness = 1.5f; // seberapa terang pas koin di-hover di atas zona ini

    private Image image;
    private Color originalColor;

    void Awake()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
    }

    // Dipanggil dari DraggableCoin.cs pas koin di-drop di sini
    public bool IsCorrectCoin(CoinType coinType)
    {
        return coinType == zoneType;
    }

    // Nyalain warna lebih terang, dipanggil DraggableCoin pas koin lagi di-drag di atas zona ini
    public void SetHighlight(bool highlighted)
    {
        if (highlighted)
        {
            image.color = new Color(
                Mathf.Min(originalColor.r * highlightBrightness, 1f),
                Mathf.Min(originalColor.g * highlightBrightness, 1f),
                Mathf.Min(originalColor.b * highlightBrightness, 1f),
                originalColor.a
            );
        }
        else
        {
            image.color = originalColor;
        }
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
}