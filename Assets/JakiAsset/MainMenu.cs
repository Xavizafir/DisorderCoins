using UnityEngine;
using UnityEngine.SceneManagement; // Wajib dipanggil untuk pindah scene

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Pindah ke scene Gameplay (pastikan nama scene sesuai)
        SceneManager.LoadScene("MainGameplay");
    }

    public void QuitGame()
    {
        // Keluar dari aplikasi game
        Application.Quit();
        Debug.Log("Game Quit"); // Untuk mengecek di Editor Unity bahwa fungsi dipanggil
    }
}