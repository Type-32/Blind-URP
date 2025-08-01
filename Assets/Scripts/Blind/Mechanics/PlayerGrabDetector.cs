using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlayerGrabDetector : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private NearFarInteractor leftHand;
    [SerializeField] private NearFarInteractor rightHand;
    
    private XRGrabInteractable currentlyGrabbedObject;
    
    void Start()
    {
        // Subscribe to grab events on both hands
        if (leftHand != null)
        {
            leftHand.selectEntered.AddListener(OnLeftHandGrab);
            leftHand.selectExited.AddListener(OnLeftHandRelease);
        }
        
        if (rightHand != null)
        {
            rightHand.selectEntered.AddListener(OnRightHandGrab);
            rightHand.selectExited.AddListener(OnRightHandRelease);
        }
    }
    
    private void OnLeftHandGrab(SelectEnterEventArgs args)
    {
        currentlyGrabbedObject = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
        CheckIfTeleportBall(currentlyGrabbedObject.gameObject);
    }
    
    private void OnRightHandGrab(SelectEnterEventArgs args)
    {
        currentlyGrabbedObject = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
        CheckIfTeleportBall(currentlyGrabbedObject.gameObject);
    }
    
    private void OnLeftHandRelease(SelectExitEventArgs args)
    {
        currentlyGrabbedObject = null;
    }
    
    private void OnRightHandRelease(SelectExitEventArgs args)
    {
        currentlyGrabbedObject = null;
    }
    
    private void CheckIfTeleportBall(GameObject grabbedObject)
    {
        if (grabbedObject.CompareTag("TeleportBall"))
        {
            Debug.Log("Teleport ball grabbed!");
            // Notify your teleportation system
            var teleportSystem = grabbedObject.GetComponent<TeleportationBall>();
            if (teleportSystem != null)
            {
                // teleportSystem.T(grabbedObject);
            }
        }
    }
}