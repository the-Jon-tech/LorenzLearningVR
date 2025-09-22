using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public bool startTimerOnAwake = true;

    [Header("UI References")]
    public TextMeshProUGUI timerDisplayText;
    public TextMeshProUGUI finalTimeText;
    public GameObject gameCompletePanel;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnGameStart;
    public UnityEngine.Events.UnityEvent OnGameComplete;

    // Timer variables
    private float startTime;
    private float endTime;
    private bool isTimerRunning = false;
    private bool isGameComplete = false;
    

    // Properties
    public float ElapsedTime
    {
        get
        {
            if (!isTimerRunning) return 0f;
            if (isGameComplete) return endTime - startTime;
            return Time.time - startTime;
        }
    }

    public bool IsRunning => isTimerRunning && !isGameComplete;
    public bool IsComplete => isGameComplete;


    void Update()
    {
        // Update timer display if UI text is assigned and timer is running
        if (timerDisplayText != null && isTimerRunning && !isGameComplete)
        {
            UpdateTimerDisplay();
        }
    }

    public void StartTimer()
    {
        if (isGameComplete) return;

        startTime = Time.time;
        isTimerRunning = true;

        //Debug.Log("Game Timer Started");
        OnGameStart?.Invoke();
    }

    public void CompleteGame()
    {
        if (!isTimerRunning || isGameComplete) return;

        endTime = Time.time;
        isGameComplete = true;

        float finalTime = ElapsedTime;
        Debug.Log("Game Completed! Final Time: " + FormatTime(finalTime));

        // Update final time display
        if (finalTimeText != null)
        {
            finalTimeText.text = "Final Time: " + FormatTime(finalTime);
        }

        // Show completion panel
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
        }

        OnGameComplete?.Invoke();
    }

    public void ResetTimer()
    {
        startTime = 0f;
        endTime = 0f;
        isTimerRunning = false;
        isGameComplete = false;

        // Hide completion panel
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(false);
        }

        Debug.Log("Game Timer Reset");
    }

    private void UpdateTimerDisplay()
    {
        if (timerDisplayText != null)
        {
            timerDisplayText.text = FormatTime(ElapsedTime);
        }
    }

    public string FormatTime(float timeInSeconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);

        if (timeSpan.TotalHours >= 1)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D2}",
                (int)timeSpan.TotalHours,
                timeSpan.Minutes,
                timeSpan.Seconds,
                timeSpan.Milliseconds / 10);
        }
        else
        {
            return string.Format("{0:D2}:{1:D2}.{2:D2}",
                timeSpan.Minutes,
                timeSpan.Seconds,
                timeSpan.Milliseconds / 10);
        }
    }

    // Public methods for external scripts to call
    public void OnPlayerReachedGoal()
    {
        CompleteGame();
    }

    public void OnPlayerDied()
    {
        // You might want to stop the timer when player dies
        // Or keep it running depending on your game design
    }

    // Save/Load functionality for persistent timing across scenes
    public void SaveTimeToPlayerPrefs(string key = "GameTime")
    {
        PlayerPrefs.SetFloat(key, ElapsedTime);
        PlayerPrefs.Save();
    }

    public float LoadTimeFromPlayerPrefs(string key = "GameTime")
    {
        return PlayerPrefs.GetFloat(key, 0f);
    }
}
