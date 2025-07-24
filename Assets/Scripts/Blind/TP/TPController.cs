using System;
using ProtoLib.Scripting;
using UnityEngine;

namespace Blind.TP
{
    public class TPController : ScriptComponent<ITPScriptComponent>, ITPScriptComponent
    {
        private TPInputs _inputs;

        [Header("Player & Camera References")]
        [Tooltip("The transform that the camera will pivot around. Should follow the player.")]
        [SerializeField] private Transform cameraPivot;
        [Tooltip("The visual representation of the player that will rotate.")]
        [SerializeField] private Transform playerModel;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float moveAcceleration = 10f;
        [SerializeField] private float moveDeceleration = 10f;
        [SerializeField] private float airMoveAcceleration = 2f;
        [SerializeField] [Range(0, 1)] private float rotationSmoothTime = 0.12f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravityMultiplier = 2.5f;

        [Header("Slope & Step Handling")]
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float slideForce = 5f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("Camera Control")]
        [SerializeField] private float cameraFollowSpeed = 15f;
        [SerializeField] private float mouseSensitivity = 0.5f;
        [SerializeField] private Vector2 cameraPitchMinMax = new Vector2(-35, 80);

        // Component References
        private Rigidbody _rb;
        private CapsuleCollider _collider;
        private Camera _mainCamera;
        
        // Input State
        private Vector2 _moveVec, _lookVec;
        private bool _isSprinting;
        private bool _jumpQueued;

        // Physics State
        private bool _isGrounded;
        private bool _isSliding;
        private Vector3 _groundNormal;
        private Rigidbody _groundRigidbody; 
        private Vector3 _platformVelocity;
        
        // Look & Rotation State
        private float _cameraYaw;
        private float _cameraPitch;
        private float _turnSmoothVelocity;

        protected void Start()
        {
            _inputs = this.ScriptManager.GetScriptComponent<TPInputs>();
            
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            _mainCamera = Camera.main;

            _rb.freezeRotation = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            SubscribeToInputs();
        }
        
        private void SubscribeToInputs()
        {
            _inputs.API.Get<Action<Vector2>>(TPInputs.CallbackKeys.MoveVector).Subscribe(OnMoveInput);
            _inputs.API.Get<Action<Vector2>>(TPInputs.CallbackKeys.MouseVector).Subscribe(OnLookInput);
            _inputs.API.Get<Action>(TPInputs.CallbackKeys.Jump).Subscribe(OnJumpInput);
            _inputs.API.Get<Action<bool>>(TPInputs.CallbackKeys.Sprint).Subscribe(OnSprintInput);
        }

        #region Input Handling
        private void OnMoveInput(Vector2 moveVec) => _moveVec = moveVec;
        private void OnLookInput(Vector2 lookVec) => _lookVec = lookVec;
        private void OnSprintInput(bool isSprinting) => _isSprinting = isSprinting;
        private void OnJumpInput() => _jumpQueued = true;
        #endregion

        private void Update()
        {
            HandleCameraRotation();
        }

        private void FixedUpdate()
        {
            GroundCheck();
            HandleSliding();
            HandleMovementAndRotation();
            HandleJump();
            HandleGravity();
        }

        private void LateUpdate()
        {
            HandleCameraPosition();
        }
        
        #region Camera Logic
        private void HandleCameraRotation()
        {
            _cameraYaw += _lookVec.x * mouseSensitivity;
            _cameraPitch -= _lookVec.y * mouseSensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch, cameraPitchMinMax.x, cameraPitchMinMax.y);

            cameraPivot.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
        }

        private void HandleCameraPosition()
        {
            // Smoothly move the camera pivot to the player's position
            cameraPivot.position = Vector3.Lerp(cameraPivot.position, transform.position, cameraFollowSpeed * Time.fixedDeltaTime);
        }
        #endregion
        
        #region Movement & Physics Logic
        private void GroundCheck()
        {
            float sphereRadius = _collider.radius * 0.9f;
            float checkDist = (_collider.height / 2f) - sphereRadius + groundCheckDistance;
            
            if (Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out RaycastHit hit, checkDist, groundLayer, QueryTriggerInteraction.Ignore))
            {
                _isGrounded = true;
                _groundNormal = hit.normal;
                _groundRigidbody = hit.rigidbody;
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
                _groundRigidbody = null;
            }
        }
        
        private void HandleMovementAndRotation()
        {
            _platformVelocity = _groundRigidbody != null ? _groundRigidbody.linearVelocity : Vector3.zero;

            // Calculate movement direction relative to the camera's orientation
            Vector3 cameraForward = _mainCamera.transform.forward;
            Vector3 cameraRight = _mainCamera.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            
            Vector3 desiredMoveDirection = (cameraForward.normalized * _moveVec.y + cameraRight.normalized * _moveVec.x);

            // --- Player Model Rotation ---
            if (desiredMoveDirection.magnitude > 0.1f)
            {
                // Smoothly rotate the player model to face the direction of movement
                float targetAngle = Mathf.Atan2(desiredMoveDirection.x, desiredMoveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(playerModel.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, rotationSmoothTime);
                playerModel.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // --- Rigidbody Movement ---
            float currentSpeed = _isSprinting ? sprintSpeed : moveSpeed;
            Vector3 targetVelocity = desiredMoveDirection * currentSpeed;
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, _groundNormal);
            targetVelocity += _platformVelocity;
            
            Vector3 currentHorizontalVelocity = _rb.linearVelocity;
            currentHorizontalVelocity.y = 0;
            Vector3 velocityDifference = targetVelocity - currentHorizontalVelocity;
            
            float acceleration = _moveVec.magnitude > 0.1f ? moveAcceleration : moveDeceleration;
            
            if (_isGrounded && !_isSliding)
            {
                _rb.AddForce(velocityDifference * acceleration, ForceMode.Acceleration);
            }
            else if (!_isGrounded && _moveVec.magnitude > 0.1f)
            {
                // Allow some air control
                _rb.AddForce(desiredMoveDirection * airMoveAcceleration, ForceMode.Acceleration);
            }
        }

        private void HandleSliding()
        {
            float slopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
            _isSliding = slopeAngle > maxSlopeAngle;

            if (_isSliding && _isGrounded)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundNormal);
                _rb.AddForce(slideDirection * slideForce, ForceMode.Acceleration);
            }
        }
        
        private void HandleJump()
        {
            if (_jumpQueued && _isGrounded && !_isSliding)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _rb.AddForce(_platformVelocity, ForceMode.VelocityChange);
            }
            _jumpQueued = false;
        }
        
        private void HandleGravity()
        {
            if (!_isGrounded)
            {
                _rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
            }
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (_collider == null) return;
            
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            float sphereRadius = _collider.radius * 0.9f;
            float checkDist = (_collider.height / 2f) - sphereRadius + groundCheckDistance;
            Vector3 spherePos = transform.position + Vector3.down * checkDist;
            Gizmos.DrawWireSphere(spherePos, sphereRadius);
        }
    }
}