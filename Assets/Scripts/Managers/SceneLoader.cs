using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public static string sceneToLoad = "GameScene";

    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider loadingBar;

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
        StartCoroutine(AnimateLoadingDots());
    }

    IEnumerator LoadTargetScene()
    {
        yield return new WaitForSeconds(0.3f);

        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
        {
            loadingBar.value = asyncOp.progress;
            yield return null;
        }

        // Останній крок
        loadingBar.value = 1f;
        yield return new WaitForSeconds(0.5f);
        asyncOp.allowSceneActivation = true;
    }

    IEnumerator AnimateLoadingDots()
    {
        string baseText = "Завантаження";
        while (true)
        {
            for (int i = 0; i <= 3; i++)
            {
                loadingText.text = baseText + new string('.', i);
                yield return new WaitForSeconds(0.4f);
            }
        }
    }
}
