using UnityEngine;
using UnityEngine.UI;

// Tempel script ini di GameObject Button manapun, di scene MANAPUN
// (Menu: Play, Quit / Gameplay: Restart, Menu, dll).
// Standalone — gak butuh GameManager atau script lain apapun buat jalan.
[RequireComponent(typeof(Button))]
public class ButtonClickSFX : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource; // drag AudioSource manapun yang ada di scene ini (boleh dipake bareng2 tombol lain)
    public AudioClip clickSound;

    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}