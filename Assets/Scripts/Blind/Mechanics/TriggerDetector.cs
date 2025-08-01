using UnityEngine;
using UnityEngine.XR;

public class TriggerDetector : MonoBehaviour
{
    [SerializeField] private XRNode handNode = XRNode.RightHand;
    private InputDevice device;
    
    public bool IsTriggerPressed()
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(handNode);
        }
        
        device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed);
        return triggerPressed;
    }
    
    public float GetTriggerValue()
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(handNode);
        }
        
        device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        return triggerValue;
    }
}