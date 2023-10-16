namespace LoadManager
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public class ExampleCondition : MonoBehaviour, ILoadingCondition
    {
        public int Order => 0;

        public string Name => typeof(ExampleCondition).Name;

        public bool IsInited => _isInited;

        public float? ServiceTimeout => null;

        private bool _isInited = false;

        public Task<Action> Initialization(CancellationToken token)
        {
            Debug.LogError("Example inited");

            _isInited = true;

            return Task.FromResult<Action>(OnSceneStart);
        }

        private void OnSceneStart()
        {
            Debug.LogError("Example start");
        }
    }
}
