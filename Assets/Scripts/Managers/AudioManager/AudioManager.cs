using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using GameCore.Core.EventSystem;

namespace GameCore.Core
{
    /// <summary>
    /// Менеджер аудіо для керування звуками та музикою з інтеграцією ResourceManager.
    /// </summary>
    public class AudioManager : MonoBehaviour, IService, IInitializable
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private int sfxSourcesPoolSize = 5;

        [Header("Audio Settings")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private bool muteOnPause = true;
        [SerializeField] private bool loadOnDemand = true;
        [SerializeField] private bool useResourceManager = true;

        [Header("Default Volumes")]
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1.0f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float defaultUiVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultAmbientVolume = 0.5f;

        // Пули аудіо-джерел для ефектів
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private Transform _poolRoot;
       
        // Кеш аудіо кліпів
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

        // Флаги стану
        private bool _isPaused = false;
        private bool _isInitialized = false;

        // Системи для завантаження ресурсів
        private ResourceManager _resourceManager;

        // Поточна музика
        private string _currentMusicName;
        private string _nextMusicName;
        private float _musicFadeTime = 0;
        private float _musicFadeTimer = 0;
        private bool _isMusicFading = false;

        // Корутіна для плавних переходів
        private Coroutine _fadeCoroutine;

        public bool IsInitialized => _isInitialized;
        public int InitializationPriority => 50;

        #region Ініціалізація

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
           
        }

        public async Task Initialize()
        {
            CoreLogger.Log("AUDIO", "Ініціалізація AudioManager...");

            // Отримуємо ResourceManager
            if (useResourceManager)
            {
                _resourceManager = ServiceLocator.Instance?.GetService<ResourceManager>();
                if (_resourceManager == null)
                {
                    CoreLogger.LogWarning("AUDIO", "ResourceManager не знайдено, звуки будуть завантажуватись напряму");
                    useResourceManager = false;
                }
            }

            // Перевірка і створення аудіо міксера, якщо не вказаний
            if (audioMixer == null)
            {
                audioMixer = await LoadAudioMixerAsync("MasterMixer");
                if (audioMixer == null)
                {
                    CoreLogger.LogWarning("AUDIO", "Не вдалося завантажити AudioMixer. Використовуємо значення за замовчуванням.");
                }
            }

            // Створення кореневого об'єкту для пулу
            _poolRoot = new GameObject("[AudioSourcesPool]").transform;
            _poolRoot.SetParent(transform);

            // Ініціалізуємо музичне джерело, якщо воно не вказане
            if (musicSource == null)
            {
                GameObject musicObj = await LoadAudioSourcePrefabAsync("MusicSource");
                if (musicObj == null)
                {
                    musicObj = new GameObject("MusicSource");
                    musicSource = musicObj.AddComponent<AudioSource>();
                }
                else
                {
                    musicSource = musicObj.GetComponent<AudioSource>();
                }

                musicSource.transform.SetParent(transform);
                musicSource.loop = true;
                musicSource.playOnAwake = false;

                // Підключаємо до міксера, якщо доступний
                if (audioMixer != null)
                {
                    AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups("Music");
                    if (mixerGroups.Length > 0)
                    {
                        musicSource.outputAudioMixerGroup = mixerGroups[0];
                    }
                }
            }

            // Ініціалізуємо UI джерело, якщо воно не вказане
            if (uiSource == null)
            {
                GameObject uiObj = new GameObject("UISource");
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.transform.SetParent(transform);
                uiSource.loop = false;
                uiSource.playOnAwake = false;

                // Підключаємо до міксера, якщо доступний
                if (audioMixer != null)
                {
                    AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups("UI");
                    if (mixerGroups.Length > 0)
                    {
                        uiSource.outputAudioMixerGroup = mixerGroups[0];
                    }
                }
            }

            // Створюємо пул для SFX
            await CreateSfxPool(sfxSourcesPoolSize);

            // Встановлюємо початкові значення гучності
            SetDefaultVolumes();

            // Підписуємось на події
            EventBus.Subscribe("Game/Pause", (data) => OnGamePause());
            EventBus.Subscribe("Game/Resume", (data) => OnGameResume());
            EventBus.Subscribe("Audio/PlaySound", OnPlaySoundEvent);
            EventBus.Subscribe("Audio/PlayMusic", OnPlayMusicEvent);
            EventBus.Subscribe("Audio/StopMusic", (data) => StopMusic());
            EventBus.Subscribe("Audio/SetVolume", OnSetVolumeEvent);

            _isInitialized = true;
            CoreLogger.Log("AUDIO", "✅ AudioManager успішно ініціалізовано");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;

                // Відписуємося від подій
                EventBus.Unsubscribe("Game/Pause", (data) => OnGamePause());
                EventBus.Unsubscribe("Game/Resume", (data) => OnGameResume());
                EventBus.Unsubscribe("Audio/PlaySound", OnPlaySoundEvent);
                EventBus.Unsubscribe("Audio/PlayMusic", OnPlayMusicEvent);
                EventBus.Unsubscribe("Audio/StopMusic", (data) => StopMusic());
                EventBus.Unsubscribe("Audio/SetVolume", OnSetVolumeEvent);
            }
        }

        #endregion

        #region Завантаження аудіо ресурсів

        /// <summary>
        /// Асинхронне завантаження аудіо кліпу через ResourceManager або Resources
        /// </summary>
        private async Task<AudioClip> LoadAudioClipAsync(string clipName, AudioType audioType)
        {
            // Формуємо шлях до ресурсу
            string resourcePath = GetAudioResourcePath(audioType, clipName);

            // Перевіряємо кеш
            if (_audioCache.TryGetValue(resourcePath, out AudioClip cachedClip) && cachedClip != null)
            {
                return cachedClip;
            }

            AudioClip clip = null;

            // Завантажуємо через ResourceManager, якщо доступний
            if (useResourceManager && _resourceManager != null)
            {
                try
                {
                    // Створюємо запит на ресурс
                    var request = _resourceManager.CreateRequest<AudioClip>(
                        GetResourceTypeForAudioType(audioType),
                        clipName,
                        false
                    );

                    // Чекаємо на завершення завантаження
                    clip = await request.GetResultAsync();
                }
                catch (Exception ex)
                {
                    CoreLogger.LogWarning("AUDIO", $"Помилка завантаження звуку через ResourceManager: {ex.Message}");
                }
            }

            // Якщо не вдалося завантажити через ResourceManager, використовуємо стандартний метод
            if (clip == null)
            {
                clip = Resources.Load<AudioClip>(resourcePath);
            }

            // Кешуємо результат, якщо успішно
            if (clip != null)
            {
                _audioCache[resourcePath] = clip;
            }
            else
            {
                CoreLogger.LogWarning("AUDIO", $"Не вдалося завантажити аудіо кліп: {resourcePath}");
            }

            return clip;
        }

        /// <summary>
        /// Асинхронне завантаження AudioMixer
        /// </summary>
        private async Task<AudioMixer> LoadAudioMixerAsync(string mixerPath)
        {
            AudioMixer mixer = null;

            // Завантажуємо через ResourceManager, якщо доступний
            if (useResourceManager && _resourceManager != null)
            {
                try
                {
                    var request = _resourceManager.CreateRequest<AudioMixer>(
                        ResourceManager.ResourceType.Audio,
                        mixerPath,
                        false
                    );

                    mixer = await request.GetResultAsync();
                }
                catch (Exception ex)
                {
                    CoreLogger.LogWarning("AUDIO", $"Помилка завантаження AudioMixer через ResourceManager: {ex.Message}");
                }
            }

            // Запасний варіант
            if (mixer == null)
            {
                mixer = Resources.Load<AudioMixer>(mixerPath);
            }

            return mixer;
        }

        /// <summary>
        /// Асинхронне завантаження префабу AudioSource
        /// </summary>
        private async Task<GameObject> LoadAudioSourcePrefabAsync(string prefabPath)
        {
            GameObject prefab = null;

            // Завантажуємо через ResourceManager, якщо доступний
            if (useResourceManager && _resourceManager != null)
            {
                try
                {
                    var request = _resourceManager.CreateRequest<GameObject>(
                        ResourceManager.ResourceType.Audio,
                        prefabPath,
                        true
                    );

                    prefab = await request.GetResultAsync();
                }
                catch (Exception ex)
                {
                    CoreLogger.LogWarning("AUDIO", $"Помилка завантаження префабу AudioSource: {ex.Message}");
                }
            }

            // Запасний варіант
            if (prefab == null)
            {
                GameObject originalPrefab = Resources.Load<GameObject>(prefabPath);
                if (originalPrefab != null)
                {
                    prefab = Instantiate(originalPrefab);
                }
            }

            return prefab;
        }

        /// <summary>
        /// Формує шлях до аудіо ресурсу на основі типу
        /// </summary>
        private string GetAudioResourcePath(AudioType audioType, string clipName)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    return $"Audio/Music/{clipName}";
                case AudioType.SFX:
                    return $"Audio/SFX/{clipName}";
                case AudioType.UI:
                    return $"Audio/UI/{clipName}";
                case AudioType.Ambient:
                    return $"Audio/Ambient/{clipName}";
                default:
                    return $"Audio/{clipName}";
            }
        }

        /// <summary>
        /// Конвертує AudioType у ResourceManager.ResourceType
        /// </summary>
        private ResourceManager.ResourceType GetResourceTypeForAudioType(AudioType audioType)
        {
            // Так як у ResourceManager тип Audio охоплює всі аудіо ресурси
            return ResourceManager.ResourceType.Audio;
        }

        #endregion

        #region Створення пулу звуків

        /// <summary>
        /// Створює пул аудіо-джерел для звукових ефектів
        /// </summary>
        private async Task CreateSfxPool(int size)
        {
            // Очищуємо існуючий пул
            foreach (var source in _sfxPool)
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }
            _sfxPool.Clear();

            // Створюємо нові джерела
            for (int i = 0; i < size; i++)
            {
                GameObject sourceObj = new GameObject($"SFX_Source_{i}");
                sourceObj.transform.SetParent(_poolRoot);

                AudioSource source = sourceObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;

                // Підключаємо до міксера, якщо доступний
                if (audioMixer != null)
                {
                    AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups("SFX");
                    if (mixerGroups.Length > 0)
                    {
                        source.outputAudioMixerGroup = mixerGroups[0];
                    }
                }

                _sfxPool.Add(source);
            }

            CoreLogger.Log("AUDIO", $"Створено пул з {size} SFX джерел");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Отримує вільне аудіо-джерело з пулу або створює нове, якщо всі зайняті
        /// </summary>
        private AudioSource GetFreeAudioSource()
        {
            // Шукаємо вільне джерело
            foreach (var source in _sfxPool)
            {
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }

            // Якщо всі зайняті, створюємо нове
            GameObject sourceObj = new GameObject($"SFX_Source_{_sfxPool.Count}");
            sourceObj.transform.SetParent(_poolRoot);

            AudioSource newSource = sourceObj.AddComponent<AudioSource>();
            newSource.playOnAwake = false;

            // Підключаємо до міксера
            if (audioMixer != null)
            {
                AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups("SFX");
                if (mixerGroups.Length > 0)
                {
                    newSource.outputAudioMixerGroup = mixerGroups[0];
                }
            }

            _sfxPool.Add(newSource);
            return newSource;
        }

        #endregion

        #region Керування гучністю

        /// <summary>
        /// Встановлює початкові значення гучності для всіх каналів
        /// </summary>
        private void SetDefaultVolumes()
        {
            SetVolume(AudioType.Master, defaultMasterVolume);
            SetVolume(AudioType.Music, defaultMusicVolume);
            SetVolume(AudioType.SFX, defaultSfxVolume);
            SetVolume(AudioType.UI, defaultUiVolume);
            SetVolume(AudioType.Ambient, defaultAmbientVolume);
        }

        /// <summary>
        /// Встановлює гучність для вказаного типу аудіо
        /// </summary>
        public void SetVolume(AudioType audioType, float volume)
        {
            volume = Mathf.Clamp01(volume);

            if (audioMixer != null)
            {
                // Перетворення лінійного значення (0-1) у логарифмічну шкалу для міксера (-80 до 0 дБ)
                float dbValue = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;

                switch (audioType)
                {
                    case AudioType.Master:
                        audioMixer.SetFloat("MasterVolume", dbValue);
                        break;
                    case AudioType.Music:
                        audioMixer.SetFloat("MusicVolume", dbValue);
                        break;
                    case AudioType.SFX:
                        audioMixer.SetFloat("SFXVolume", dbValue);
                        break;
                    case AudioType.UI:
                        audioMixer.SetFloat("UIVolume", dbValue);
                        break;
                    case AudioType.Ambient:
                        audioMixer.SetFloat("AmbientVolume", dbValue);
                        break;
                }
            }
            else
            {
                // Якщо міксер недоступний, встановлюємо гучність напряму
                switch (audioType)
                {
                    case AudioType.Master:
                        // Застосовуємо до всіх джерел
                        if (musicSource != null) musicSource.volume = volume;
                        if (uiSource != null) uiSource.volume = volume;
                        foreach (var source in _sfxPool)
                        {
                            if (source != null) source.volume = volume;
                        }
                        break;
                    case AudioType.Music:
                        if (musicSource != null) musicSource.volume = volume;
                        break;
                    case AudioType.UI:
                        if (uiSource != null) uiSource.volume = volume;
                        break;
                    case AudioType.SFX:
                    case AudioType.Ambient:
                        // Для SFX і Ambient налаштовуємо через пули, якщо потрібно
                        break;
                }
            }

            // Сповіщаємо про зміну гучності
            EventBus.Emit("Audio/VolumeChanged", new VolumeChangeData { AudioType = audioType, Volume = volume });
        }

        /// <summary>
        /// Отримує поточне значення гучності для вказаного типу аудіо
        /// </summary>
        public float GetVolume(AudioType audioType)
        {
            if (audioMixer != null)
            {
                float dbValue = 0f;
                bool success = false;

                switch (audioType)
                {
                    case AudioType.Master:
                        success = audioMixer.GetFloat("MasterVolume", out dbValue);
                        break;
                    case AudioType.Music:
                        success = audioMixer.GetFloat("MusicVolume", out dbValue);
                        break;
                    case AudioType.SFX:
                        success = audioMixer.GetFloat("SFXVolume", out dbValue);
                        break;
                    case AudioType.UI:
                        success = audioMixer.GetFloat("UIVolume", out dbValue);
                        break;
                    case AudioType.Ambient:
                        success = audioMixer.GetFloat("AmbientVolume", out dbValue);
                        break;
                }

                if (success)
                {
                    // Перетворюємо з дБ (лог. шкала) у лінійне значення (0-1)
                    return dbValue <= -80f ? 0f : Mathf.Pow(10f, dbValue / 20f);
                }
            }

            // Запасний варіант, якщо міксер недоступний
            switch (audioType)
            {
                case AudioType.Music:
                    return musicSource != null ? musicSource.volume : defaultMusicVolume;
                case AudioType.UI:
                    return uiSource != null ? uiSource.volume : defaultUiVolume;
                case AudioType.SFX:
                    return defaultSfxVolume;
                case AudioType.Ambient:
                    return defaultAmbientVolume;
                case AudioType.Master:
                default:
                    return defaultMasterVolume;
            }
        }

        /// <summary>
        /// Увімкнення/вимкнення звуку для вказаного типу
        /// </summary>
        public void SetMute(AudioType audioType, bool mute)
        {
            if (audioMixer != null)
            {
                // Зберігаємо поточну гучність
                float currentVolume = GetVolume(audioType);

                // Встановлюємо -80 дБ для вимкнення або відновлюємо попередню гучність
                float dbValue = mute ? -80f : (currentVolume > 0 ? 20f * Mathf.Log10(currentVolume) : -80f);

                switch (audioType)
                {
                    case AudioType.Master:
                        audioMixer.SetFloat("MasterVolume", dbValue);
                        break;
                    case AudioType.Music:
                        audioMixer.SetFloat("MusicVolume", dbValue);
                        break;
                    case AudioType.SFX:
                        audioMixer.SetFloat("SFXVolume", dbValue);
                        break;
                    case AudioType.UI:
                        audioMixer.SetFloat("UIVolume", dbValue);
                        break;
                    case AudioType.Ambient:
                        audioMixer.SetFloat("AmbientVolume", dbValue);
                        break;
                }
            }
            else
            {
                // Налаштовуємо mute напряму для джерел
                switch (audioType)
                {
                    case AudioType.Master:
                        // Застосовуємо до всіх джерел
                        if (musicSource != null) musicSource.mute = mute;
                        if (uiSource != null) uiSource.mute = mute;
                        foreach (var source in _sfxPool)
                        {
                            if (source != null) source.mute = mute;
                        }
                        break;
                    case AudioType.Music:
                        if (musicSource != null) musicSource.mute = mute;
                        break;
                    case AudioType.UI:
                        if (uiSource != null) uiSource.mute = mute;
                        break;
                    case AudioType.SFX:
                    case AudioType.Ambient:
                        // Для SFX і Ambient налаштовуємо через пули, якщо потрібно
                        foreach (var source in _sfxPool)
                        {
                            if (source != null) source.mute = mute;
                        }
                        break;
                }
            }
        }

        #endregion

        #region Відтворення звуків

        /// <summary>
        /// Відтворює звуковий ефект
        /// </summary>
        public async void PlaySound(string soundName, AudioType audioType = AudioType.SFX, float volume = 1.0f, float pitch = 1.0f)
        {
            if (string.IsNullOrEmpty(soundName))
                return;

            // Якщо потрібно завантажувати звук за потреби
            AudioClip clip = null;
            if (loadOnDemand)
            {
                clip = await LoadAudioClipAsync(soundName, audioType);
            }
            else
            {
                // Шукаємо в кеші або завантажуємо стандартно
                string resourcePath = GetAudioResourcePath(audioType, soundName);
                if (!_audioCache.TryGetValue(resourcePath, out clip) || clip == null)
                {
                    clip = Resources.Load<AudioClip>(resourcePath);
                    if (clip != null)
                    {
                        _audioCache[resourcePath] = clip;
                    }
                }
            }

            if (clip == null)
            {
                CoreLogger.LogWarning("AUDIO", $"Не вдалося знайти аудіо кліп: {soundName}");
                return;
            }

            // Вибір правильного джерела в залежності від типу звуку
            AudioSource source = null;
            switch (audioType)
            {
                case AudioType.UI:
                    source = uiSource;
                    break;
                case AudioType.SFX:
                case AudioType.Ambient:
                    source = GetFreeAudioSource();
                    break;
                default:
                    source = GetFreeAudioSource();
                    break;
            }

            if (source != null)
            {
                // Налаштовуємо параметри
                source.clip = clip;
                source.volume = volume;
                source.pitch = pitch;

                // Підключаємо до міксера, якщо потрібно
                if (audioMixer != null && audioType != AudioType.UI) // для UI джерела вже налаштовано
                {
                    string groupName = audioType == AudioType.Ambient ? "Ambient" : "SFX";
                    AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups(groupName);
                    if (mixerGroups.Length > 0)
                    {
                        source.outputAudioMixerGroup = mixerGroups[0];
                    }
                }

                // Відтворюємо звук
                source.Play();
            }
        }

        /// <summary>
        /// Відтворює музику з можливістю переходу
        /// </summary>
        public async void PlayMusic(string musicName, float fadeTime = 1.0f)
        {
            if (string.IsNullOrEmpty(musicName))
                return;

            // Якщо та сама музика, просто перевіряємо, чи вона відтворюється
            if (_currentMusicName == musicName && musicSource.isPlaying)
                return;

            // Зберігаємо назву наступного треку
            _nextMusicName = musicName;
            _musicFadeTime = fadeTime;

            // Якщо плавний перехід не потрібен, просто зупиняємо поточну музику
            if (fadeTime <= 0 || !musicSource.isPlaying)
            {
                StopMusic();
                await PlayNextMusic();
                return;
            }

            // Запускаємо плавний перехід
            _isMusicFading = true;
            _musicFadeTimer = 0;

            // Використовуємо корутину для плавного переходу
            StartCoroutine(FadeOutMusic(fadeTime));
        }

        /// <summary>
        /// Зупиняє поточну музику
        /// </summary>
        public void StopMusic(float fadeTime = 0)
        {
            // Скидаємо флаг наступного треку
            _nextMusicName = null;

            // Якщо музика не відтворюється, нічого не робимо
            if (!musicSource.isPlaying)
                return;

            // Якщо потрібно зупинити одразу
            if (fadeTime <= 0)
            {
                musicSource.Stop();
                _currentMusicName = null;
                return;
            }

            // Запускаємо плавну зупинку
            StartCoroutine(FadeOutMusic(fadeTime));
        }

        /// <summary>
        /// Завантажує та відтворює наступний музичний трек
        /// </summary>
        private async Task PlayNextMusic()
        {
            if (string.IsNullOrEmpty(_nextMusicName))
                return;

            // Завантажуємо аудіо кліп
            AudioClip musicClip = await LoadAudioClipAsync(_nextMusicName, AudioType.Music);
            if (musicClip == null)
            {
                CoreLogger.LogWarning("AUDIO", $"Не вдалося завантажити музику: {_nextMusicName}");
                _nextMusicName = null;
                return;
            }

            // Оновлюємо музичне джерело
            musicSource.clip = musicClip;
            musicSource.Play();
            _currentMusicName = _nextMusicName;
            _nextMusicName = null;

            // Плавно збільшуємо гучність, якщо потрібно
            if (_musicFadeTime > 0)
            {
                StartCoroutine(FadeInMusic(_musicFadeTime));
            }

            // Сповіщаємо про зміну музики
            EventBus.Emit("Audio/MusicChanged", _currentMusicName);
        }

        /// <summary>
        /// Корутина для плавного затухання музики
        /// </summary>
        private System.Collections.IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = musicSource.volume;
            float timer = 0;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                musicSource.volume = Mathf.Lerp(startVolume, 0, t);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = startVolume; // Відновлюємо гучність

            // Якщо є наступний трек, відтворюємо його
            if (!string.IsNullOrEmpty(_nextMusicName))
            {
                _ = PlayNextMusic();
            }
            else
            {
                _currentMusicName = null;
            }
        }

        /// <summary>
        /// Корутина для плавного підвищення гучності музики
        /// </summary>
        private System.Collections.IEnumerator FadeInMusic(float duration)
        {
            float targetVolume = GetVolume(AudioType.Music);
            musicSource.volume = 0;
            float timer = 0;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                musicSource.volume = Mathf.Lerp(0, targetVolume, t);
                yield return null;
            }

            musicSource.volume = targetVolume;
        }

        #endregion

        #region Обробники подій

        private void OnGamePause()
        {
            if (!muteOnPause) return;

            _isPaused = true;
            SetMute(AudioType.Master, true);
            CoreLogger.Log("AUDIO", "Звук призупинено через паузу гри");
        }

        private void OnGameResume()
        {
            if (!_isPaused) return;

            _isPaused = false;
            SetMute(AudioType.Master, false);
            CoreLogger.Log("AUDIO", "Звук відновлено");
        }

        private void OnPlaySoundEvent(object data)
        {
            if (data is string soundName)
            {
                PlaySound(soundName);
            }
            else if (data is Dictionary<string, object> dict)
            {
                // Отримуємо параметри
                string name = dict.TryGetValue("name", out object nameObj) ? nameObj as string : null;

                AudioType type = AudioType.SFX;
                if (dict.TryGetValue("type", out object typeObj) && typeObj is string typeStr)
                {
                    Enum.TryParse(typeStr, out type);
                }

                float volume = dict.TryGetValue("volume", out object volObj) && volObj is float v ? v : 1.0f;
                float pitch = dict.TryGetValue("pitch", out object pitchObj) && pitchObj is float p ? p : 1.0f;

                // Відтворюємо звук з вказаними параметрами
                if (!string.IsNullOrEmpty(name))
                {
                    PlaySound(name, type, volume, pitch);
                }
            }
        }

        private void OnPlayMusicEvent(object data)
        {
            if (data is string musicName)
            {
                PlayMusic(musicName);
            }
            else if (data is Dictionary<string, object> dict)
            {
                // Отримуємо параметри
                string name = dict.TryGetValue("name", out object nameObj) ? nameObj as string : null;
                float fadeTime = dict.TryGetValue("fadeTime", out object fadeObj) && fadeObj is float f ? f : 1.0f;

                // Відтворюємо музику
                if (!string.IsNullOrEmpty(name))
                {
                    PlayMusic(name, fadeTime);
                }
            }
        }

        private void OnSetVolumeEvent(object data)
        {
            if (data is Dictionary<string, object> dict)
            {
                // Отримуємо параметри
                AudioType type = AudioType.Master;
                if (dict.TryGetValue("type", out object typeObj) && typeObj is string typeStr)
                {
                    Enum.TryParse(typeStr, out type);
                }

                float volume = dict.TryGetValue("volume", out object volObj) && volObj is float v ? v : 1.0f;

                // Встановлюємо гучність
                SetVolume(type, volume);
            }
        }

        #endregion
    }

    /// <summary>
    /// Типи аудіо для різних каналів міксера
    /// </summary>
    public enum AudioType
    {
        Master,
        Music,
        SFX,
        UI,
        Ambient
    }

    /// <summary>
    /// Клас для передачі даних про зміну гучності
    /// </summary>
    public class VolumeChangeData
    {
        public AudioType AudioType { get; set; }
        public float Volume { get; set; }
    }
}