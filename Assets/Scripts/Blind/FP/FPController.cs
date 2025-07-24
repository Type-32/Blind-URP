using System;
using ProtoLib.Scripting;
using UnityEngine;

namespace Blind.FP
{
    public class FPController : ScriptComponent<IFPScriptComponent>, IFPScriptComponent
    {
        private FPInputs _inputs;
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float moveAcceleration = 10f;
        [SerializeField] private float moveDeceleration = 10f; // Higher for snappy stops
        [SerializeField] private float airMoveAcceleration = 2f;
        
        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravityMultiplier = 2.5f; // For more satisfying gravity

        [Header("Slope & Step Handling")]
        [SerializeField] private float maxSlopeAngle = 45f;
        [SerializeField] private float slideForce = 5f;
        [SerializeField] private float stepHeight = 0.3f;
        [SerializeField] private float stepCheckDistance = 0.3f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("Look Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 0.5f;
        [SerializeField] private float upDownLookLimit = 80.0f;

        private Rigidbody _rb;
        private CapsuleCollider _collider;
        private float _verticalLookRotation;
        
        private Vector2 _moveVec, _mouseVec;
        private bool _isSprinting;
        private bool _jumpQueued;
        private bool _isGrounded;
        private bool _isSliding;
        private Vector3 _groundNormal;
        private Rigidbody _groundRigidbody; // For moving platforms
        private Vector3 _platformVelocity;
        
        protected void Start()
        {
            _inputs = this.ScriptManager.GetScriptComponent<FPInputs>();
            // Get the Rigidbody component attached to this GameObject
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            // Freeze rotation on the Rigidbody to prevent the player from tipping over
            _rb.freezeRotation = true;

            // Lock the cursor to the center of the screen and make it invisible
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            SubscribeToInputs();
        }
        
        private void SubscribeToInputs()
        {
            _inputs.API.Get<Action<Vector2>>(FPInputs.CallbackKeys.MoveVector).Subscribe(OnMoveInput);
            _inputs.API.Get<Action<Vector2>>(FPInputs.CallbackKeys.MouseVector).Subscribe(OnLookInput);
            _inputs.API.Get<Action>(FPInputs.CallbackKeys.Jump).Subscribe(OnJumpInput);
            _inputs.API.Get<Action<bool>>(FPInputs.CallbackKeys.Sprint).Subscribe(OnSprintInput);
        }
        
        private void OnMoveInput(Vector2 moveVec) => _moveVec = moveVec;
        private void OnSprintInput(bool isSprinting) => _isSprinting = isSprinting;
        private void OnJumpInput() => _jumpQueued = true;
        
        private void OnLookInput(Vector2 mouseVec)
        {
            float mouseX = mouseVec.x * mouseSensitivity;
            float mouseY = mouseVec.y * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            _verticalLookRotation -= mouseY;
            _verticalLookRotation = Mathf.Clamp(_verticalLookRotation, -upDownLookLimit, upDownLookLimit);
            cameraTransform.localEulerAngles = new Vector3(_verticalLookRotation, 0, 0);
        }
        
        private void GroundCheck()
        {
            // Use a sphere cast which is more robust than a raycast.
            float sphereRadius = _collider.radius * 0.9f;
            float checkDist = (_collider.height / 2f) - sphereRadius + groundCheckDistance;
            
            if (Physics.SphereCast(transform.position, sphereRadius, Vector3.down, out RaycastHit hit, checkDist, groundLayer, QueryTriggerInteraction.Ignore))
            {
                _isGrounded = true;
                _groundNormal = hit.normal;
                _groundRigidbody = hit.rigidbody; // Get the rigidbody of the ground object
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
                _groundRigidbody = null;
            }
        }
        
        private void HandleMovement()
        {
            // MOVING PLATFORM: Capture platform velocity
            _platformVelocity = _groundRigidbody != null ? _groundRigidbody.linearVelocity : Vector3.zero;

            // Calculate target velocity based on input
            float currentSpeed = _isSprinting ? sprintSpeed : moveSpeed;
            Vector3 targetVelocity = (transform.forward * _moveVec.y + transform.right * _moveVec.x) * currentSpeed;

            // Project target velocity onto the ground plane. Prevents "skiing" up slopes.
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, _groundNormal);
            
            // Add platform velocity to our target. Makes us move with the platform.
            targetVelocity += _platformVelocity;

            // Calculate the difference between current velocity and target velocity
            // Don't factor in Y velocity for acceleration calculation.
            Vector3 currentHorizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
            Vector3 velocityDifference = targetVelocity - currentHorizontalVelocity;
            
            // Choose acceleration or deceleration based on whether we are trying to move
            float acceleration = _moveVec.magnitude > 0.1f ? moveAcceleration : moveDeceleration;
            
            // Apply the force! This is the core of acceleration-based movement.
            // We only apply force if grounded or if there is input (for air control)
            if (_isGrounded && !_isSliding)
            {
                // Extra check for steps before applying movement force
                HandleSteps(velocityDifference);
                _rb.AddForce(velocityDifference * acceleration, ForceMode.Acceleration);
            }
            else if (!_isGrounded)
            {
                _rb.AddForce((targetVelocity / currentSpeed).normalized * airMoveAcceleration, ForceMode.Acceleration);
            }
        }

        private void HandleSteps(Vector3 velocityDifference)
        {
            // Simple Step-up logic
            if (_moveVec.magnitude < 0.1f) return;
            
            Vector3 stepCheckStart = transform.position + new Vector3(0, 0.05f, 0); // slightly above feet
            Vector3 stepCheckDir = new Vector3(velocityDifference.x, 0, velocityDifference.z).normalized;
            
            // Check for a wall in front
            if (Physics.Raycast(stepCheckStart, stepCheckDir, stepCheckDistance, groundLayer))
            {
                // If there's a wall, check if we can step up it
                Vector3 stepUpStart = stepCheckStart + stepCheckDir * stepCheckDistance;
                stepUpStart.y += stepHeight;

                // If there's NO wall at step-height, it's a step!
                if (!Physics.Raycast(stepUpStart, Vector3.down, stepHeight, groundLayer))
                {
                    // Apply an upward boost to climb the step
                    _rb.position += new Vector3(0, stepHeight, 0);
                }
            }
        }

        private void HandleSliding()
        {
            float slopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
            _isSliding = slopeAngle > maxSlopeAngle;

            if (_isSliding && _isGrounded)
            {
                // Calculate slide direction
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundNormal);
                _rb.AddForce(slideDirection * slideForce, ForceMode.Acceleration);
            }
        }
        
        private void HandleJump()
        {
            if (_jumpQueued && _isGrounded && !_isSliding)
            {
                // Reset Y velocity to ensure consistent jump height, even on slopes/platforms
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                // Add platform's vertical momentum to the jump
                _rb.AddForce(_platformVelocity, ForceMode.VelocityChange);
            }
            _jumpQueued = false; // Consume the jump input
        }
        
        private void HandleGravity()
        {
            // Apply stronger gravity when falling for a "tighter" feel
            if (!_isGrounded)
            {
                _rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
            }
        }

        private void FixedUpdate()
        {
            GroundCheck();
            HandleSliding();
            HandleMovement();
            HandleJump();
            // HandleGravity();
        }
        
        // For Debugging
        private void OnDrawGizmos()
        {
            if (_collider == null) return;
            
            // Ground Check Sphere
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            float sphereRadius = _collider.radius * 0.9f;
            float checkDist = (_collider.height / 2f) - sphereRadius + groundCheckDistance;
            Vector3 spherePos = transform.position + Vector3.down * checkDist;
            Gizmos.DrawWireSphere(spherePos, sphereRadius);
        }
    }
}