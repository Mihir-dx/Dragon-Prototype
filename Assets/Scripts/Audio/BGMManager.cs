using UnityEngine;

namespace Dragon.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BGMManager : MonoBehaviour
    {
        [Header("Background Music")]
        [Tooltip("The music track to loop in the background.")]
        [SerializeField] private AudioClip backgroundMusic;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.5f;

        private static BGMManager instance;
        private AudioSource audioSource;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false; // We control it via script to ensure settings
            audioSource.spatialBlend = 0f; // 2D sound for BGM

            if (backgroundMusic != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.volume = volume;
                audioSource.Play();
            }
        }
    }
}
