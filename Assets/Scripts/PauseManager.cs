using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

// Tempel script ini di GameObject kosong (misal "PauseManager"). Drag panel pause
// (yang punya CanvasGroup) ke field "Pause Canvas Group" di bawah.
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Panel (harus punya CanvasGroup component)")]
    public CanvasGroup pauseCanvasGroup;
    public float fadeDuration = 0.3f;

    [Header("Scene Navigation")]
    public string menuSceneName = "Menu";

    [Header("Background Blur (opsional, teknik sama kayak GameOverUI)")]
    public RawImage blurBackgroundImage;
    public Camera captureCamera;
    public Material blurMaterial;
    public int blurIterations = 3;

    private bool isPaused = false;

    void Awake()
    {
        Instance = this;

        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        // Jangan bisa buka Pause kalau udah Game Over (panel "YOU LOSE" lagi tampil)
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        // Jangan bisa buka Pause juga kalau tutorial masih kebuka
        if (TutorialManager.IsTutorialActive) return;

        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;

        // Capture & blur SEBELUM panel pause diaktifin, sama kayak logic GameOverUI
        if (blurBackgroundImage != null && captureCamera != null && blurMaterial != null)
        {
            Texture2D blurredTexture = CaptureAndBlur();
            blurBackgroundImage.texture = blurredTexture;
            blurBackgroundImage.gameObject.SetActive(true);
        }

        pauseCanvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeIn());

        GameManager.IsInputFrozen = true; // extra safety, block drag koin selama pause
        Time.timeScale = 0f; // freeze semua Update()/coroutine yang pakai Time.deltaTime biasa
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        GameManager.IsInputFrozen = false;

        StartCoroutine(FadeOutAndHide());

        if (blurBackgroundImage != null) blurBackgroundImage.gameObject.SetActive(false);
    }

    private Texture2D CaptureAndBlur()
    {
        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture prevTarget = captureCamera.targetTexture;
        captureCamera.targetTexture = rt;
        captureCamera.Render();
        captureCamera.targetTexture = prevTarget;

        RenderTexture current = rt;
        for (int i = 0; i < blurIterations; i++)
        {
            RenderTexture next = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(current, next, blurMaterial);
            RenderTexture.ReleaseTemporary(current);
            current = next;
        }

        RenderTexture.active = current;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = null;

        RenderTexture.ReleaseTemporary(current);
        return result;
    }

    // Pakai Time.unscaledDeltaTime, BUKAN Time.deltaTime — biar animasi fade-nya sendiri
    // tetep jalan normal walaupun Time.timeScale lagi 0 (game dalam kondisi paused)
    private IEnumerator FadeIn()
    {
        float t = 0f;
        pauseCanvasGroup.alpha = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            pauseCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        pauseCanvasGroup.alpha = 1f;
        pauseCanvasGroup.interactable = true;
        pauseCanvasGroup.blocksRaycasts = true;
    }

    private IEnumerator FadeOutAndHide()
    {
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;

        float startAlpha = pauseCanvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / fadeDuration);
            yield return null;
        }

        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.gameObject.SetActive(false);
    }

    // Hubungin ke OnClick() tombol Resume
    public void OnResumeButton()
    {
        ResumeGame();
    }

    // Hubungin ke OnClick() tombol Menu
    public void OnMenuButton()
    {
        Time.timeScale = 1f; // WAJIB reset dulu sebelum pindah scene, biar scene Menu gak ikut freeze
        GameManager.IsInputFrozen = false;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(menuSceneName);
        }
        else
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}