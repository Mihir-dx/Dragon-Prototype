using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dragon.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [Tooltip("The index of the game scene to load.")]
        [SerializeField] private int gameSceneIndex = 1;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(PlayGame);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
                
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(PlayGame);
            
            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
        }

        private void PlayGame()
        {
            SceneManager.LoadScene(gameSceneIndex);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

// Code by Mihir