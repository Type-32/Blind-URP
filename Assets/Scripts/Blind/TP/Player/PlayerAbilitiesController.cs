using System;
using System.Collections;
using ProtoLib.Scripting;
using UnityEngine;

namespace Blind.TP.Player
{
    public class PlayerAbilitiesController : ScriptComponent<ITPScriptComponent>, ITPScriptComponent
    {
        [SerializeField] private GameObject heartfireEffectSphere;
        [SerializeField] private float showHeartfireDelay = 1.0f;
        [SerializeField] private float hideHeartfireDelay = 1.0f;

        public bool UsingHeartfire { get; private set; } = false;
        
        private bool _heartfireDelayInProcess = false;
        private TPInputs _inputs;

        protected void Start()
        {
            _inputs = ScriptManager.GetScriptComponent<TPInputs>();
            
            _inputs.API.Get<Action>(TPInputs.CallbackKeys.Illuminate).Subscribe(() =>
            {
                if (UsingHeartfire)
                    HideHeartfire();
                else
                    ShowHeartfire();
            });
            
            StartCoroutine(SetHeartfireState(UsingHeartfire, 0f));
        }

        public void ShowHeartfire()
        {
            if (_heartfireDelayInProcess || UsingHeartfire) return;
            StartCoroutine(SetHeartfireState(true, showHeartfireDelay));
        }

        public void HideHeartfire()
        {
            if (_heartfireDelayInProcess || !UsingHeartfire) return;
            StartCoroutine(SetHeartfireState(false, hideHeartfireDelay));
        }

        private IEnumerator SetHeartfireState(bool state, float delay)
        {
            _heartfireDelayInProcess = true;
            if (!state)
            {
                heartfireEffectSphere.SetActive(false);
                UsingHeartfire = false;
            } // When using heartfire, the effect collider gets enabled after the delay
            // When hiding the heartfire, the effect collider gets disabled before the delay
            yield return new WaitForSeconds(delay);
            UsingHeartfire = state;
            heartfireEffectSphere.SetActive(state);
            _heartfireDelayInProcess = false;
        }
    }
}