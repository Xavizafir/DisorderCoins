using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Tempel script ini di GameObject kosong (misal "TutorialManager").
// Struktur yang dibutuhin:
//   TutorialPanelRoot (kotak putih background)
//    ├── Page0 (isi gambar + teks penjelasan halaman 1)
//    ├── Page1 (isi gambar + teks penjelasan halaman 2)
//    ├── Page2 (dst...)
//    ├── DotsContainer
//    │    ├── Dot0 (Image lingkaran kecil)
//    │    ├── Dot1
//    │    └── Dot2
//    ├── NextButton (teks "NEXT", berubah jadi "START" di halaman terakhir)
//    └── SkipAllButton
public class TutorialManager : MonoBehaviour
{
    // Dicek dari PauseManager.cs, biar ESC gak bisa dipencet pas tutorial masih kebuka
    public static bool IsTutorialActive = false;

    // Sekali tutorial ditampilin dalam 1 sesi (dari Menu -> Play), gak bakal muncul lagi
    // walau player Restart berkali-kali. Flag ini di-reset dari MainMenu.cs tiap kali
    // player masuk scene Menu, jadi bakal muncul lagi kalau player balik ke Menu dulu.
    private static bool hasShownTutorial = false;

    public static void ResetTutorialFlag()
    {
        hasShownTutorial = false;
    }

    [Header("Panel Utama")]
    public GameObject tutorialPanelRoot;
    public CanvasGroup tutorialCanvasGroup; // buat efek fade, taruh di GameObject yang sama kayak Tutorial Panel Root
    public float fadeDuration = 0.4f;

    [Header("Halaman Tutorial (urutan sesuai urutan tampil)")]
    public List<GameObject> pages;

    [Header("Page Indicator (titik kecil, urutan HARUS sama kayak Pages)")]
    public List<Image> pageDots;
    public Color activeDotColor = Color.red;
    public Color inactiveDotColor = Color.black;

    [Header("Tombol")]
    public Button nextButton;
    public Button skipAllButton;
    public TMP_Text nextButtonText; // opsional: teksnya berubah di halaman terakhir
    public string normalButtonLabel = "NEXT";
    public string lastPageButtonLabel = "START";

    private int currentPageIndex = 0;

    void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (skipAllButton != null) skipAllButton.onClick.AddListener(OnSkipAllClicked);

        // Udah pernah liat tutorial di sesi ini (misal abis Restart) — langsung skip,
        // gameplay lanjut normal tanpa nampilin tutorial lagi
        if (hasShownTutorial)
        {
            if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);
            IsTutorialActive = false;
            Time.timeScale = 1f;
            GameManager.IsInputFrozen = false;
            return;
        }

        ShowTutorial();
        hasShownTutorial = true;
    }

    private void ShowTutorial()
    {
        currentPageIndex = 0;
        UpdatePageDisplay();

        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(true);

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = 1f;
            tutorialCanvasGroup.interactable = true;
            tutorialCanvasGroup.blocksRaycasts = true;
        }

        IsTutorialActive = true;

        // Pause gameplay selama tutorial ditampilin — teknik sama kayak PauseManager
        Time.timeScale = 0f;
        GameManager.IsInputFrozen = true;
    }

    private void UpdatePageDisplay()
    {
        // Nyalain cuma halaman yang lagi aktif, matiin sisanya
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null) pages[i].SetActive(i == currentPageIndex);
        }

        // Update warna titik indikator
        for (int i = 0; i < pageDots.Count; i++)
        {
            if (pageDots[i] != null)
            {
                pageDots[i].color = (i == currentPageIndex) ? activeDotColor : inactiveDotColor;
            }
        }

        // Ganti label tombol Next jadi "START" di halaman terakhir
        if (nextButtonText != null)
        {
            bool isLastPage = currentPageIndex == pages.Count - 1;
            nextButtonText.text = isLastPage ? lastPageButtonLabel : normalButtonLabel;
        }
    }

    private void OnNextClicked()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
        else
        {
            CloseTutorial();
        }
    }

    private void OnSkipAllClicked()
    {
        CloseTutorial();
    }

    private void CloseTutorial()
    {
        StartCoroutine(FadeOutAndClose());
    }

    // Pakai Time.unscaledDeltaTime — animasinya tetep jalan normal walau Time.timeScale masih 0
    private IEnumerator FadeOutAndClose()
    {
        // Matiin interaksi begitu proses fade mulai, biar gak bisa diklik dobel
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }

        if (tutorialCanvasGroup != null)
        {
            float startAlpha = tutorialCanvasGroup.alpha;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                tutorialCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
                yield return null;
            }

            tutorialCanvasGroup.alpha = 0f;
        }

        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);

        IsTutorialActive = false;

        // Baru sekarang gameplay beneran mulai jalan, setelah fade selesai total
        Time.timeScale = 1f;
        GameManager.IsInputFrozen = false;
    }
}