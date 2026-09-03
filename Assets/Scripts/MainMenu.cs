using UnityEngine;
using UnityEngine.SceneManagement; // Wajib dipanggil untuk pindah scene

public class MainMenu : MonoBehaviour
{
    [Header("Panel Credit")]
    public GameObject creditPanel; // drag panel Credit (yang isi CreditBG, dkk) ke sini

    void Awake()
    {
        // Setiap kali balik/masuk ke scene Menu, anggap "sesi baru" —
        // tutorial bakal muncul lagi kalau nanti player akhirnya masuk ke gameplay
        TutorialManager.ResetTutorialFlag();

        // Pastiin panel Credit ketutup di awal
        if (creditPanel != null) creditPanel.SetActive(false);
    }

    // Hubungin ke tombol "Play" di menu utama — sekarang arahnya ke scene SelectMode,
    // bukan langsung ke MainGameplay lagi
    public void PlayGame()
    {
        LoadSceneWithTransition("SelectMode");
    }

    // Hubungin ke tombol "Credit" di menu utama
    public void OpenCreditPanel()
    {
        if (creditPanel != null) creditPanel.SetActive(true);
    }

    // Hubungin ke tombol "Back" di dalam panel Credit
    public void CloseCreditPanel()
    {
        if (creditPanel != null) creditPanel.SetActive(false);
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