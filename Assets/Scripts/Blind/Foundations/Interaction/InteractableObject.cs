using System;
using FacetAPI.Runtime.Core;
using UnityEngine;

namespace Blind.Foundations.Interaction
{
    public class InteractableObject : MonoBehaviour, IInteractableObject
    {
        public static class CallbackKeys
        {
            public const string OnInteractStart = "OnInteractStart";
            public const string OnInteractEnd = "OnInteractEnd";
        }

        [Header("Interaction Settings")] public bool Interactable = true;
        [SerializeField] private InteractableObjectData interactableObjectData;
        [SerializeField] private string interactHint;
        [SerializeField] private float interactDuration = 1f;
        [SerializeField] private bool showOutline = true;
        [SerializeField] private bool instantInteract = true;
        [SerializeField] private bool destroyAfterInteraction = true;
        
        public FacetApi API {get; private set;}

        protected virtual void Awake()
        {
            API = new FacetApi();
            API.CreateCallback<Action>(CallbackKeys.OnInteractStart);
            API.CreateCallback<Action>(CallbackKeys.OnInteractEnd);
        }

        public virtual void OnInteractStart()
        {
            
        }

        public virtual void OnInteractEnd()
        {
            Destroy(gameObject);
        }
    }
}