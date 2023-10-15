namespace LoadManager
{
    using UnityEngine;
    using UniRx;
    using UnityEngine.UI;

    public class LoadObserver : MonoBehaviour
    {
        [SerializeField]
        private Transform _loadImageTransform = default;

        [SerializeField]
        private Button _button = default;

        private CompositeDisposable _loadDis = new CompositeDisposable();

        private void OnEnable()
        {
            var manager = LoadManager.Instance;

            _loadImageTransform.gameObject.SetActive(true);
            _button.gameObject.SetActive(false);
      
            manager.LoadComplete.Where(_ => _).Subscribe(_ => 
            {
                _loadImageTransform.gameObject.SetActive(false);

                if (manager.CurrentLoadType.Value == LoadType.WaitAction)
                {
                    _button.gameObject.SetActive(true);

                    _button.OnClickAsObservable().Subscribe(_ =>
                    {
                        manager.IsAvailableLoad.Value = true;
                        _loadDis.Clear();
                    }).AddTo(_loadDis);
                }
            }).AddTo(_loadDis);

        }

        private void OnDestroy()
        {
            _loadDis.Clear();
        }
    }
}
