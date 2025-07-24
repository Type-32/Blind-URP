using UnityEngine;

namespace ProtoLib.Scripting
{
    public abstract class ScriptComponent<T> : MonoBehaviour, IScriptComponent where T : IScriptComponent
    {
        protected ScriptManager<T> ScriptManager;
        [SerializeField] protected bool collectManagerOnAwake = true;

        protected virtual void Awake()
        {
            if (collectManagerOnAwake)
                CollectManager();
        }

        public virtual void CollectManager()
        {
            ScriptManager = GetComponentInParent<ScriptManager<T>>();
        }

        public void SetManager(ScriptManager<T> manager)
        {
            ScriptManager = manager;
        }
    }
}