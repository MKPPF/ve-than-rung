using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TopDownGame.Items
{
    [CreateAssetMenu(menuName = "Top Down Game/Item Data", fileName = "ItemData")]
    [MovedFrom(true, "TopDownGame.Items", "Assembly-CSharp", "ItemData")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
