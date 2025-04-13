using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Префаби Панелей")]
    public GameObject mainMenuPanelPrefab;
    public GameObject loadingPanelPrefab;
    public GameObject gameplayPanelPrefab;

    [Header("Canvas для UI")]
    public Transform panelParent; // Сюди інстанціюються панелі (UICanvas_Root)

    [Header("Fade")]
    public FadeController fadeController;

    private Dictionary<string, GameObject> panelInstances = new();
    private GameObject currentPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
       
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Logger.Log($"📍 Активна мапа: {PlayerInput.all[0].currentActionMap.name}");
        switch (scene.name)
        {
            case "MainMenu":
                ShowPanel(mainMenuPanelPrefab);
                break;
            case "LoadingScene":
                ShowPanel(loadingPanelPrefab);
                break;
            case "GameScene":
                ShowPanel(gameplayPanelPrefab);
                break;
            case "Startup":
                
                break;
            default:
                Logger.LogWarning($"UIManager: Scene {scene.name} не має відповідної панелі.");
                HideAll();
                break;
        }
    }

    public void ShowPanel(GameObject panelPrefab)
    {
        HideAll();

        if (!panelInstances.TryGetValue(panelPrefab.name, out var instance))
        {
            instance = Instantiate(panelPrefab, panelParent);
            panelInstances[panelPrefab.name] = instance;
        }

        instance.SetActive(true);
        currentPanel = instance;
    }

    public void HideAll()
    {
        foreach (var panel in panelInstances.Values)
        {
            if (panel != null) panel.SetActive(false);
        }

        currentPanel = null;
    }
}
