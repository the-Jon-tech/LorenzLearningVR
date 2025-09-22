using UnityEngine;
using UnityEngine.UI;
using System.Collections; // nodig voor IEnumerator

public class StartScreen : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip GameStart;
    private AudioSource audioSource;

    [Header("Buttons")]
    public Button PlayButton;
    public Button ExitButton;

    [Header("Other Scripts")]
    public GameTimer gameTimer;
    public MouseLookController lookScript;
    public PlayerMovement _PlayerMovement;

    [Header("UI Fade")]
    public CanvasGroup canvasGroup;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlayButton.onClick.AddListener(OnPlayButtonClicked);

        // Zorg dat speler geen controle heeft voordat het spel start
        lookScript.enabled = false;
        _PlayerMovement.enabled = false;
    }

    void OnPlayButtonClicked()
    {
        StartCoroutine(StartGameSequence());
    }

    void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private IEnumerator StartGameSequence()
    {
        if (audioSource != null && GameStart != null)
        {
            audioSource.PlayOneShot(GameStart);
        }

        yield return StartCoroutine(FadeOutUI());

        gameTimer.StartTimer(); // LET OP: haakjes toegevoegd
        lookScript.enabled = true;
        _PlayerMovement.enabled = true;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutUI()
    {
        float duration = 2f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Debug.Log("UI fade out complete!");
    }
}
