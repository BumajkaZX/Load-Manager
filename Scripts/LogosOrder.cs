namespace LoadManager.ApplicationStart
{
    using UnityEngine;

    public class LogosOrder : MonoBehaviour
    {
        public CanvasGroup CanvasGroup => _canvasGroup;

        public int Order => _order;

        [SerializeField]
        private int _order = 0;

        [SerializeField]
        private CanvasGroup _canvasGroup = default;
    }
}
