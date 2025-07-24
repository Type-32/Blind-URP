using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoLib.Scripting
{
    public class ScriptManager<T> : MonoBehaviour where T : IScriptComponent
    {
        [Header("Manager Settings")]
        [SerializeField] protected bool collectComponentsOnAwake = true;

        protected Dictionary<Type, T> Components = new Dictionary<Type, T>();

        protected virtual void Awake()
        {
            if (collectComponentsOnAwake)
                CollectComponents();
        }

        public virtual void CollectComponents()
        {
            Components.Clear();
            var components = GetComponentsInChildren<T>();
            foreach (var component in components)
            {
                Components[component.GetType()] = component;
                if (component is ScriptComponent<T> scriptComponent)
                {
                    scriptComponent.SetManager(this);
                }
            }
        }

        public TComponent GetScriptComponent<TComponent>() where TComponent : T
        {
            if (Components.TryGetValue(typeof(TComponent), out T component))
            {
                return (TComponent)component;
            }
            return default;
        }
    }
}