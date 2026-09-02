using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// Tempel script ini di GameObject kosong di scene SelectMode (misal "SelectModeManager").
public class SelectModeManager : MonoBehaviour
{
    [System.Serializable]
    public class GameModeOption
    {
        public string modeName;
        public string sceneName;
        public GameObject previewObject;
    }

    [Header("Daftar Mode (urutan sesuai urutan carousel)")]
    public List<GameModeOption> modes;

    [Header("UI")]
    public TMP_Text modeNameText;

    [Header("Tombol")]
    public Button arrowLeftButton;
    public Button arrowRightButton;
    public Button startButton;
    public Button backButton;

    [Header("Efek Slide Transisi")]
    public float slideDistance = 900f;
    public float textSlideDistance = 400f;
    public float slideDuration = 0.35f;

    [Header("Efek Motion Blur (Afterimage Trail)")]
    public bool enableMotionBlurTrail = true;
    public float ghostSpawnInterval = 0.04f; // makin kecil, makin rapat jejaknya (makin berat)
    public float ghostFadeDuration = 0.2f;   // seberapa lama tiap jejak bertahan sebelum ilang
    public float ghostStartAlpha = 0.35f;    // transparansi awal tiap jejak

    [Header("Scene Navigation")]
    public string menuSceneName = "Menu";

    private int currentIndex = 0;
    private bool isTransitioning = false;
    private List<Vector2> basePositions = new List<Vector2>();
    private Vector2 textBasePos;

    void Start()
    {
        if (arrowLeftButton != null) arrowLeftButton.onClick.AddListener(PreviousMode);
        if (arrowRightButton != null) arrowRightButton.onClick.AddListener(NextMode);
        if (startButton != null) startButton.onClick.AddListener(StartSelectedMode);
        if (backButton != null) backButton.onClick.AddListener(BackToMenu);

        foreach (GameModeOption mode in modes)
        {
            Vector2 pos = Vector2.zero;
            if (mode.previewObject != null)
            {
                pos = mode.previewObject.GetComponent<RectTransform>().anchoredPosition;
            }
            basePositions.Add(pos);
        }

        if (modeNameText != null)
        {
            textBasePos = modeNameText.rectTransform.anchoredPosition;
        }

        for (int i = 0; i < modes.Count; i++)
        {
            if (modes[i].previewObject != null) modes[i].previewObject.SetActive(i == currentIndex);
        }
        if (modeNameText != null && modes.Count > 0)
        {
            modeNameText.text = modes[currentIndex].modeName;
        }
    }

    private void PreviousMode()
    {
        if (modes.Count == 0 || isTransitioning) return;
        int newIndex = (currentIndex - 1 + modes.Count) % modes.Count;
        StartCoroutine(TransitionTo(newIndex, -1));
    }

    private void NextMode()
    {
        if (modes.Count == 0 || isTransitioning) return;
        int newIndex = (currentIndex + 1) % modes.Count;
        StartCoroutine(TransitionTo(newIndex, 1));
    }

    private IEnumerator TransitionTo(int newIndex, int direction)
    {
        isTransitioning = true;

        GameObject oldPreview = modes[currentIndex].previewObject;
        GameObject newPreview = modes[newIndex].previewObject;
        RectTransform oldRect = oldPreview != null ? oldPreview.GetComponent<RectTransform>() : null;
        RectTransform newRect = newPreview != null ? newPreview.GetComponent<RectTransform>() : null;

        Vector2 oldBasePos = basePositions[currentIndex];
        Vector2 newBasePos = basePositions[newIndex];
        Vector2 newStartPos = newBasePos + new Vector2(slideDistance * direction, 0f);
        Vector2 oldEndPos = oldBasePos - new Vector2(slideDistance * direction, 0f);

        if (newRect != null)
        {
            newPreview.SetActive(true);
            newRect.anchoredPosition = newStartPos;
        }

        RectTransform textRect = modeNameText != null ? modeNameText.rectTransform : null;
        Vector2 textExitPos = textBasePos - new Vector2(textSlideDistance * direction, 0f);
        Vector2 textEntryPos = textBasePos + new Vector2(textSlideDistance * direction, 0f);
        bool textSwapped = false;

        float t = 0f;
        float timeSinceLastGhost = 0f;

        while (t < slideDuration)
        {
            t += Time.deltaTime;
            timeSinceLastGhost += Time.deltaTime;
            float progress = Mathf.Clamp01(t / slideDuration);
            float eased = 1f - (1f - progress) * (1f - progress);

            if (oldRect != null) oldRect.anchoredPosition = Vector2.Lerp(oldBasePos, oldEndPos, eased);
            if (newRect != null) newRect.anchoredPosition = Vector2.Lerp(newStartPos, newBasePos, eased);

            if (textRect != null)
            {
                if (progress < 0.5f)
                {
                    float localProgress = progress / 0.5f;
                    float localEased = 1f - (1f - localProgress) * (1f - localProgress);
                    textRect.anchoredPosition = Vector2.Lerp(textBasePos, textExitPos, localEased);
                }
                else
                {
                    if (!textSwapped)
                    {
                        modeNameText.text = modes[newIndex].modeName;
                        textRect.anchoredPosition = textEntryPos;
                        textSwapped = true;
                    }

                    float localProgress = (progress - 0.5f) / 0.5f;
                    float localEased = 1f - (1f - localProgress) * (1f - localProgress);
                    textRect.anchoredPosition = Vector2.Lerp(textEntryPos, textBasePos, localEased);
                }
            }

            // Nyisain "jejak" transparan tiap sekian detik, buat efek motion blur murah-meriah
            if (enableMotionBlurTrail && timeSinceLastGhost >= ghostSpawnInterval)
            {
                timeSinceLastGhost = 0f;
                if (oldRect != null) SpawnGhost(oldRect);
                if (newRect != null) SpawnGhost(newRect);
                if (textRect != null) SpawnGhost(textRect);
            }

            yield return null;
        }

        if (oldRect != null) oldRect.anchoredPosition = oldBasePos;
        if (oldPreview != null) oldPreview.SetActive(false);
        if (newRect != null) newRect.anchoredPosition = newBasePos;

        if (textRect != null)
        {
            if (!textSwapped) modeNameText.text = modes[newIndex].modeName;
            textRect.anchoredPosition = textBasePos;
        }

        currentIndex = newIndex;
        isTransitioning = false;
    }

    // Bikin 1 duplikat transparan dari objek yang lagi geser, di posisinya SEKARANG,
    // terus fade out & hancurin dirinya sendiri — kumpulan jejak ini yang bikin efek "motion blur"
    private void SpawnGhost(RectTransform source)
    {
        GameObject ghost = Instantiate(source.gameObject, source.parent);
        ghost.name = source.name + "_Ghost";

        // Taruh SEBELUM objek aslinya di Hierarchy, biar ke-render di BELAKANG (gak nutupin yang asli)
        ghost.transform.SetSiblingIndex(source.GetSiblingIndex());

        RectTransform ghostRect = ghost.GetComponent<RectTransform>();
        ghostRect.anchoredPosition = source.anchoredPosition;
        ghostRect.sizeDelta = source.sizeDelta;
        ghostRect.localScale = source.localScale;

        // Matiin semua interaksi di ghost (biar gak numpuk kena klik / drag / dsb)
        CanvasGroup cg = ghost.GetComponent<CanvasGroup>();
        if (cg == null) cg = ghost.AddComponent<CanvasGroup>();
        cg.alpha = ghostStartAlpha;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        Destroy(ghost, ghostFadeDuration);
        StartCoroutine(FadeGhost(cg));
    }

    private IEnumerator FadeGhost(CanvasGroup cg)
    {
        float startAlpha = cg != null ? cg.alpha : 0f;
        float t = 0f;

        while (t < ghostFadeDuration && cg != null)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, t / ghostFadeDuration);
            yield return null;
        }
    }

    private void StartSelectedMode()
    {
        if (modes.Count == 0 || isTransitioning) return;
        LoadSceneWithTransition(modes[currentIndex].sceneName);
    }

    private void BackToMenu()
    {
        if (isTransitioning) return;
        LoadSceneWithTransition(menuSceneName);
    }

    private void LoadSceneWithTransition(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}