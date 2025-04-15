using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace GameCore.Core
{
    public class FadeController : MonoBehaviour
    {
        [Header("Fade Settings")]
        public Image fadeImage;
        public float fadeDuration = 0.4f;

        private bool isFading = false;

        private void Awake()
        {
            if (fadeImage == null)
            {
                fadeImage = GetComponentInChildren<Image>();
                CoreLogger.LogWarning("FadeController", "fadeImage not assigned, trying to auto-find.");
            }

            // Стартуємо повністю прозорими
            SetAlpha(0f);
            fadeImage.raycastTarget = false;
        }

        public async Task FadeToBlack()
        {
            await Fade(0f, 1f);
            fadeImage.raycastTarget = true;
        }

        public async Task FadeFromBlack()
        {
            await Fade(1f, 0f);
            fadeImage.raycastTarget = false;
        }

        private async Task Fade(float startAlpha, float endAlpha)
        {
            if (isFading || fadeImage == null)
                return;

            isFading = true;
            float time = 0f;
            Color color = fadeImage.color;

            while (time < fadeDuration)
            {
                float t = time / fadeDuration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, t);
                fadeImage.color = color;
                time += Time.deltaTime;
                await Task.Yield();
            }

            color.a = endAlpha;
            fadeImage.color = color;
            isFading = false;
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }
        }
    }
}
