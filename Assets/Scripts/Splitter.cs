using System.Collections;
using UnityEngine;

public class Splitter : Belt
{
    [Header("Splitter Settings")]
    public Belt primaryBelt;      // Forward direction belt
    public Belt secondaryBelt;    // Backward direction belt
    
    private bool _sendToPrimary = true;  // Alternates between true/false
    private bool _isProcessing = false;
    
    public new void Start()
    {
        // Initialize belt manager
        _beltManager = FindObjectOfType<BeltManager>();
        
        // Find both output belts
        primaryBelt = FindNextBelt();      // Forward direction
        secondaryBelt = FindAlternateBelt(); // Side/opposite direction
        
        gameObject.name = $"Splitter: {gameObject.GetInstanceID()}";
        
        Debug.Log($"Splitter initialized - Primary: {primaryBelt?.name}, Secondary: {secondaryBelt?.name}");
    }
    
    public new void Update()
    {
        // Keep looking for belts if we don't have them
        if (primaryBelt == null)
            primaryBelt = FindNextBelt();
            
        if (secondaryBelt == null)
            secondaryBelt = FindAlternateBelt();
        
        // Process items that arrive
        if (beltItem != null && beltItem.item != null && !_isProcessing)
        {
            StartCoroutine(ProcessSplitterItem());
        }
    }
    
    public IEnumerator ProcessSplitterItem()
    {
        if (beltItem?.item == null)
            yield break;
            
        _isProcessing = true;
        isSpaceTaken = true;
        
        // Move item to center of splitter first
        Vector3 splitterCenter = GetSplitterCenter();
        yield return StartCoroutine(MoveItemToCenter(beltItem.item.transform, splitterCenter));
        
        // Determine which belt to send to
        Belt targetBelt = GetTargetBelt();
        
        if (targetBelt != null && !targetBelt.isSpaceTaken)
        {
            // Send to chosen belt
            yield return StartCoroutine(SendItemToBelt(targetBelt));
            
            // Toggle for next item
            _sendToPrimary = !_sendToPrimary;
            
            Debug.Log($"Splitter: Item sent to {targetBelt.name}, next will go to {(_sendToPrimary ? "primary" : "secondary")}");
        }
        else
        {
            // If chosen belt is occupied, try the other belt
            Belt alternateBelt = _sendToPrimary ? secondaryBelt : primaryBelt;
            
            if (alternateBelt != null && !alternateBelt.isSpaceTaken)
            {
                yield return StartCoroutine(SendItemToBelt(alternateBelt));
                Debug.Log($"Splitter: Primary choice blocked, sent to alternate {alternateBelt.name}");
            }
            else
            {
                // Both belts occupied, wait and don't toggle the alternation
                Debug.Log("Splitter: Both output belts occupied, waiting...");
                _isProcessing = false;
                yield break;
            }
        }
        
        _isProcessing = false;
    }
    
    public IEnumerator MoveItemToCenter(Transform itemTransform, Vector3 targetPosition)
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
    
    public IEnumerator SendItemToBelt(Belt targetBelt)
    {
        if (beltItem?.item == null || targetBelt == null)
            yield break;
            
        // Reserve space on target belt
        targetBelt.isSpaceTaken = true;
        
        Vector3 targetPosition = targetBelt.GetItemPosition();
        
        // Move item to target belt
        while (Vector3.Distance(beltItem.item.transform.position, targetPosition) > 0.01f)
        {
            var step = _beltManager.speed * Time.deltaTime;
            beltItem.item.transform.position = 
                Vector3.MoveTowards(beltItem.item.transform.position, targetPosition, step);
            yield return null;
        }
        
        // Ensure final position is exact
        beltItem.item.transform.position = targetPosition;
        
        // Transfer item to target belt
        targetBelt.beltItem = beltItem;
        beltItem = null;
        isSpaceTaken = false;
    }
    
    /// <summary>
    /// Gets the belt to send the next item to based on alternation
    /// </summary>
    private Belt GetTargetBelt()
    {
        return _sendToPrimary ? primaryBelt : secondaryBelt;
    }
    
    /// <summary>
    /// Gets the splitter center position using BeltManager for consistent height
    /// </summary>
    public Vector3 GetSplitterCenter()
    {
        // Use BeltManager for consistent height
        if (_beltManager != null)
            return _beltManager.GetItemPosition(transform);
        
        // Fallback if BeltManager not found
        var padding = 0.3f;
        var position = transform.position;
        return new Vector3(position.x, position.y + padding, position.z);
    }
    
    /// <summary>
    /// Finds the secondary belt in backward direction
    /// </summary>
    public Belt FindAlternateBelt()
    {
        Transform currentTransform = transform;
        RaycastHit hit;
        
        // Try backward direction for secondary belt
        Vector3 backwardDirection = -transform.forward;
        Ray ray = new Ray(currentTransform.position, backwardDirection);
        
        if (Physics.Raycast(ray, out hit, 1f))
        {
            Belt belt = hit.collider.GetComponent<Belt>();
            if (belt != null && belt != this)
            {
                Debug.Log($"Splitter: Found secondary belt {belt.gameObject.name} backward");
                return belt;
            }
        }
        
        return null;
    }
    
    // Override FindNextBelt to be explicit about primary direction
    public new Belt FindNextBelt()
    {
        Transform currentTransform = transform;
        RaycastHit hit;
        
        var forward = transform.forward;
        Ray ray = new Ray(currentTransform.position, forward);
        
        if (Physics.Raycast(ray, out hit, 1f))
        {
            Belt belt = hit.collider.GetComponent<Belt>();
            
            if (belt != null && belt != this)
            {
                Debug.Log($"Splitter: Found primary belt {belt.gameObject.name} forward");
                return belt;
            }
        }
        
        return null;
    }
    
    // Override StartBeltMove to prevent default belt behavior
    public new IEnumerator StartBeltMove()
    {
        // Splitter handles its own item movement logic
        yield break;
    }
    
    // Public methods for monitoring splitter state
    public bool IsProcessing()
    {
        return _isProcessing;
    }
    
    public bool WillSendToPrimary()
    {
        return _sendToPrimary;
    }
    
    public Belt GetPrimaryBelt()
    {
        return primaryBelt;
    }
    
    public Belt GetSecondaryBelt()
    {
        return secondaryBelt;
    }
    
    // Optional: Method to manually set which belt gets the next item
    public void SetNextTarget(bool sendToPrimary)
    {
        _sendToPrimary = sendToPrimary;
    }
    
    // Optional: Method to force reconnection to belts
    public void RefreshConnections()
    {
        primaryBelt = FindNextBelt();
        secondaryBelt = FindAlternateBelt();
        Debug.Log($"Splitter connections refreshed - Primary: {primaryBelt?.name}, Secondary: {secondaryBelt?.name}");
    }
}