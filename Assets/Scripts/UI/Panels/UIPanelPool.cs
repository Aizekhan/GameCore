using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCore.Core
{
    /// <summary>
    /// Пул UI панелей для оптимізації їх повторного використання
    /// </summary>
    public class UIPanelPool : MonoBehaviour, IService, IInitializable
    {
        [SerializeField] private int defaultPoolSize = 1;
        [SerializeField] private bool autoExpandPool = true;

        private Dictionary<string, Queue<UIPanel>> _panelPools = new Dictionary<string, Queue<UIPanel>>();
        private Dictionary<string, int> _panelInUseCount = new Dictionary<string, int>();
        private UIPanelFactory _panelFactory;

        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 63; // Має бути після UIPanelFactory (пріоритет 60)

        public async Task Initialize()
        {
            _panelFactory = ServiceLocator.Instance.GetService<UIPanelFactory>();

            if (_panelFactory == null)
            {
                CoreLogger.LogError("UI", "UIPanelPool requires UIPanelFactory to be registered");
                return;
            }

            // Підписуємось на зміну сцени для управління пулами
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            IsInitialized = true;
            CoreLogger.Log("UI", "✅ UIPanelPool initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Отримує панель з пулу або створює нову, якщо пул порожній
        /// </summary>
        public async Task<UIPanel> GetPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                CoreLogger.LogError("UI", "Cannot get panel with empty name");
                return null;
            }

            // Ініціалізуємо пул для цього типу панелі, якщо його ще немає
            if (!_panelPools.ContainsKey(panelName))
            {
                _panelPools[panelName] = new Queue<UIPanel>();
                _panelInUseCount[panelName] = 0;
            }

            UIPanel panel = null;

            // Перевіряємо, чи є панель у пулі
            if (_panelPools[panelName].Count > 0)
            {
                panel = _panelPools[panelName].Dequeue();

                // Перевіряємо, чи панель ще існує (могла бути знищена)
                if (panel == null)
                {
                    return await GetPanel(panelName); // Рекурсивно пробуємо отримати іншу панель
                }

                panel.gameObject.SetActive(true);
                CoreLogger.Log("UI", $"Panel {panelName} obtained from pool");
            }
            else
            {
                // Створюємо нову панель через фабрику
                panel = _panelFactory.CreatePanel(panelName);

                if (panel == null)
                {
                    CoreLogger.LogError("UI", $"Failed to create panel: {panelName}");
                    return null;
                }

                CoreLogger.Log("UI", $"New panel {panelName} created (pool was empty)");
            }

            // Збільшуємо лічильник використання
            _panelInUseCount[panelName]++;

            // Якщо пул порожній, а autoExpandPool увімкнено, наперед створюємо панелі
            if (_panelPools[panelName].Count == 0 && autoExpandPool)
            {
                _ = PreloadPanel(panelName, defaultPoolSize).ConfigureAwait(false);
            }

            return panel;
        }

        /// <summary>
        /// Повертає панель до пулу
        /// </summary>
        public void ReturnToPool(UIPanel panel)
        {
            if (panel == null) return;

            string panelName = panel.name.Replace("(Clone)", "").Trim();

            // Скидаємо стан панелі
            panel.Reset();
            panel.gameObject.SetActive(false);

            // Ініціалізуємо пул, якщо він ще не існує
            if (!_panelPools.ContainsKey(panelName))
            {
                _panelPools[panelName] = new Queue<UIPanel>();
                _panelInUseCount[panelName] = 0;
            }

            // Додаємо панель до пулу
            _panelPools[panelName].Enqueue(panel);

            // Зменшуємо лічильник використання
            if (_panelInUseCount.ContainsKey(panelName) && _panelInUseCount[panelName] > 0)
            {
                _panelInUseCount[panelName]--;
            }

            CoreLogger.Log("UI", $"Panel {panelName} returned to pool");
        }

        /// <summary>
        /// Попередньо завантажує панелі в пул
        /// </summary>
        public async Task PreloadPanel(string panelName, int count = 1)
        {
            if (string.IsNullOrEmpty(panelName) || count <= 0) return;

            // Ініціалізуємо пул для цього типу панелі, якщо його ще немає
            if (!_panelPools.ContainsKey(panelName))
            {
                _panelPools[panelName] = new Queue<UIPanel>();
                _panelInUseCount[panelName] = 0;
            }

            // Створюємо вказану кількість панелей і додаємо їх до пулу
            for (int i = 0; i < count; i++)
            {
                UIPanel panel = _panelFactory.CreatePanel(panelName);

                if (panel != null)
                {
                    panel.Reset();
                    panel.gameObject.SetActive(false);
                    _panelPools[panelName].Enqueue(panel);

                    // Даємо можливість Unity обробити інші події перед продовженням
                    if (i % 3 == 0) await Task.Yield();
                }
            }

            CoreLogger.Log("UI", $"Preloaded {count} panels of type {panelName}");
        }

        /// <summary>
        /// Попередньо завантажує декілька типів панелей
        /// </summary>
        public async Task PreloadPanels(string[] panelNames, int countPerType = 1)
        {
            foreach (var panelName in panelNames)
            {
                await PreloadPanel(panelName, countPerType);
            }
        }

        /// <summary>
        /// Очищує пул панелей вказаного типу
        /// </summary>
        public void ClearPool(string panelName)
        {
            if (!_panelPools.ContainsKey(panelName)) return;

            // Знищуємо всі панелі в пулі
            while (_panelPools[panelName].Count > 0)
            {
                var panel = _panelPools[panelName].Dequeue();
                if (panel != null)
                {
                    Destroy(panel.gameObject);
                }
            }

            CoreLogger.Log("UI", $"Pool cleared for panel type: {panelName}");
        }

        /// <summary>
        /// Обробляє зміну сцени - очищує пули для сцено-залежних панелей
        /// </summary>
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Список типів глобальних панелей, які не потрібно очищувати
            string[] globalPanels = { "MainMenuPanel", "LoadingPanel", "SettingsPanel", "ErrorPanel" };

            // Очищуємо пули для типів панелей, які не є глобальними
            foreach (var panelType in _panelPools.Keys.ToList())
            {
                if (!globalPanels.Contains(panelType) && _panelInUseCount[panelType] == 0)
                {
                    ClearPool(panelType);
                }
            }
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            // Очищуємо всі пули
            foreach (var panelType in _panelPools.Keys.ToList())
            {
                ClearPool(panelType);
            }

            _panelPools.Clear();
            _panelInUseCount.Clear();
        }
    }
}