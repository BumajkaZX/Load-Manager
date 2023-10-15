namespace LoadManager.ApplicationStart
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UniRx;
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    public class StartLogosController : MonoBehaviour
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

        public async Task Init()
        {

            foreach(LogosOrder canvas in _canvases)
            {
                canvas.CanvasGroup.alpha = 0;
                canvas.gameObject.SetActive(true);
                await Observable.FromCoroutine(_ => Fade(canvas.CanvasGroup, false)).GetAwaiter();
                await Observable.Timer(TimeSpan.FromSeconds(_logoTime)).GetAwaiter();
                await Observable.FromCoroutine(_ => Fade(canvas.CanvasGroup, true)).GetAwaiter();
                canvas.gameObject.SetActive(false);
            }
        }

        private IEnumerator Fade(CanvasGroup canvas, bool inverse)
        {
            float fade = 0;
            float min = inverse ? 1 : 0;
            float max = inverse ? 0 : 1;
            while(fade < _fadeTime)
            {
                canvas.alpha = Mathf.Lerp(min, max, fade / _fadeTime);
                fade += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
