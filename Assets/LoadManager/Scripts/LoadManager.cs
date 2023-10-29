namespace LoadManager
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Linq;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using ApplicationStart;
    using System.Collections;

    /// <summary>
    /// Load manager with conditions
    /// </summary>
    public class LoadManager : MonoBehaviour
    {
        private const float MAX_SCENE_LOAD_PROGRESS = 0.25f;

        /// <summary>
        /// Instance
        /// </summary>
        public static LoadManager Instance = default;

        [SceneInfo]
        public string LoadScene = default;

        [SceneInfo]
        public string MenuScene = default;

        public LoadType CurrentLoadType { get; private set; }

        public Action<bool> IsAvailableLoad { get; private set; } = delegate { };

        public event Action<float> LoadProgress = delegate { };

        public bool LoadComplete { get; private set; } = false;

        [SerializeField, Min(1)]
        private float _defaultServiceTimeout = 5;

        [SerializeField]
        private LoadType _loadType = LoadType.ThroughAfterLoad;

        [SerializeField, Header("Additional")]
        private StartLogosController _logosController = default;

        private event Action _onSceneStart = delegate { };

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private bool _isAvailableLoad = false;

        private float _loadProgress = default;

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

            IsAvailableLoad += _ => _isAvailableLoad = _;
 
            if (_logosController != null)
            {
                await _logosController.Init();
            }

            await InitScripts();

            await LoadLevel(MenuScene, _loadType);
        }

        /// <summary>
        /// Invoke IsLoadAvailable true, if wait for action - true
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadType"></param>
        /// <returns></returns>
        public async Task LoadLevel(string sceneName, LoadType loadType)
        {
            try
            {
                _onSceneStart = delegate { };

                CurrentLoadType = loadType;

                LoadProgress(0);

                _loadProgress = 0;

                StartCoroutine(LoadLerp());

                if (loadType == LoadType.WaitAction || loadType == LoadType.ThroughAfterLoad)
                {
                    SceneManager.LoadSceneAsync(LoadScene, LoadSceneMode.Single);
                }

                AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(sceneName, loadType == LoadType.WaitAction || loadType == LoadType.ThroughAfterLoad ? LoadSceneMode.Additive : LoadSceneMode.Single);

                await WaitLoad(sceneLoad);

                switch (loadType)
                {
                    case LoadType.Through:

                        await LoadThrough(sceneName);

                        break;

                    case LoadType.WaitAction:

                        await LoadWaitAction(sceneName);

                        break;

                    case LoadType.ThroughAfterLoad:

                        await LoadThroughAfterLoad(sceneName);

                        break;
                }

                StopAllCoroutines();

            }
            catch (Exception ex)
            {
                Log("Cancel load or quit app: error " + ex.Message);
            }
        }

        private async Task LoadThrough(string sceneName)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            await InitScripts();

            LoadComplete = true;

            _onSceneStart();
        }

        private async Task LoadWaitAction(string sceneName)
        {
            IsAvailableLoad(false);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            await InitScripts();

            LoadComplete = true;

            await WaitAction();

            AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(LoadScene);

            await WaitLoad(sceneUnload);

            _onSceneStart();
        }

        private async Task LoadThroughAfterLoad(string sceneName)
        {
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

            await InitScripts();

            LoadComplete = true;

            AsyncOperation sceneUnload = SceneManager.UnloadSceneAsync(LoadScene);

            await WaitLoad(sceneUnload);

            _onSceneStart();
        }

        private async Task WaitAction()
        {
            while (!_isAvailableLoad)
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                await Task.Yield();
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

                float currentProgress = Mathf.Lerp(0, MAX_SCENE_LOAD_PROGRESS, sceneLoad.progress);

                _loadProgress = currentProgress;

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

                try
                {
                    Task<Action> task = condition.Initialization(tokenSourceConditions.Token);
                    var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(condition.ServiceTimeout == null ? _defaultServiceTimeout : condition.ServiceTimeout.Value), tokenSourceConditions.Token));
                    if (completedTask != task)
                    {
                        Log("Block " + condition.Name);
                    }
                    else
                    {
                        Log("Complete " + condition.Name);

                        if (task.Result != null)
                        {
                            _onSceneStart += task.Result;
                        }
                    }

                    _loadProgress = Mathf.Lerp(MAX_SCENE_LOAD_PROGRESS, 1, (listconditions.IndexOf(condition) + 1) / (float)listconditions.Count);
                }
                catch (Exception ex)
                {
                    Log($"Error with {condition.Name}: {ex.Message}");
                }

                await Task.Yield();
            }

            tokenSourceConditions.Cancel();
        }

        //TODO: Create different animation types
        private IEnumerator LoadLerp()
        {
            float baseProgress = 0;

            while (baseProgress < 1)
            {
                float progress = Mathf.Lerp(baseProgress, _loadProgress, _loadProgress == 1 ? Time.deltaTime * 10 : Time.deltaTime); //10 - random number to increase animation speed on completed load

                baseProgress = progress;

                yield return null;

                LoadProgress(progress);
            }
        }

        private void Log(string message)
        {

#if LOAD_DEBUG

            Debug.LogWarning(message);

#endif

        }

        private void OnDestroy() => _tokenSource.Cancel();
        private void OnApplicationQuit() => OnDestroy();
    }
}
