using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlayerXRInteractionManager : MonoBehaviour
{
    [SerializeField] private XROrigin origin;
    [SerializeField] private XRDirectInteractor leftHand;
    [SerializeField] private XRDirectInteractor rightHand;

    private XRGrabInteractable leftHandGrabbedObject;
    private XRGrabInteractable rightHandGrabbedObject;

    void Start()
    {
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

    void OnLeftHandGrab(SelectEnterEventArgs args)
    {
        leftHandGrabbedObject = args.interactableObject as XRGrabInteractable;

    }

    void OnRightHandGrab(SelectEnterEventArgs args)
    {
        rightHandGrabbedObject = args.interactableObject as XRGrabInteractable;
    }

    void OnLeftHandRelease(SelectExitEventArgs args)
    {
        leftHandGrabbedObject = null;
    }

    void OnRightHandRelease(SelectExitEventArgs args)
    {
        rightHandGrabbedObject = null;
    }

    public void CheckSonarBallInteracted()
    {

    }
}
