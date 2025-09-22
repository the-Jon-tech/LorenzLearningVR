using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;

namespace InteractableSystem
{
    public class BookInteractable : InteractableObject
    {
        [Header("Player Interaction")]
        public PlayerInteraction playerInteraction;

        [Header("Book Settings")]
        public GameObject bookUI;
        public string bookTitle = "Untitled Book";
        public string[] bookPages = new string[3]; // Fixed to 3 pages
        public TextMeshProUGUI bookTitleText;
        public TextMeshProUGUI bookPageText;
        public TextMeshProUGUI bookWhatPageText;
        public TextMeshProUGUI Completed_Books;
        public Slider StamBarSlider;
        private bool isInteractionLocked = false;

        [Header("Answer Buttons")]
        public Button answerButton1;
        public Button answerButton2;
        public Button answerButton3;

        [Header("Answer Texts for Each Page")]
        public string[] answerTexts1 = new string[3];
        public string[] answerTexts2 = new string[3];
        public string[] answerTexts3 = new string[3];

        [Header("Correct Answers (1-3 for each page)")]
        public int[] correctAnswers = new int[3];

        [Header("Feedback")]
        public GameObject resultPanel;

        [Header("Vanish Settings")]
        public bool vanishAfterCompletion = true;
        public VanishType vanishType = VanishType.FadeOut;
        public float vanishDuration = 1f;
        public bool destroyAfterVanish = false;
        public AudioClip vanishSound;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip BookOpen;
        public AudioClip BookClose;
        public AudioClip BookComplete;
        public AudioClip BookFailed;
        public AudioClip BookPageFlipped;
        public AudioClip ExitDoorOpen;

        [Header("Scrips")]
        public MouseLookController lookScript;
        public PlayerMovement _PlayerMovement;
        public BookFloater _MeshRotator;

        private int currentPage = 0;
        private bool isBookOpen = false;
        private bool[] answeredCorrectly = new bool[3];
        private bool[] hasAnswered = new bool[3];
        private static int _CompletedBooks = 0; 

        // Vanish-related variables
        private Renderer objRenderer;
        private Material originalMaterial;
        private Collider objCollider;
        private bool isVanishing = false;
        private bool hasVanished = false;
        private bool isFinished = false;
        

        
        private static BookInteractable currentlyOpenBook = null;

        public enum VanishType
        {
            Instant,
            FadeOut,
            ScaleDown,
            FadeAndScale,
            Dissolve
        }

        private void Start()
        {
            // Initialize the interaction prompt
            UpdateInteractionPrompt();
            //Debug.Log("BookInteractable Start() - Set interaction prompt to: '" + interactionPrompt + "'");

            if (bookUI != null)
                bookUI.SetActive(false);
            if (resultPanel != null)
                resultPanel.SetActive(false);
            if (lookScript == null)
                lookScript = FindObjectOfType<MouseLookController>();

            // Update completed books counter for all books
            UpdateCompletedBooksDisplay();
            SetupButtonEvents();

            // Initialize vanish components
            objRenderer = GetComponent<Renderer>();
            objCollider = GetComponent<Collider>();

            if (objRenderer != null)
            {
                originalMaterial = objRenderer.material;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        
        private void UpdateInteractionPrompt()
        {
            if (hasVanished)
            {
                interactionPrompt = ""; // No interaction for vanished books
            }
            else if (isBookOpen)
            {
                interactionPrompt = ""; // No prompt while book is open
            }
            else
            {
                interactionPrompt = "Read " + bookTitle;
            }

            //Debug.Log($"Updated interaction prompt for {bookTitle}: '{interactionPrompt}'");
        }

       
        public override string GetInteractionPrompt()
        {
            UpdateInteractionPrompt();
            return interactionPrompt;
        }

        private void Update()
        {
            // Handle escape key to close book
            if (isBookOpen && currentlyOpenBook == this && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseBook();
                ResetQuiz();
                Debug.Log("Book closed via Escape: " + bookTitle);
            }
        }

        private void UpdateCompletedBooksDisplay()
        {
            if (Completed_Books != null)
                Completed_Books.text = $"{_CompletedBooks} / 5 books completed";
        }

        private void SetupButtonEvents()
        {
            if (answerButton1 != null)
                answerButton1.onClick.AddListener(() => OnAnswerButtonClick(1));
            if (answerButton2 != null)
                answerButton2.onClick.AddListener(() => OnAnswerButtonClick(2));
            if (answerButton3 != null)
                answerButton3.onClick.AddListener(() => OnAnswerButtonClick(3));
        }

        public override void Interact()
        {
            if (hasVanished) return; // Can't interact with vanished book

            // Close any currently open book before opening this one
            if (currentlyOpenBook != null && currentlyOpenBook != this)
            {
                currentlyOpenBook.CloseBook();
            }

            if (bookUI != null)
            {
                if (isBookOpen)
                {
                    CloseBook();
                }
                else
                {
                    OpenBook();
                }
            }
        }

        private void OpenBook()
        {
            // Set this book as the currently open book
            currentlyOpenBook = this;

            isBookOpen = true;
            bookUI.SetActive(true);

            if (_MeshRotator != null)
                _MeshRotator.enabled = false;


            if (lookScript != null)
                lookScript.enabled = false;
            if (_PlayerMovement != null)
                _PlayerMovement.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Completed_Books != null)
                Completed_Books.gameObject.SetActive(false);

            if (StamBarSlider != null)
            {
                StamBarSlider.gameObject.SetActive(false);
            }

            audioSource.PlayOneShot(BookOpen);
            currentPage = 0;
            DisplayCurrentPage();

            // Update interaction prompt (will be empty while book is open)
            UpdateInteractionPrompt();

            Debug.Log("Book opened: " + bookTitle);
        }

        public void CloseBook()
        {
            ResetQuiz();
            audioSource.PlayOneShot(BookClose);

            // Clear the currently open book reference if it's this book
            if (currentlyOpenBook == this)
            {
                currentlyOpenBook = null;
            }

            isBookOpen = false;
            bookUI.SetActive(false);
            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (lookScript != null)
                lookScript.enabled = true;
            if (_PlayerMovement != null)
                _PlayerMovement.enabled = true;

            if (_MeshRotator != null)
                _MeshRotator.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (Completed_Books != null)
                Completed_Books.gameObject.SetActive(true);
            if (StamBarSlider != null)
                StamBarSlider.gameObject.SetActive(true);

            // Update interaction prompt (will be "Read [BookTitle]" after closing)
            UpdateInteractionPrompt();

            // Force update the interaction text with a small delay to ensure timing
            if (playerInteraction != null)
            {
                StartCoroutine(ForceUpdateAfterDelay());
            }

            Debug.Log("Book closed: " + bookTitle + " - Prompt set to: '" + interactionPrompt + "'");
        }

        // NEW: Coroutine to force update with proper timing
        private IEnumerator ForceUpdateAfterDelay()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            playerInteraction.ForceUpdateInteractionText();
            Debug.Log("ForceUpdateInteractionText called for: " + bookTitle);
        }

        public void NextPage()
        {
            isInteractionLocked = false;

            // Only proceed if this is the currently open book
            if (currentlyOpenBook != this) return;

            if (currentPage < bookPages.Length - 1)
            {
                currentPage++;
                audioSource.PlayOneShot(BookPageFlipped);
                DisplayCurrentPage();
            }
            else
            {
                Debug.Log("Last page reached, showing results");
                ShowResults();
                return;
            }
        }

        private void DisplayCurrentPage()
        {
            // Only update UI if this is the currently open book
            if (currentlyOpenBook != this) return;

            if (bookUI != null)
            {
                if (bookTitleText != null)
                {
                    bookTitleText.text = bookTitle;
                    bookWhatPageText.text = $"Page {currentPage + 1}/3";
                }
                if (bookPageText != null && bookPages.Length > 0)
                {
                    bookPageText.text = bookPages[currentPage];
                }

                UpdateButtonTexts();
            }
        }

        private void UpdateButtonTexts()
        {
            if (answerButton1 != null && answerTexts1.Length > currentPage)
            {
                TextMeshProUGUI buttonText = answerButton1.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = answerTexts1[currentPage];
            }
            if (answerButton2 != null && answerTexts2.Length > currentPage)
            {
                TextMeshProUGUI buttonText = answerButton2.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = answerTexts2[currentPage];
            }
            if (answerButton3 != null && answerTexts3.Length > currentPage)
            {
                TextMeshProUGUI buttonText = answerButton3.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = answerTexts3[currentPage];
            }
        }

        private void OnAnswerButtonClick(int buttonNumber)
        {
            // Only proceed if this is the currently open book
            if (currentlyOpenBook != this || isInteractionLocked) return;

            Debug.Log($"Answer button {buttonNumber} clicked on page {currentPage + 1} for book: {bookTitle}");

            hasAnswered[currentPage] = true;
            bool isCorrect = (buttonNumber == correctAnswers[currentPage]);
            answeredCorrectly[currentPage] = isCorrect;

            string selectedAnswer = "";
            switch (buttonNumber)
            {
                case 1:
                    selectedAnswer = answerTexts1[currentPage];
                    break;
                case 2:
                    selectedAnswer = answerTexts2[currentPage];
                    break;
                case 3:
                    selectedAnswer = answerTexts3[currentPage];
                    break;
            }

            Debug.Log($"Button clicked: {buttonNumber}, Correct answer for page {currentPage}: {correctAnswers[currentPage]}");

            if (isCorrect && answeredCorrectly[currentPage])
            {
                if (hasAnswered[currentPage])
                {
                    isInteractionLocked = true; // Prevent further input
                    Invoke("NextPage", 0.5f);
                }
            }
            else
            {
                isInteractionLocked = true; // Prevent further input
                audioSource.PlayOneShot(BookFailed);
                CloseBook();
            }
        }


        private async Task ShowResults()
        {
            // Only proceed if this is the currently open book
            if (currentlyOpenBook != this) return;

            Debug.Log("ShowResults called for book: " + bookTitle);

            Debug.Log($"Final answeredCorrectly array: [{answeredCorrectly[0]}, {answeredCorrectly[1]}, {answeredCorrectly[2]}]");
            Debug.Log($"Final hasAnswered array: [{hasAnswered[0]}, {hasAnswered[1]}, {hasAnswered[2]}]");

            int correctCount = 0;
            for (int i = 0; i < answeredCorrectly.Length; i++)
            {
                if (answeredCorrectly[i])
                {
                    correctCount++;
                    Debug.Log($"Page {i + 1} was answered correctly");
                }
                else
                {
                    Debug.Log($"Page {i + 1} was answered incorrectly or not answered");
                }
            }

            string resultMessage = $"Quiz Complete! You got {correctCount} out of 3 questions correct.";

            if (correctCount == 3)
            {
                if (!isFinished)
                {
                    isFinished = true;
                    audioSource.PlayOneShot(BookComplete);

                    _CompletedBooks++;

                    if (_CompletedBooks == 5)
                    {
                        audioSource.PlayOneShot(ExitDoorOpen);
                        Debug.Log("play exit door sound");

                    }

                    // Update all book displays
                    BookInteractable[] allBooks = FindObjectsOfType<BookInteractable>();
                    foreach (BookInteractable book in allBooks)
                    {
                        book.UpdateCompletedBooksDisplay();
                    }

                    // Trigger vanish after perfect completion if enabled
                    if (vanishAfterCompletion)
                    {
                        await Task.Delay(1000);
                        StartCoroutine(VanishAfterDelay(1.2f)); // Wait 1.2 seconds before vanishing
                    }


                }
            }

            Debug.Log(resultMessage);
        }

        // Vanish functionality
        public void Vanish()
        {
            if (isVanishing || hasVanished) return;

            isVanishing = true;
            hasVanished = true;

            // Close book if it's open
            if (isBookOpen)
            {
                CloseBook();
            }

            // Update interaction prompt for vanished state
            UpdateInteractionPrompt();

            // Play vanish sound if available
            if (audioSource != null && vanishSound != null)
            {
                audioSource.PlayOneShot(vanishSound);
            }

            // Disable collider
            if (objCollider != null)
            {
                objCollider.enabled = false;
            }

            // Start vanish animation based on type
            switch (vanishType)
            {
                case VanishType.Instant:
                    InstantVanish();
                    break;
                case VanishType.FadeOut:
                    StartCoroutine(FadeOutCoroutine());
                    break;
                case VanishType.ScaleDown:
                    StartCoroutine(ScaleDownCoroutine());
                    break;
                case VanishType.FadeAndScale:
                    StartCoroutine(FadeAndScaleCoroutine());
                    break;
                case VanishType.Dissolve:
                    StartCoroutine(DissolveCoroutine());
                    break;
            }
        }

        IEnumerator VanishAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Vanish();
        }

        void InstantVanish()
        {
            if (destroyAfterVanish)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        IEnumerator FadeOutCoroutine()
        {
            if (objRenderer == null) yield break;

            Material mat = objRenderer.material;
            Color originalColor = mat.color;
            float elapsed = 0f;

            while (elapsed < vanishDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / vanishDuration);
                mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            FinishVanish();
        }

        IEnumerator ScaleDownCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < vanishDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 0f, elapsed / vanishDuration);
                transform.localScale = originalScale * scale;
                yield return null;
            }

            FinishVanish();
        }

        IEnumerator FadeAndScaleCoroutine()
        {
            if (objRenderer == null) yield break;

            Material mat = objRenderer.material;
            Color originalColor = mat.color;
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < vanishDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / vanishDuration;

                float alpha = Mathf.Lerp(1f, 0f, progress);
                mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

                float scale = Mathf.Lerp(1f, 0f, progress);
                transform.localScale = originalScale * scale;

                yield return null;
            }

            FinishVanish();
        }

        IEnumerator DissolveCoroutine()
        {
            if (objRenderer == null) yield break;

            Material mat = objRenderer.material;

            if (mat.HasProperty("_Dissolve"))
            {
                float elapsed = 0f;

                while (elapsed < vanishDuration)
                {
                    elapsed += Time.deltaTime;
                    float dissolve = Mathf.Lerp(0f, 1f, elapsed / vanishDuration);
                    mat.SetFloat("_Dissolve", dissolve);
                    yield return null;
                }
            }
            else
            {
                StartCoroutine(FadeOutCoroutine());
                yield break;
            }

            FinishVanish();
        }

        void FinishVanish()
        {
            if (destroyAfterVanish)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // Public methods for external control
        public void ForceVanish()
        {
            Vanish();
        }

        public void SetVanishType(VanishType type)
        {
            vanishType = type;
        }

        public void SetVanishDuration(float duration)
        {
            vanishDuration = duration;
        }

        public void ResetQuiz()
        {
            for (int i = 0; i < hasAnswered.Length; i++)
            {
                hasAnswered[i] = false;
                answeredCorrectly[i] = false;
            }
            isInteractionLocked = false;
            currentPage = 0;
            if (resultPanel != null)
                resultPanel.SetActive(false);
            DisplayCurrentPage();
            Debug.Log("Quiz reset for book: " + bookTitle);
        }

        public static int CompletedBooksCount
        {
            get { return _CompletedBooks; }
        }
    }
}