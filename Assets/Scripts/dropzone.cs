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
}