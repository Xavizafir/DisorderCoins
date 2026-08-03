using UnityEngine;
using UnityEngine.SceneManagement; // Wajib dipanggil untuk pindah scene

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Pindah ke scene Gameplay pakai transisi fade (kalau manager-nya ada di scene)
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene("MainGameplay");
        }
        else
        {
            // Fallback: kalau SceneTransitionManager belum ke-setup, pindah langsung tanpa transisi
            SceneManager.LoadScene("MainGameplay");
        }
    }

    public void QuitGame()
    {
        // Keluar dari aplikasi game
        Application.Quit();
        Debug.Log("Game Quit"); // Untuk mengecek di Editor Unity bahwa fungsi dipanggil
    }
}