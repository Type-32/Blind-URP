using UnityEngine;

namespace Blind.Foundations.Interaction
{
    [CreateAssetMenu(fileName = "New Interactable Object Data", menuName = "Elpis/Interactable Object Data", order = 0)]
    public class InteractableObjectData : ScriptableObject
    {
        public new string name;
        public string description;
        public Sprite icon;
    }
}