namespace LoadManager.Examples
{
 
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using System;

    public class ExampleLoadObserver : MonoBehaviour
    {
        [SerializeField]
        private Transform _loadImageTransform = default;

        [SerializeField]
        private Button _button = default;

        [SerializeField]
        private Slider _slider = default;

        private LoadManager _manager = default;

        private void OnEnable()
        {
            _manager = LoadManager.Instance;

            _loadImageTransform.gameObject.SetActive(true);
            _button.gameObject.SetActive(false);

            _manager.LoadProgress += OnChangeProgress;

            StartCoroutine(WaitLoad());
        }

        private void OnChangeProgress(float progress) => _slider.value = progress;

        private void OnClick() => _manager.IsAvailableLoad(true);

        private IEnumerator WaitLoad()
        {
            while(isActiveAndEnabled && !_manager.LoadComplete)
            {
                yield return null;
            }

            _loadImageTransform.gameObject.SetActive(false);
            _button.gameObject.SetActive(true);

            if (_manager.CurrentLoadType == LoadType.WaitAction)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnDestroy()
        {
            if (_manager.CurrentLoadType == LoadType.WaitAction)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }
    }
}
