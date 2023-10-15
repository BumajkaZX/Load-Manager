namespace LoadManager
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UniRx;
    using System.Linq;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using ApplicationStart;

    /// <summary>
    /// Load manager with conditions
    /// </summary>
    public class LoadManager : MonoBehaviour
    {
        /// <summary>
        /// Instance
        /// </summary>
        public static LoadManager Instance = default;

        public string LoadScene = default;

        public string MenuScene = default;

        /// <summary>
        /// On load complete
        /// </summary>
        [HideInInspector]
        public ReactiveProperty<bool> LoadComplete = new ReactiveProperty<bool>();

        [HideInInspector]
        public ReactiveProperty<bool> IsAvailableLoad = new ReactiveProperty<bool>(true);

        [HideInInspector]
        public ReactiveProperty<LoadType> CurrentLoadType = new ReactiveProperty<LoadType>();

        [SerializeField, Min(1)]
        private float _defaultServiceTimeout = 5;

        [SerializeField]
        private LoadType _loadType = LoadType.ThroughAfterLoad;

        [SerializeField]
        private StartLogosController _logosController = default;

        private event Action _onSceneStart = delegate { };

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private async void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            await InitScripts();

            await _logosController.Init();

            await LoadLevel(MenuScene, _loadType);
        }

        /// <summary>
        /// Set IsLoadAvailable true, if wait for action - true
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="waitForTouch"></param>
        /// <returns></returns>
        public async Task LoadLevel(string sceneName, LoadType loadType)
        {
            try
            {
                _onSceneStart = delegate { };

                if (loadType == LoadType.WaitAction || loadType == LoadType.ThroughAfterLoad)
                {
                    SceneManager.LoadSceneAsync(LoadScene, LoadSceneMode.Single);
                }

                AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadType == LoadType.WaitAction || loadType == LoadType.ThroughAfterLoad ? LoadSceneMode.Additive : LoadSceneMode.Single);

                await WaitLoad(sceneLoad);

                CurrentLoadType.SetValueAndForceNotify(loadType);

                Debug.Log("Init load");

                if (loadType == LoadType.Through)
                {

                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

                    await InitScripts();

                    LoadComplete.SetValueAndForceNotify(true);

                    _onSceneStart();
                }

                if (loadType == LoadType.WaitAction)
                {
                    IsAvailableLoad.Value = false;

                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

                    await InitScripts();

                    LoadComplete.SetValueAndForceNotify(true);

                    await WaitAction();

                    AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(LoadScene);

                    await WaitLoad(sceneUnload);

                    _onSceneStart();

                    Debug.Log("Scene Active");

                }

                if(loadType == LoadType.ThroughAfterLoad)
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

                    await InitScripts();

                    LoadComplete.SetValueAndForceNotify(true);

                    AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(LoadScene);

                    await WaitLoad(sceneUnload);

                    _onSceneStart();

                    Debug.Log("Scene Active");
                }

                LoadComplete.Value = false;
            }
            catch(Exception ex)
            {
                Debug.Log("Cancel load or quit app: error " + ex.Message);
            }
        }

        private async Task WaitLoad(AsyncOperation sceneLoad)
        {
            if (_tokenSource.Token.IsCancellationRequested || sceneLoad == null)
            {
                return;
            }
            while (!sceneLoad.isDone) 
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
                Debug.LogWarning(sceneLoad.progress);
                await Task.Yield();
            }
        }

        private async Task WaitAction()
        {
            while (!IsAvailableLoad.Value)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
                await Task.Yield();
            }
        }

        /// <summary>
        /// Doesn't reinit blocked conditions 
        /// </summary>
        /// <returns></returns>
        private async Task InitScripts()
        {
            var conditions = FindObjectsOfType<MonoBehaviour>(true).OfType<ILoadingCondition>();

            var listconditions = conditions.OrderBy(condition => condition.Order).ToList();

            CancellationTokenSource tokenSourceConditions = new CancellationTokenSource();

            foreach (ILoadingCondition condition in listconditions)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
                if (condition.IsInited)
                {
                    continue;
                }

                Debug.Log("Init load " + condition.Name);
                try
                {
                    Task<Action> task = condition.Initialization(tokenSourceConditions.Token);
                    var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(condition.ServiceTimeout == null ? _defaultServiceTimeout : condition.ServiceTimeout.Value), tokenSourceConditions.Token));
                    if (completedTask != task)
                    {
                        Debug.LogWarning("block " + condition.Name);
                    }
                    else
                    {
                        Debug.Log($"Complete {condition.Name}");
                        if (task.Result != null)
                        {
                            _onSceneStart += task.Result;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.LogError($"Error with {condition.Name}: {ex.Message}");
                }

                await Task.Yield();
            }

            tokenSourceConditions.Cancel();
        }

        private void OnDestroy() => _tokenSource.Cancel();
        private void OnApplicationQuit() => OnDestroy();
    }
}
