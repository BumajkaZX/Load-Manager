namespace LoadManager.ApplicationStart
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEngine;

    public class ExampleStartLogos : StartLogosController
    {
        [SerializeField, Range(1, 10)]
        private float _logoTime = 2f;

        [SerializeField, Range(0.1f, 5f)]
        private float _fadeTime = 0.2f;

        private List<LogosOrder> _canvases = default;


        private void Awake()
        {
            _canvases = new List<LogosOrder>(FindObjectsOfType<LogosOrder>(true).OrderBy(_ => _.Order));

            foreach (LogosOrder canvas in _canvases)
            {
                canvas.gameObject.SetActive(false);
            }
        }

        public override async Task Init()
        {
            foreach (LogosOrder canvas in _canvases)
            {
                canvas.CanvasGroup.alpha = 0;
                canvas.gameObject.SetActive(true);

                StartCoroutine(Fade(canvas.CanvasGroup, false));
                await Task.Delay((int)(1000 * _fadeTime));

                await Task.Delay((int)(1000 * _logoTime));

                StartCoroutine(Fade(canvas.CanvasGroup, true));
                await Task.Delay((int)(1000 * _fadeTime));

                canvas.gameObject.SetActive(false);
            }
        }

        private IEnumerator Fade(CanvasGroup canvas, bool inverse)
        {
            float fade = 0;
            float min = inverse ? 1 : 0;
            float max = inverse ? 0 : 1;
            while (fade < _fadeTime)
            {
                canvas.alpha = Mathf.Lerp(min, max, fade / _fadeTime);
                fade += Time.deltaTime;
                yield return null;
            }
        }
    }
}
