using System;
using FacetAPI.Runtime.Core;
using ProtoLib.Scripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blind.FP
{
    public class FPInputs : ScriptComponent<IFPScriptComponent>, IFPScriptComponent
    {
        public static class CallbackKeys
        {
            public const string MoveVector = "FP.MoveVector";
            public const string MouseVector = "FP.MouseVector";
            public const string Jump = "FP.Jump";
            public const string Sprint = "FP.Sprint";
            public const string Illuminate = "FP.Illuminate";
        }
        
        private FPActionsMap _map;
        public FacetApi API { get; set; }

        protected override void Awake()
        {
            base.Awake();
            API = new FacetApi();
            API.CreateCallback<Action<Vector2>>(CallbackKeys.MoveVector);
            API.CreateCallback<Action<Vector2>>(CallbackKeys.MouseVector);
            API.CreateCallback<Action>(CallbackKeys.Jump);
            API.CreateCallback<Action<bool>>(CallbackKeys.Sprint);
            API.CreateCallback<Action>(CallbackKeys.Illuminate);
        }

        private void Start()
        {
            _map = new FPActionsMap();
        }

        private void OnEnable()
        {
            EnableInputs();
        }

        private void OnDisable()
        {
            DisableInputs();
        }

        public void EnableInputs()
        {
            if (_map == null)
                _map = new FPActionsMap();
            
            _map.Enable();
            _map.Player.Movement.performed += OnMove;
            _map.Player.Movement.canceled += OnMove;
            _map.Player.Look.performed += OnMouseMove;
            _map.Player.Look.canceled += OnMouseMove;
            
            _map.Player.Jump.performed += OnJump;
            _map.Player.Sprint.performed += OnSprint;
            _map.Player.Sprint.canceled += OnSprint;
            _map.Player.Illuminate.performed += OnIlluminate;
        }

        public void DisableInputs()
        {
            _map.Disable();
            _map.Player.Movement.performed -= OnMove;
            _map.Player.Movement.canceled -= OnMove;
            _map.Player.Look.performed -= OnMouseMove;
            _map.Player.Look.canceled -= OnMouseMove;
            
            _map.Player.Jump.performed -= OnJump;
            _map.Player.Sprint.performed -= OnSprint;
            _map.Player.Sprint.canceled -= OnSprint;
            _map.Player.Illuminate.performed -= OnIlluminate;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            API.Get<Action<Vector2>>(CallbackKeys.MoveVector).Invoke(context.ReadValue<Vector2>());
        }

        private void OnMouseMove(InputAction.CallbackContext context)
        {
            API.Get<Action<Vector2>>(CallbackKeys.MouseVector).Invoke(context.ReadValue<Vector2>());
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            API.Get<Action>(CallbackKeys.Jump).Invoke();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            API.Get<Action<bool>>(CallbackKeys.Sprint).Invoke(context.ReadValueAsButton());
        }

        private void OnIlluminate(InputAction.CallbackContext context)
        {
            API.Get<Action>(CallbackKeys.Illuminate).Invoke();
        }
    }
}