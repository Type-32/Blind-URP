using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportationBall : MonoBehaviour
{
    [Header("Teleportation Settings")]
    [SerializeField] private XRNode teleportButtonHand = XRNode.RightHand;
    [SerializeField] private XRNode recallButtonHand = XRNode.LeftHand;
    [SerializeField] private float teleportDistance = 2f;
    [SerializeField] private LayerMask teleportMask = -1;
    
    [Header("References")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    [SerializeField] private Transform xrOrigin;
    
    private InputDevice teleportDevice;
    private InputDevice recallDevice;
    private bool isGrabbed = false;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private Rigidbody rb;

    private FPActionsMap map;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
        
        // Setup grab events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        
        // Get input devices
        teleportDevice = InputDevices.GetDeviceAtXRNode(teleportButtonHand);
        recallDevice = InputDevices.GetDeviceAtXRNode(recallButtonHand);

        map = new FPActionsMap();

        map.Enable();
        map.Player.Teleport.performed += action =>
        {
            if (isGrabbed)
            {
                Debug.Log("Right Trigger");
                TeleportToBall();
            }
        };
        map.Player.Recall.performed += action =>
        {
            Debug.Log("Left Trigger");
            RecallBall();
        };
    }
    
    void Update()
    {
        if(!teleportDevice.isValid || !recallDevice.isValid)
        {
            teleportDevice = InputDevices.GetDeviceAtXRNode(teleportButtonHand);
            recallDevice = InputDevices.GetDeviceAtXRNode(recallButtonHand);
        }

        if (isGrabbed)
        {
            // Check for teleport button (e.g., trigger on left hand)
            if (teleportDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool teleportPressed))
            {
                if (teleportPressed)
                {
                    Debug.Log("Right Trigger");
                    TeleportToBall();
                }
            }
        }

        // Check for recall button (e.g., grip on right hand) - works even when not grabbed
        if (recallDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool recallPressed))
        {
            if (recallPressed)
            {
                Debug.Log("Left Trigger");
                RecallBall();
            }
        }
        
        // Update last known valid position when not grabbed
        if (!isGrabbed)
        {
            lastValidPosition = transform.position;
            lastValidRotation = transform.rotation;
        }
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        Debug.Log("Teleport ball grabbed");
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        // isGrabbed = false;
        // Update last valid position when released
        lastValidPosition = transform.position;
        lastValidRotation = transform.rotation;
        Debug.Log("Teleport ball released");
    }
    
    private void TeleportToBall()
    {
        if (xrOrigin == null) return;
        xrOrigin.position = transform.position;
        isGrabbed = false;
        return;
        // Calculate position in front of the ball (so player doesn't teleport inside it)
        Vector3 directionFromBall = (xrOrigin.position - transform.position).normalized;
        Vector3 teleportPosition = transform.position + (directionFromBall * teleportDistance);
        
        // Raycast to ensure we're not teleporting into geometry
        if (Physics.Raycast(transform.position, directionFromBall, out RaycastHit hit, teleportDistance, teleportMask))
        {
            // Teleport to hit point instead
            teleportPosition = hit.point;
        }
        
        // Teleport the player
        xrOrigin.position = teleportPosition;
        
        // Move ball in front of player after teleportation
        Vector3 ballPosition = teleportPosition + (xrOrigin.forward * 1f) + (Vector3.up * 0.5f);
        transform.position = ballPosition;
        
        // Make ball face player
        transform.LookAt(xrOrigin.position);
        
        Debug.Log("Teleported to ball position");
    }
    
    private void RecallBall()
    {
        // Move ball in front of player
        Vector3 recallPosition = xrOrigin.position + (xrOrigin.forward * 1f) + (Vector3.up * 0.5f);
        transform.position = recallPosition;
        
        // Reset physics
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("Ball recalled");
        isGrabbed = false;
    }
    
    // Public methods for external systems
    public bool IsBallGrabbed()
    {
        return isGrabbed;
    }
    
    public Vector3 GetLastValidPosition()
    {
        return lastValidPosition;
    }
}