namespace LoadManager.ApplicationStart
{
    using UnityEngine;
    using System.Threading.Tasks;

    public abstract class StartLogosController : MonoBehaviour
    {
        public abstract Task Init();
    }
}
