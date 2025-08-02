using System.Collections;
using UnityEngine;

public class Converter : Belt
{
    [Header("Converter Settings")]
    public GameObject inputItemPrefab;      // What item type we accept
    public GameObject outputItemPrefab;     // What item type we produce
    public float conversionTime = 1f;       // Time to complete conversion
    public float ejectForce = 5f;          // Force applied to ejected items

    public ProductType Product;
    
    private bool _isConverting = false;
    private float _conversionProgress = 0f;
    
    public void Start()
    {
        // Call parent Start to initialize belt functionality
        base.Start();
    }
    
    public void Update()
    {
        // Find next belt if we don't have one
        if (beltInSequence == null)
            beltInSequence = FindNextBelt();
            
        // Handle conversion process
        if (beltItem != null && beltItem.item != null && !_isConverting)
        {
            StartCoroutine(StartConversion());
        }
    }
    
    public IEnumerator StartConversion()
    {
        if (beltItem?.item == null)
            yield break;
            
        _isConverting = true;
        isSpaceTaken = true;
        
        // Check if the input item matches what we can convert
        if (!CanConvertItem(beltItem.item))
        {
            Debug.Log($"Converter: Cannot convert {beltItem.item.name}, ejecting item");
            yield return StartCoroutine(EjectItem());
            yield break;
        }
        
        Debug.Log($"Converter: Starting conversion of {beltItem.item.name}");
        
        // Move item to center of converter for conversion
        Vector3 conversionPosition = GetConversionPosition();
        yield return StartCoroutine(MoveItemToPosition(beltItem.item.transform, conversionPosition));
        
        // Perform conversion
        yield return StartCoroutine(PerformConversion());
        
        // Try to move converted item to next belt
        if (beltItem != null && beltItem.item != null)
        {
            yield return StartCoroutine(StartBeltMove());
        }
        
        _isConverting = false;
        _conversionProgress = 0f;
    }
    
    public IEnumerator PerformConversion()
    {
        if (beltItem?.item == null || outputItemPrefab == null)
            yield break;
            
        GameObject oldItem = beltItem.item;
        Vector3 itemPosition = oldItem.transform.position;
        
        // Animate conversion progress
        float startTime = Time.time;
        while (Time.time - startTime < conversionTime)
        {
            _conversionProgress = (Time.time - startTime) / conversionTime;
            
            // Optional: Add visual effects here based on _conversionProgress
            // You could scale the item, change its color, add particles, etc.
            
            yield return null;
        }
        
        // Destroy old item
        Destroy(oldItem);
        
        // Create new item at the same position (already at correct height)
        GameObject newItem = Instantiate(outputItemPrefab, itemPosition, Quaternion.identity);

        ObjectivesManager.Instance.Produce(Product);
        
        // Update BeltItem reference
        BeltItem newBeltItem = newItem.GetComponent<BeltItem>();
        if (newBeltItem == null)
        {
            newBeltItem = newItem.AddComponent<BeltItem>();
        }
        
        newBeltItem.item = newItem;
        beltItem = newBeltItem;
        
        Debug.Log($"Converter: Conversion complete, created {newItem.name}");
    }
    
    public IEnumerator EjectItem()
    {
        if (beltItem?.item == null)
            yield break;
            
        GameObject itemToEject = beltItem.item;
        
        // Remove BeltItem component
        BeltItem beltItemComponent = beltItem;
        if (beltItemComponent != null)
        {
            Destroy(beltItemComponent);
        }
        
        // Add Rigidbody for physics
        Rigidbody rb = itemToEject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = itemToEject.AddComponent<Rigidbody>();
        }
        
        // Apply ejection force (upward and slightly forward)
        Vector3 ejectDirection = (Vector3.up + transform.right * 0.5f).normalized;
        rb.AddForce(ejectDirection * ejectForce, ForceMode.Impulse);
        
        // Start the destroy timer through BeltManager
        if (_beltManager != null)
        {
            _beltManager.StartDestroyTimer(itemToEject);
        }
        
        // Clear our belt item reference
        beltItem = null;
        isSpaceTaken = false;
        _isConverting = false;
        
        Debug.Log($"Converter: Item {itemToEject.name} ejected and destroy timer started");
        
        yield return null;
    }
    
    public bool CanConvertItem(GameObject item)
    {
        if (inputItemPrefab == null)
            return false;
            
        // Check if the item name matches the input prefab name
        // You can modify this logic based on your item identification system
        string inputName = inputItemPrefab.name;
        string itemName = item.name.Replace("(Clone)", "").Trim();
        
        return itemName == inputName;
    }
    
    /// <summary>
    /// Gets the conversion position using BeltManager for consistent height
    /// </summary>
    public Vector3 GetConversionPosition()
    {
        // Use BeltManager for consistent height
        if (_beltManager != null)
            return _beltManager.GetItemPosition(transform);
        
        // Fallback if BeltManager not found
        var padding = 0.3f;
        var position = transform.position;
        return new Vector3(position.x, position.y + padding, position.z);
    }
    
    public IEnumerator MoveItemToPosition(Transform itemTransform, Vector3 targetPosition)
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
    
    // Override the StartBeltMove to use our custom logic
    public new IEnumerator StartBeltMove()
    {
        if (beltItem?.item == null || beltInSequence == null || beltInSequence.isSpaceTaken)
            yield break;
            
        Vector3 toPosition = beltInSequence.GetItemPosition();
        beltInSequence.isSpaceTaken = true;
        
        var step = _beltManager.speed * Time.deltaTime;
        
        while (Vector3.Distance(beltItem.item.transform.position, toPosition) > 0.01f)
        {
            beltItem.item.transform.position = 
                Vector3.MoveTowards(beltItem.item.transform.position, toPosition, step);
            yield return null;
        }
        
        beltItem.item.transform.position = toPosition;
        
        // Transfer item to next belt
        beltInSequence.beltItem = beltItem;
        beltItem = null;
        isSpaceTaken = false;
    }
    
    // Public methods for monitoring conversion state
    public bool IsConverting()
    {
        return _isConverting;
    }
    
    public float GetConversionProgress()
    {
        return _conversionProgress;
    }
    
    // Optional: Method to change conversion settings at runtime
    public void SetConversionRecipe(GameObject input, GameObject output, float time = 1f)
    {
        inputItemPrefab = input;
        outputItemPrefab = output;
        conversionTime = time;
    }
}