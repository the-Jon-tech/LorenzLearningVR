using UnityEngine;
using TMPro;
using System.Collections;

public class Trigger : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip endSound;
    [SerializeField] private AudioClip normalSound;
    private AudioSource audioSource;

    private bool hasTriggered = false;
    public GameTimer gameTimer;

    [Header("End page Settings")]
    public GameObject endUI;
    public TextMeshProUGUI timePlayer;
    private CanvasGroup canvasGroup;


    [Header ("Player interaction")]
    public MouseLookController lookScript;
    public PlayerMovement _PlayerMovement;


    private void Start()
    {
        canvasGroup = endUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("No CanvasGroup found on endUI!");
        }

        // Keep the UI active but invisible
        endUI.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("No AudioSource found on this GameObject!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (audioSource != null && endSound != null)
            {
                audioSource.Stop();

                audioSource.PlayOneShot(endSound);
                gameTimer.CompleteGame();

                float endTime = gameTimer.ElapsedTime;
                int minutes = Mathf.FloorToInt(endTime / 60f);
                int seconds = Mathf.FloorToInt(endTime % 60f);
                timePlayer.text = $"You have completed the game in : {minutes:00}:{seconds:00}";
                StartCoroutine(FadeInUI());

                if (lookScript != null)
                    lookScript.enabled = false;
                if (_PlayerMovement != null)
                    _PlayerMovement.enabled = false;




                //Debug.Log("Trigger activated! Playing sound and fading in UI.");
            }
            else
            {
                Debug.LogWarning("AudioSource or endSound is missing!");
            }
        }
    }

    private IEnumerator FadeInUI()
    {
        float duration = 2f;
        float time = 0f;

        // Ensure UI is ready for interaction after fade
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        Debug.Log("UI fade in complete!");
    }
}