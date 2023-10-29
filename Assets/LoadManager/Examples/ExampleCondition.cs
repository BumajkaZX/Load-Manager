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

        public async Task<Action> Initialization(CancellationToken token)
        {
            Debug.Log("Example inited");

            await Task.Yield();

            _isInited = true;

            return OnSceneStart;
        }

        private void OnSceneStart()
        {
            Debug.Log("Example start");
        }
    }
}
