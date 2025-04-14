// Assets/Scripts/Managers/AudioManager/AudioManager.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace GameCore.Core
{
    public enum AudioType { SFX, Music, UI, Ambient }

    [System.Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public float musicVolume = 1.0f;
        public float uiVolume = 1.0f;
        public float ambientVolume = 1.0f;
    }

    public class AudioManager : MonoBehaviour, IService, IInitializable
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSettings settings = new AudioSettings();
        [SerializeField] private AudioMixerGroup masterMixer;
        [SerializeField] private AudioMixerGroup sfxMixer;
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup uiMixer;
        [SerializeField] private AudioMixerGroup ambientMixer;

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxSourcesCount = 5;

        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
        private Dictionary<AudioType, List<AudioSource>> _audioSources = new Dictionary<AudioType, List<AudioSource>>();

        // IInitializable implementation
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 70;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.RegisterService<AudioManager>(this).ConfigureAwait(false);
            }
            else
            {
                Initialize().ConfigureAwait(false);
            }
        }

        public async Task Initialize()
        {
            if (IsInitialized) return;

            InitAudioSources();
            LoadSavedSettings();
            ApplyVolumeSettings();

            // Підписка на події
            EventBus.Subscribe("Audio/PlaySound", OnPlaySoundEvent);
            EventBus.Subscribe("Audio/PlayMusic", OnPlayMusicEvent);
            EventBus.Subscribe("Audio/StopMusic", _ => StopMusic());
            EventBus.Subscribe("Audio/SetVolume", OnSetVolumeEvent);

            IsInitialized = true;
            CoreLogger.Log("AUDIO", "AudioManager initialized");

            await Task.CompletedTask;
        }

        private void InitAudioSources()
        {
            // Перевіряємо музичне джерело
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                if (musicMixer != null) musicSource.outputAudioMixerGroup = musicMixer;
            }

            _audioSources[AudioType.Music] = new List<AudioSource> { musicSource };

            // Створюємо пул джерел для SFX
            _audioSources[AudioType.SFX] = new List<AudioSource>();
            for (int i = 0; i < sfxSourcesCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                if (sfxMixer != null) source.outputAudioMixerGroup = sfxMixer;
                _audioSources[AudioType.SFX].Add(source);
            }

            // Створюємо пул джерел для UI
            _audioSources[AudioType.UI] = new List<AudioSource>();
            AudioSource uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            if (uiMixer != null) uiSource.outputAudioMixerGroup = uiMixer;
            _audioSources[AudioType.UI].Add(uiSource);

            // Створюємо пул джерел для Ambient
            _audioSources[AudioType.Ambient] = new List<AudioSource>();
            AudioSource ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            if (ambientMixer != null) ambientSource.outputAudioMixerGroup = ambientMixer;
            _audioSources[AudioType.Ambient].Add(ambientSource);
        }

        private void LoadSavedSettings()
        {
            // Завантажуємо налаштування аудіо з PlayerPrefs
            if (PlayerPrefs.HasKey("MasterVolume"))
                settings.masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            if (PlayerPrefs.HasKey("SFXVolume"))
                settings.sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
            if (PlayerPrefs.HasKey("MusicVolume"))
                settings.musicVolume = PlayerPrefs.GetFloat("MusicVolume");
            if (PlayerPrefs.HasKey("UIVolume"))
                settings.uiVolume = PlayerPrefs.GetFloat("UIVolume");
            if (PlayerPrefs.HasKey("AmbientVolume"))
                settings.ambientVolume = PlayerPrefs.GetFloat("AmbientVolume");
        }

        private void ApplyVolumeSettings()
        {
            // Застосовуємо налаштування гучності до міксерів
            if (masterMixer != null)
                masterMixer.audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.0001f, settings.masterVolume)) * 20);
            if (sfxMixer != null)
                sfxMixer.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, settings.sfxVolume)) * 20);
            if (musicMixer != null)
                musicMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, settings.musicVolume)) * 20);
            if (uiMixer != null)
                uiMixer.audioMixer.SetFloat("UIVolume", Mathf.Log10(Mathf.Max(0.0001f, settings.uiVolume)) * 20);
            if (ambientMixer != null)
                ambientMixer.audioMixer.SetFloat("AmbientVolume", Mathf.Log10(Mathf.Max(0.0001f, settings.ambientVolume)) * 20);
        }

        public void PlaySound(string clipName, AudioType type = AudioType.SFX, float volume = 1.0f, float pitch = 1.0f)
        {
            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            AudioSource source = GetAvailableSource(type);
            if (source == null) return;

            source.clip = clip;
            source.volume = GetVolumeForType(type) * volume;
            source.pitch = pitch;
            source.Play();

            CoreLogger.Log("AUDIO", $"Playing sound: {clipName}, Type: {type}");
        }

        public void PlayMusic(string clipName, float fadeInDuration = 1.0f, float volume = 1.0f)
        {
            AudioClip clip = GetAudioClip(clipName);
            if (clip == null) return;

            if (musicSource.isPlaying && musicSource.clip == clip)
                return;

            StartCoroutine(CrossFadeMusic(clip, fadeInDuration, volume));
        }

        private System.Collections.IEnumerator CrossFadeMusic(AudioClip newClip, float fadeDuration, float targetVolume)
        {
            float startVolume = musicSource.volume;
            float startTime = Time.time;

            // Якщо музика вже грає, поступово зменшуємо гучність
            if (musicSource.isPlaying)
            {
                while (Time.time - startTime < fadeDuration)
                {
                    float t = (Time.time - startTime) / fadeDuration;
                    musicSource.volume = Mathf.Lerp(startVolume, 0, t);
                    yield return null;
                }

                musicSource.Stop();
            }

            // Встановлюємо новий трек і починаємо грати
            musicSource.clip = newClip;
            musicSource.volume = 0;
            musicSource.Play();

            // Поступово збільшуємо гучність
            startTime = Time.time;
            float targetVol = GetVolumeForType(AudioType.Music) * targetVolume;

            while (Time.time - startTime < fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                musicSource.volume = Mathf.Lerp(0, targetVol, t);
                yield return null;
            }

            musicSource.volume = targetVol;
            CoreLogger.Log("AUDIO", $"Music changed to: {newClip.name}");
        }

        public void StopMusic(float fadeOutDuration = 1.0f)
        {
            if (!musicSource.isPlaying) return;
            StartCoroutine(FadeOutMusic(fadeOutDuration));
        }

        private System.Collections.IEnumerator FadeOutMusic(float fadeDuration)
        {
            float startVolume = musicSource.volume;
            float startTime = Time.time;

            while (Time.time - startTime < fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0, t);
                yield return null;
            }

            musicSource.volume = 0;
            musicSource.Stop();
            CoreLogger.Log("AUDIO", "Music stopped");
        }

        public void SetVolume(AudioType type, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (type)
            {
                case AudioType.SFX:
                    settings.sfxVolume = volume;
                    PlayerPrefs.SetFloat("SFXVolume", volume);
                    break;
                case AudioType.Music:
                    settings.musicVolume = volume;
                    PlayerPrefs.SetFloat("MusicVolume", volume);
                    break;
                case AudioType.UI:
                    settings.uiVolume = volume;
                    PlayerPrefs.SetFloat("UIVolume", volume);
                    break;
                case AudioType.Ambient:
                    settings.ambientVolume = volume;
                    PlayerPrefs.SetFloat("AmbientVolume", volume);
                    break;
            }

            ApplyVolumeSettings();
            CoreLogger.Log("AUDIO", $"Volume for {type} set to {volume}");
        }

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            settings.masterVolume = volume;
            PlayerPrefs.SetFloat("MasterVolume", volume);
            ApplyVolumeSettings();
            CoreLogger.Log("AUDIO", $"Master volume set to {volume}");
        }

        private AudioClip GetAudioClip(string clipName)
        {
            if (_audioClips.TryGetValue(clipName, out AudioClip clip))
                return clip;

            // Спроба завантажити з Resources
            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip == null)
            {
                CoreLogger.LogWarning("AUDIO", $"Audio clip not found: {clipName}");
                return null;
            }

            _audioClips[clipName] = clip;
            return clip;
        }

        private AudioSource GetAvailableSource(AudioType type)
        {
            if (!_audioSources.TryGetValue(type, out List<AudioSource> sources) || sources.Count == 0)
            {
                CoreLogger.LogWarning("AUDIO", $"No audio sources available for type: {type}");
                return null;
            }

            if (type == AudioType.Music || type == AudioType.Ambient)
                return sources[0]; // Для музики та амбієнта завжди використовуємо перше джерело

            // Для SFX та UI шукаємо вільне джерело
            foreach (AudioSource source in sources)
            {
                if (!source.isPlaying)
                    return source;
            }

            // Якщо всі зайняті, повертаємо джерело, яке грає найдовше
            AudioSource oldestSource = sources[0];
            float longestTime = 0;

            foreach (AudioSource source in sources)
            {
                float playingTime = source.time;
                if (playingTime > longestTime)
                {
                    longestTime = playingTime;
                    oldestSource = source;
                }
            }

            return oldestSource;
        }

        private float GetVolumeForType(AudioType type)
        {
            float volume = settings.masterVolume;
            switch (type)
            {
                case AudioType.SFX:
                    volume *= settings.sfxVolume;
                    break;
                case AudioType.Music:
                    volume *= settings.musicVolume;
                    break;
                case AudioType.UI:
                    volume *= settings.uiVolume;
                    break;
                case AudioType.Ambient:
                    volume *= settings.ambientVolume;
                    break;
            }
            return volume;
        }

        // Event Handlers
        private void OnPlaySoundEvent(object data)
        {
            if (data is string clipName)
            {
                PlaySound(clipName);
            }
            else if (data is System.Tuple<string, AudioType> soundData)
            {
                PlaySound(soundData.Item1, soundData.Item2);
            }
        }

        private void OnPlayMusicEvent(object data)
        {
            if (data is string clipName)
            {
                PlayMusic(clipName);
            }
        }

        private void OnSetVolumeEvent(object data)
        {
            if (data is System.Tuple<AudioType, float> volumeData)
            {
                SetVolume(volumeData.Item1, volumeData.Item2);
            }
        }
    }
}
