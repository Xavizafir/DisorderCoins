using UnityEngine;
using UnityEngine.SceneManagement; // Wajib dipanggil untuk pindah scene

public class MainMenu : MonoBehaviour
{
    [Header("Panel Pilih Game Mode")]
    public GameObject chooseGMPanel; // drag ChooseGMPanel ke sini

    void Awake()
    {
        // Setiap kali balik/masuk ke scene Menu, anggap "sesi baru" —
        // tutorial bakal muncul lagi kalau nanti player pencet Play
        TutorialManager.ResetTutorialFlag();

        // Pastiin panel pilih mode ketutup di awal
        if (chooseGMPanel != null) chooseGMPanel.SetActive(false);
    }

    // Hubungin ke tombol "Mode" di menu utama
    public void OpenChooseGMPanel()
    {
        if (chooseGMPanel != null) chooseGMPanel.SetActive(true);
    }

    // Hubungin ke tombol "Back" di dalam ChooseGMPanel
    public void CloseChooseGMPanel()
    {
        if (chooseGMPanel != null) chooseGMPanel.SetActive(false);
    }

    public void PlayGame()
    {
        LoadSceneWithTransition("MainGameplay");
    }

    // Hubungin ke tombol "GM1Button" di dalam ChooseGMPanel
    public void PlayGameMode1()
    {
        LoadSceneWithTransition("ModeGameplay2");
    }

    // Hubungin ke tombol "GM2Button" di dalam ChooseGMPanel
    public void PlayGameMode2()
    {
        LoadSceneWithTransition("ModeGameplay3");
    }

    // Method umum buat pindah scene pakai transisi fade (kalau manager-nya ada di scene)
    private void LoadSceneWithTransition(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            // Fallback: kalau SceneTransitionManager belum ke-setup, pindah langsung tanpa transisi
            SceneManager.LoadScene(sceneName);
        }
    }

    public void QuitGame()
    {
        // Keluar dari aplikasi game
        Application.Quit();
        Debug.Log("Game Quit"); // Untuk mengecek di Editor Unity bahwa fungsi dipanggil
    }
}