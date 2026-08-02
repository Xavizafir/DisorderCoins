using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

// Tempel script ini di GameObject kosong (misal "GameOverUI"), lalu drag Panel-nya
// (yang punya CanvasGroup) ke field "Panel Canvas Group" di bawah.
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [Header("Panel (harus punya CanvasGroup component)")]
    public CanvasGroup panelCanvasGroup;
    public float fadeDuration = 0.5f;

    [Header("Texts")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText;

    [Header("Scene Navigation")]
    public string menuSceneName = "Menu"; // sesuaikan sama nama scene menu lo

    private const string HighScoreKey = "DisorderCoins_HighScore";

    void Awake()
    {
        Instance = this;

        // Panel disembunyiin total di awal (invisible + gak bisa diklik)
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.gameObject.SetActive(false);
    }

    // Dipanggil dari GameManager.cs pas waktu abis
    public void ShowGameOver(int score)
    {
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (scoreText != null) scoreText.text = "Score: " + score;
        if (highScoreText != null) highScoreText.text = "High Score: " + highScore;

        panelCanvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        panelCanvasGroup.alpha = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    // Hubungin ini ke OnClick() tombol Restart di Inspector
    public void OnRestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Hubungin ini ke OnClick() tombol Menu di Inspector
    public void OnMenuButton()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}