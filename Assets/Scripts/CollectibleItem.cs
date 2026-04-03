using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private ObjectInventory.ItemType itemType = ObjectInventory.ItemType.ScrapMetal;
    [SerializeField] private float quantity = 1f;

    public ObjectInventory.ItemType ItemType => itemType;
    public float Quantity => quantity;

    void Start()
    {
        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogWarning($"CollectibleItem '{gameObject.name}' requires a Rigidbody2D component!");
        }
    }
}
