using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Tempel script ini di GameObject kosong (misal "SceneTransitionManager"), taruh SEBAGAI CHILD
// dari sebuah Canvas yang isinya 1 Image full-screen (drag Image itu ke field Fade Canvas Group,
// tapi field-nya tipe CanvasGroup, jadi tempel CanvasGroup component di Image itu dulu).
//
// PENTING: taruh GameObject ini (yang isi Canvas + Image + script ini) di KEDUA scene
// (Menu dan MainGameplay) — singleton check di bawah bakal otomatis hapus yang duplikat,
// jadi cuma 1 yang aktif & persist lintas scene lewat DontDestroyOnLoad.
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Fade Settings")]
    public CanvasGroup fadeCanvasGroup; // CanvasGroup di Image full-screen (warna bebas, sesuaikan di Inspector Image-nya)
    public float fadeDuration = 0.5f;

    void Awake()
    {
        // WAJIB reset FadeOverlay lokal scene ini ke kondisi aman DULUAN, SEBELUM cek duplicate.
        // Kalau dibalik urutannya (cek duplicate dulu baru reset), instance yang ternyata
        // duplicate bakal langsung ke-Destroy tanpa sempet reset FadeOverlay-nya sendiri —
        // ninggalin FadeOverlay itu numpuk di layar dengan kondisi blocksRaycasts=true selamanya,
        // nutupin klik ke semua tombol di scene itu.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        // Singleton: kalau udah ada instance lain (dari scene sebelumnya yang persist), hapus yang baru
        if (Instance != null && Instance != this)
        {
            // PENTING: FadeOverlay punya instance lama itu udah ke-hapus (scene lamanya di-unload),
            // jadi "serahterimakan" FadeOverlay versi scene INI (yang masih fresh & valid) ke instance
            // yang persist. Tanpa ini, fade cuma jalan SEKALI doang (di transisi pertama), abis itu
            // instance yang persist bakal terus nyoba pake referensi FadeOverlay yang udah gak ada.
            if (fadeCanvasGroup != null)
            {
                Instance.fadeCanvasGroup = fadeCanvasGroup;
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Panggil ini dari tombol Play / Restart / Menu, gantiin SceneManager.LoadScene() biasa
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.blocksRaycasts = true; // block klik selama transisi

        yield return StartCoroutine(FadeTo(1f)); // fade nutup layar

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
        yield return loadOp; // tunggu scene baru selesai load di balik layar yang ketutup

        yield return StartCoroutine(FadeTo(0f)); // fade buka layar lagi

        if (fadeCanvasGroup != null) fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}