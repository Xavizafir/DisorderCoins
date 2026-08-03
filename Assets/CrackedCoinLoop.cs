using UnityEngine;

// Tempel script ini di GameObject PARENT yang isinya 2 potongan koin
// (misal parent "CrackedCoinFX", isinya child "CrackedCoins" & "CrackedCoins (1)").
// Drag ke-2 potongan itu ke field Left Half & Right Half di bawah.
public class CrackedCoinLoop : MonoBehaviour
{
    [Header("Potongan Koin")]
    public RectTransform leftHalf;
    public RectTransform rightHalf;

    [Header("Animasi Pecah")]
    public float splitDistance = 40f;   // seberapa jauh potongannya misah (pixel)
    public float splitRotation = 15f;   // seberapa miring tiap potongan pas misah (derajat)
    public float cycleDuration = 2f;    // durasi 1 siklus penuh (nyatu -> pecah -> nyatu lagi), detik

    private Vector2 leftOriginalPos;
    private Vector2 rightOriginalPos;
    private Quaternion leftOriginalRot;
    private Quaternion rightOriginalRot;

    void Start()
    {
        // Simpen posisi & rotasi awal (kondisi "nyatu") sebagai titik referensi
        leftOriginalPos = leftHalf.anchoredPosition;
        rightOriginalPos = rightHalf.anchoredPosition;
        leftOriginalRot = leftHalf.localRotation;
        rightOriginalRot = rightHalf.localRotation;
    }

    void Update()
    {
        float speed = (2f * Mathf.PI) / cycleDuration;

        // Sine wave 0 -> 1 -> 0, mulus tanpa "patahan" di titik balik (beda sama PingPong yang linear)
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

        // Geser tiap potongan menjauh dari tengah, sebanding sama t
        leftHalf.anchoredPosition = leftOriginalPos + new Vector2(-splitDistance * t, 0f);
        rightHalf.anchoredPosition = rightOriginalPos + new Vector2(splitDistance * t, 0f);

        // Miringin tiap potongan berlawanan arah, biar kesannya "terpelintir" pas pecah
        leftHalf.localRotation = leftOriginalRot * Quaternion.Euler(0f, 0f, splitRotation * t);
        rightHalf.localRotation = rightOriginalRot * Quaternion.Euler(0f, 0f, -splitRotation * t);
    }
}