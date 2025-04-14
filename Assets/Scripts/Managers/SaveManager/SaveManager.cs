// Assets/Scripts/Managers/SaveManager/SaveManager.cs
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    [Serializable]
    public class GameData
    {
        public int version = 1;
        public string playerName = "Player";
        public int lastLevel = 1;
        // Додайте інші дані гри, які потрібно зберігати
    }

    public class SaveManager : MonoBehaviour, IService, IInitializable
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField] private string saveFileName = "save.json";
        [SerializeField] private bool useEncryption = true;
        [SerializeField] private string encryptionKey = "YourSecretKey123";

        private GameData _currentData;
        private bool _dataLoaded = false;

        // IInitializable implementation
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 75;

        public GameData CurrentData
        {
            get
            {
                if (!_dataLoaded)
                {
                    LoadGame();
                }
                return _currentData;
            }
        }

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
            if (IsInitialized) return;

            // Завантажуємо дані гри при ініціалізації
            LoadGame();

            // Підписуємося на події
            EventBus.Subscribe("Save/SaveGame", _ => SaveGame());
            EventBus.Subscribe("Save/LoadGame", _ => LoadGame());
            EventBus.Subscribe("Save/ResetGame", _ => ResetGame());

            IsInitialized = true;
            CoreLogger.Log("SAVE", "SaveManager initialized");

            await Task.CompletedTask;
        }

        public void SaveGame()
        {
            try
            {
                string path = GetSavePath();
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonData = JsonUtility.ToJson(_currentData, true);

                if (useEncryption)
                {
                    jsonData = EncryptDecrypt(jsonData);
                }

                File.WriteAllText(path, jsonData);
                CoreLogger.Log("SAVE", "Game saved successfully");

                // Сповіщаємо про успішне збереження
                EventBus.Emit("Save/GameSaved", null);
            }
            catch (Exception e)
            {
                CoreLogger.LogError("SAVE", $"Failed to save game: {e.Message}");
            }
        }

        public void LoadGame()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                CoreLogger.Log("SAVE", "No save file found, creating new save");
                _currentData = new GameData();
                _dataLoaded = true;
                return;
            }

            try
            {
                string jsonData = File.ReadAllText(path);

                if (useEncryption)
                {
                    jsonData = EncryptDecrypt(jsonData);
                }

                _currentData = JsonUtility.FromJson<GameData>(jsonData);
                _dataLoaded = true;
                CoreLogger.Log("SAVE", "Game loaded successfully");

                // Сповіщаємо про успішне завантаження
                EventBus.Emit("Save/GameLoaded", _currentData);
            }
            catch (Exception e)
            {
                CoreLogger.LogError("SAVE", $"Failed to load game: {e.Message}");
                _currentData = new GameData();
                _dataLoaded = true;
            }
        }

        public void ResetGame()
        {
            _currentData = new GameData();
            SaveGame();
            CoreLogger.Log("SAVE", "Game progress reset");

            // Сповіщаємо про скидання прогресу
            EventBus.Emit("Save/GameReset", null);
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        private string EncryptDecrypt(string data)
        {
            char[] result = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (char)(data[i] ^ encryptionKey[i % encryptionKey.Length]);
            }
            return new string(result);
        }

        // Допоміжні методи для збереження конкретних даних
        public void SavePlayerName(string name)
        {
            _currentData.playerName = name;
            SaveGame();
        }

        public void SaveLastLevel(int level)
        {
            _currentData.lastLevel = level;
            SaveGame();
        }
    }
}