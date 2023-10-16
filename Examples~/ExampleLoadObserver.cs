namespace LoadManager.Examples
{
 
    using UnityEngine;
    using UnityEngine.UI;

#if ALLOW_UNIRX

    using UniRx;

#endif

#if !ALLOW_UNIRX

    using System.Collections;

#endif

    public class ExampleLoadObserver : MonoBehaviour
    {
        [SerializeField]
        private Transform _loadImageTransform = default;

        [SerializeField]
        private Button _button = default;

        private LoadManager _manager = default;


#if ALLOW_UNIRX

        private CompositeDisposable _loadDis = new CompositeDisposable();

#endif

        private void OnEnable()
        {
            _manager = LoadManager.Instance;

            _loadImageTransform.gameObject.SetActive(true);
            _button.gameObject.SetActive(false);

#if ALLOW_UNIRX

            _manager.LoadComplete.Where(_ => _).Subscribe(_ => 
            {
                _loadImageTransform.gameObject.SetActive(false);

                if (_manager.CurrentLoadType.Value == LoadType.WaitAction)
                {
                    _button.gameObject.SetActive(true);

                    _button.OnClickAsObservable().Subscribe(_ =>
                    {
                        _manager.IsAvailableLoad.Value = true;
                        _loadDis.Clear();
                    }).AddTo(_loadDis);
                }
            }).AddTo(_loadDis);

#endif

#if !ALLOW_UNIRX

            StartCoroutine(WaitLoad());

#endif

        }

#if !ALLOW_UNIRX

        private void OnClick() => _manager.IsAvailableLoad.Invoke(true);

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

#endif

        private void OnDestroy()
        {

#if ALLOW_UNIRX

            _loadDis.Clear();

#endif

#if !ALLOW_UNIRX

            if (_manager.CurrentLoadType == LoadType.WaitAction)
            {
                _button.onClick.RemoveListener(OnClick);
            }

#endif

        }
    }
}
