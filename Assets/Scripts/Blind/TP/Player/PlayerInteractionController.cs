using System;
using System.Collections.Generic;
using Blind.Foundations.Interaction;
using FacetAPI.Runtime.Core;
using ProtoLib.Scripting;
using UnityEngine;

namespace Blind.TP.Player
{
    public class PlayerInteractionController : ScriptComponent<ITPScriptComponent>, ITPScriptComponent
    {
        public static class CallbackKeys
        {
            public const string OnInteractStart = "OnInteractStart";
            public const string OnInteractEnd = "OnInteractEnd";
        }

        [SerializeField] private Transform raycastOrigin;
        [SerializeField] private float raycastDistance;
        [SerializeField] private bool raycastOrb = true;
        [SerializeField] private float raycastOrbRadius;
        
        public List<InteractableObject> interactableObjects = new List<InteractableObject>();
        public FacetApi API {get; private set;}

        private int focusedObjectIndex = -1;

        protected override void Awake()
        {
            base.Awake();
            API = new FacetApi();
            API.CreateCallback<Action>(CallbackKeys.OnInteractStart);
            API.CreateCallback<Action>(CallbackKeys.OnInteractEnd);
        }

        public void StartInteraction()
        {
            API.Get<Action>(CallbackKeys.OnInteractStart).Invoke();
        }

        public void StopInteraction()
        {
            API.Get<Action>(CallbackKeys.OnInteractEnd).Invoke();
        }

        protected void FixedUpdate()
        {
            CheckInteractables();
        }

        public void CheckInteractables()
        {
            if (interactableObjects.Count > 0) 
                interactableObjects.Clear();

            if (Physics.Raycast(raycastOrigin.position, raycastOrigin.forward, out RaycastHit hit, raycastDistance))
            {
                if (hit.collider.gameObject.TryGetComponent<InteractableObject>(out var focusedObject))
                {
                    interactableObjects.Add(focusedObject);
                    focusedObjectIndex = 0;
                }
                else
                    focusedObjectIndex = -1;
                if (raycastOrb)
                {
                    Collider[] colliders = Physics.OverlapSphere(hit.transform.position, raycastOrbRadius);
                    foreach (Collider collider in colliders)
                    {
                        if (collider.gameObject.TryGetComponent<InteractableObject>(out var interactableObject) &&
                            !interactableObjects.Contains(interactableObject))
                        {
                            
                            interactableObjects.Add(interactableObject);
                        }
                    }
                }
            }
            else
            {
                if (raycastOrb)
                {
                    Collider[] colliders = Physics.OverlapSphere(hit.transform.position, raycastOrbRadius);
                    foreach (Collider collider in colliders)
                    {
                        if (collider.gameObject.TryGetComponent<InteractableObject>(out var interactableObject) &&
                            !interactableObjects.Contains(interactableObject))
                        {
                            
                            interactableObjects.Add(interactableObject);
                        }
                    }
                }
            }
        }

        public InteractableObject GetNearestInteractable()
        {
            if (focusedObjectIndex >= 0) return interactableObjects[focusedObjectIndex];
            else
            {
                if (!raycastOrb) return null;
                float smallestDistance = 1000f;
                int index = -1;
                for (int i = 0; i < interactableObjects.Count; i++)
                {
                    if (smallestDistance > Vector3.Distance(interactableObjects[i].transform.position,
                            raycastOrigin.forward * raycastDistance + raycastOrigin.position))
                        index = i;
                }
                
                if (index >= 0)
                    return interactableObjects[index];
            }

            return null;
        }
    }
}