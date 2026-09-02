using UnityEngine;
using UnityEngine.SceneManagement; // Wajib dipanggil untuk pindah scene

public class MainMenu : MonoBehaviour
{
    void Awake()
    {
        // Setiap kali balik/masuk ke scene Menu, anggap "sesi baru" —
        // tutorial bakal muncul lagi kalau nanti player akhirnya masuk ke gameplay
        TutorialManager.ResetTutorialFlag();
    }

    // Hubungin ke tombol "Play" di menu utama — sekarang arahnya ke scene SelectMode,
    // bukan langsung ke MainGameplay lagi
    public void PlayGame()
    {
        LoadSceneWithTransition("SelectMode");
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