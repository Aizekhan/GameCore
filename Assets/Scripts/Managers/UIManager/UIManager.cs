﻿// Assets/Scripts/Managers/UIManager/UIManager.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Core
{
    /// <summary>
    /// Менеджер для управління UI панелями та їх відображенням
    /// </summary>
    public class UIManager : MonoBehaviour, IService, IInitializable
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Префаби Панелей")]
        [SerializeField] private GameObject mainMenuPanelPrefab;
        [SerializeField] private GameObject loadingPanelPrefab;
        [SerializeField] private GameObject gameplayPanelPrefab;
        [SerializeField] private GameObject settingsPanelPrefab;

        [Header("Canvas для UI")]
        [SerializeField] private Transform panelParent; // Сюди інстанціюються панелі (UICanvas_Root)

        [Header("Fade")]
        [SerializeField] private FadeController fadeController;

        // Словник для зберігання інстансів панелей
        private readonly Dictionary<string, UIPanel> _panelInstances = new Dictionary<string, UIPanel>();
        private UIPanel _currentPanel;

        // IInitializable implementation
        public bool IsInitialized { get; private set; }
        public int InitializationPriority => 80; // Високий пріоритет, але нижче за ServiceLocator і PlatformDetector

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
                ServiceLocator.Instance.RegisterService<UIManager>(this).ConfigureAwait(false);
            }
            else
            {
                CoreLogger.LogWarning("UI", "ServiceLocator not found. UIManager will function in standalone mode.");
                Initialize().ConfigureAwait(false);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public async Task Initialize()
        {
            if (IsInitialized) return;

            // Переконуємося, що у нас є батьківський об'єкт для панелей
            if (panelParent == null)
            {
                var canvasObj = GameObject.Find("UICanvas_Root");
                if (canvasObj != null)
                {
                    panelParent = canvasObj.transform;
                }
                else
                {
                    CoreLogger.LogWarning("UI", "Panel parent not assigned and UICanvas_Root not found!");
                }
            }

            // Підписуємося на події
            EventBus.Subscribe("UI/ShowPanel", OnShowPanelEvent);
            EventBus.Subscribe("UI/HidePanel", OnHidePanelEvent);
            EventBus.Subscribe("UI/HideAllPanels", _ => HideAll());

            IsInitialized = true;
            CoreLogger.Log("UI", "UIManager initialized");

            await Task.CompletedTask;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CoreLogger.Log("UI", $"Scene loaded: {scene.name}");

            // Перевіряємо наявність PlayerInput і його поточну карту
            if (UnityEngine.InputSystem.PlayerInput.all.Count > 0)
            {
                var currentMap = UnityEngine.InputSystem.PlayerInput.all[0].currentActionMap.name;
                CoreLogger.Log("UI", $"Active input map: {currentMap}");
            }

            // Показуємо відповідну панель для сцени
            switch (scene.name)
            {
                case "MainMenu":
                    ShowPanel(mainMenuPanelPrefab).ConfigureAwait(false);
                    break;
                case "LoadingScene":
                    ShowPanel(loadingPanelPrefab).ConfigureAwait(false);
                    break;
                case "GameScene":
                    ShowPanel(gameplayPanelPrefab).ConfigureAwait(false);
                    break;
                case "Startup":
                    // Стартова сцена не потребує UI
                    break;
                default:
                    CoreLogger.LogWarning("UI", $"Scene {scene.name} does not have a matching panel.");
                    HideAll();
                    break;
            }
        }

        public async Task<UIPanel> ShowPanel(GameObject panelPrefab, bool withAnimation = true)
        {
            if (panelPrefab == null)
            {
                CoreLogger.LogError("UI", "Cannot show null panel prefab!");
                return null;
            }

            // Отримуємо або створюємо інстанс панелі
            UIPanel panel = GetOrCreatePanelInstance(panelPrefab);

            if (panel == null)
            {
                CoreLogger.LogError("UI", $"Failed to create panel from prefab: {panelPrefab.name}");
                return null;
            }

            // Якщо є fadeController і withAnimation, робимо перехід з затемненням
            if (fadeController != null && withAnimation)
            {
                await fadeController.FadeIn();
            }

            // Приховуємо поточну панель, якщо вона відрізняється від нової
            if (_currentPanel != null && _currentPanel != panel)
            {
                if (withAnimation)
                    await _currentPanel.HideAnimated();
                else
                    _currentPanel.Hide();
            }

            // Показуємо нову панель
            if (withAnimation)
                await panel.ShowAnimated();
            else
                panel.Show();

            // Якщо був fadeController, прибираємо затемнення
            if (fadeController != null && withAnimation)
            {
                await fadeController.FadeOut();
            }

            _currentPanel = panel;
            EventBus.Emit("UI/PanelChanged", panel.PanelName);

            return panel;
        }
       
        public async Task<UIPanel> ShowPanelByName(string panelName, bool withAnimation = true)
        {
            // Перевіряємо, чи панель вже створена
            foreach (var panel in _panelInstances.Values)
            {
                if (panel != null && panel.PanelName == panelName)
                {
                    return await ShowPanel(panel.gameObject, withAnimation);
                }
            }

            // Якщо ні, намагаємось завантажити її з Resources
            GameObject prefab = Resources.Load<GameObject>($"UI/Panels/{panelName}");
            if (prefab != null)
            {
                return await ShowPanel(prefab, withAnimation);
            }

            CoreLogger.LogError("UI", $"Panel with name {panelName} not found in panels or Resources!");
            return null;
        }
        private UIPanel GetOrCreatePanelInstance(GameObject panelPrefab)
        {
            string prefabName = panelPrefab.name;

            // Перевіряємо, чи існує вже інстанс
            if (_panelInstances.TryGetValue(prefabName, out var panel))
            {
                if (panel != null)
                    return panel;

                // Якщо інстанс був знищений, видаляємо його з словника
                _panelInstances.Remove(prefabName);
            }

            // Створюємо новий інстанс
            GameObject instance = Instantiate(panelPrefab, panelParent);
            UIPanel uiPanel = instance.GetComponent<UIPanel>();

            if (uiPanel == null)
            {
                CoreLogger.LogWarning("UI", $"Panel prefab {prefabName} doesn't have UIPanel component!");
                // Спробуємо додати базовий компонент
                uiPanel = instance.AddComponent<DefaultUIPanel>();
            }

            _panelInstances[prefabName] = uiPanel;
            return uiPanel;
        }

        public void HideAll()
        {
            foreach (var panel in _panelInstances.Values)
            {
                if (panel != null && panel.IsActive)
                    panel.Hide();
            }

            _currentPanel = null;
        }

        public UIPanel GetCurrentPanel()
        {
            return _currentPanel;
        }

        private void OnShowPanelEvent(object data)
        {
            if (data is string panelName)
            {
                // Знаходимо панель за іменем
                foreach (var panel in _panelInstances.Values)
                {
                    if (panel != null && panel.PanelName == panelName)
                    {
                        ShowPanel(panel.gameObject).ConfigureAwait(false);
                        return;
                    }
                }

                CoreLogger.LogWarning("UI", $"Panel with name {panelName} not found.");
            }
            else if (data is GameObject panelPrefab)
            {
                ShowPanel(panelPrefab).ConfigureAwait(false);
            }
        }

        private void OnHidePanelEvent(object data)
        {
            if (data is string panelName)
            {
                // Знаходимо панель за іменем
                foreach (var panel in _panelInstances.Values)
                {
                    if (panel != null && panel.PanelName == panelName)
                    {
                        if (panel.IsActive)
                            panel.Hide();
                        return;
                    }
                }
            }
            else if (data is UIPanel panel)
            {
                if (panel.IsActive)
                    panel.Hide();
            }
        }

        // Допоміжний клас для підтримки панелей без UIPanel компонента
        private class DefaultUIPanel : UIPanel
        {
            protected override void Awake()
            {
                base.Awake();
                panelName = gameObject.name;
            }
        }
    }
}