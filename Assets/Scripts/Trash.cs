using System.Collections;
using UnityEngine;

public class Trash : Belt
{
    [Header("Trash Settings")]
    public float destructionDelay = 0.5f; // Time before throwing item
    public float throwForce = 8f;         // Force applied to thrown items
    
    private bool _isDestroying = false;
    
    public new void Start()
    {
        // Initialize belt manager but don't look for next belt
        _beltManager = FindObjectOfType<BeltManager>();
        beltInSequence = null; // Always null for trash
        gameObject.name = $"Trash: {gameObject.GetInstanceID()}";
    }
    
    public new void Update()
    {
        // Don't look for next belt - trash is always the end
        // beltInSequence should always remain null
        
        if (beltItem != null && beltItem.item != null && !_isDestroying)
            StartCoroutine(StartTrashProcess());
    }
    
    public IEnumerator StartTrashProcess()
    {
        if (beltItem?.item == null)
            yield break;
            
        _isDestroying = true;
        isSpaceTaken = true;
        
        Debug.Log($"Trash: Processing item {beltItem.item.name}");
        
        // Move item to center (midpoint) of trash
        Vector3 trashCenter = GetTrashCenter();
        yield return StartCoroutine(MoveItemToTrashCenter(beltItem.item.transform, trashCenter));
        
        // Optional: Add destruction effects here
        yield return StartCoroutine(ThrowAwayItem());
        
        _isDestroying = false;
        isSpaceTaken = false;
    }
    
    public IEnumerator MoveItemToTrashCenter(Transform itemTransform, Vector3 targetPosition)
    {
        if (itemTransform == null)
            yield break;
            
        while (Vector3.Distance(itemTransform.position, targetPosition) > 0.01f)
        {
            var step = _beltManager.speed * Time.deltaTime;
            itemTransform.position = Vector3.MoveTowards(itemTransform.position, targetPosition, step);
            yield return null;
        }
        
        itemTransform.position = targetPosition;
    }
    
    public IEnumerator ThrowAwayItem()
    {
        if (beltItem?.item == null)
            yield break;
            
        GameObject itemToThrow = beltItem.item;
        
        Debug.Log($"Trash: Throwing away item {itemToThrow.name}");
        
        // Optional: Add visual/audio effects here before throwing
        // You could add particles, sound effects, scaling animation, etc.
        
        // Wait for destruction delay (for effects)
        yield return new WaitForSeconds(destructionDelay);
        
        // Remove BeltItem component
        BeltItem beltItemComponent = beltItem;
        if (beltItemComponent != null)
        {
            Destroy(beltItemComponent);
        }
        
        // Add Rigidbody for physics
        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = itemToThrow.AddComponent<Rigidbody>();
        }
        
        // Apply throwing force (upward and in a random direction for variety)
        Vector3 randomDirection = new Vector3(
            Random.Range(-0.5f, 0.5f),  // Random X direction
            1f,                         // Always upward
            Random.Range(-0.5f, 0.5f)   // Random Z direction
        ).normalized;
        
        rb.AddForce(randomDirection * throwForce, ForceMode.Impulse);
        
        // Start the destroy timer through BeltManager
        if (_beltManager != null)
        {
            _beltManager.StartDestroyTimer(itemToThrow);
        }
        
        // Clear our belt item reference
        beltItem = null;
        
        Debug.Log($"Trash: Item {itemToThrow.name} thrown away and destroy timer started");
        
        yield return null;
    }
    
    /// <summary>
    /// Gets the trash center position using BeltManager for consistent height
    /// </summary>
    public Vector3 GetTrashCenter()
    {
        // Use BeltManager for consistent height
        if (_beltManager != null)
            return _beltManager.GetItemPosition(transform);
        
        // Fallback if BeltManager not found
        var padding = 0.3f;
        var position = transform.position;
        return new Vector3(position.x, position.y + padding, position.z);
    }
    
    // Override FindNextBelt to always return null - trash doesn't connect to anything
    public new Belt FindNextBelt()
    {
        return null; // Trash never connects to another belt
    }
    
    // Override StartBeltMove to prevent items from moving to next belt
    public new IEnumerator StartBeltMove()
    {
        // Trash doesn't move items to next belt, it throws them away
        yield break;
    }
    
    // Public methods for monitoring trash state
    public bool IsDestroying()
    {
        return _isDestroying;
    }
    
    // Optional: Method to set destruction delay at runtime
    public void SetDestructionDelay(float delay)
    {
        destructionDelay = delay;
    }
    
    // Optional: Method to set throw force at runtime
    public void SetThrowForce(float force)
    {
        throwForce = force;
    }
}