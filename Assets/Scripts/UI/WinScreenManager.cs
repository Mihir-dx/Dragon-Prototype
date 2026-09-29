using System.Collections;
using Dragon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dragon.UI
{
    public class WinScreenManager : MonoBehaviour
    {
        [Header("Dragon References")]
        [Tooltip("The player dragon's Health component.")]
        [SerializeField] private Health playerHealth;

        [Tooltip("The AI dragon's Health component.")]
        [SerializeField] private Health aiHealth;

        [Header("Display Names")]
        [SerializeField] private string playerName = "Player Dragon";
        [SerializeField] private string aiName = "Enemy Dragon";

        [Header("Win Screen")]
        [Tooltip("Root of the win screen hierarchy. Kept inactive until someone wins.")]
        [SerializeField] private GameObject winScreenRoot;

        [Tooltip("Label that receives the winner's name.")]
        [SerializeField] private TMP_Text winnerLabel;

        [Tooltip("Button that reloads the scene.")]
        [SerializeField] private Button restartButton;

        [Tooltip("Button that goes to the main menu.")]
        [SerializeField] private Button mainMenuButton;

        [Tooltip("{0} is replaced with the winner's name.")]
        [SerializeField] private string winMessageFormat = "{0} Wins!";

        [Header("Timing")]
        [Tooltip("Delay in seconds after death before the win screen appears, so the death " +
                 "animation can play out.")]
        [SerializeField] private float showDelay = 2.0f;

        private bool gameEnded;

        private void Awake()
        {
            if (winScreenRoot != null) winScreenRoot.SetActive(false);

            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(MainMenu);

            if (playerHealth != null) playerHealth.Died += HandlePlayerDied;
            if (aiHealth != null) aiHealth.Died += HandleAIDied;
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(MainMenu);

            if (playerHealth != null) playerHealth.Died -= HandlePlayerDied;
            if (aiHealth != null) aiHealth.Died -= HandleAIDied;
        }

        private void HandlePlayerDied(Health _) => EndGame(aiName);
        private void HandleAIDied(Health _) => EndGame(playerName);

        private void EndGame(string winnerName)
        {
            if (gameEnded) return;
            gameEnded = true;

            StartCoroutine(ShowAfterDelay(winnerName));
        }

        private IEnumerator ShowAfterDelay(string winnerName)
        {
            yield return new WaitForSeconds(showDelay);

            if (winnerLabel != null) winnerLabel.text = string.Format(winMessageFormat, winnerName);
            if (winScreenRoot != null) winScreenRoot.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void MainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }
}

// Code by Mihir