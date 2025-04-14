// Оновлений FadeController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace GameCore.Core
{
    public class FadeController : MonoBehaviour
    {
        public Image fadeImage;
        public float fadeDuration = 0.5f;

        private void Start()
        {
            FadeOut().ConfigureAwait(false);
        }

        public async Task FadeIn()
        {
            await Fade(0f, 1f);
        }

        public async Task FadeOut()
        {
            await Fade(1f, 0f);
        }

        private async Task Fade(float startAlpha, float endAlpha)
        {
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
        }
    }
}