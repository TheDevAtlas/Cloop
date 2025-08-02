using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject itemPrefab;
    public float spawnInterval = 2f;
    public Transform spawnPoint;
    
    [Header("Connection")]
    public Belt connectedBelt;
    
    private BeltManager _beltManager;
    public BeltItem _currentItem;
    public bool _isMovingItem = false;
    public bool _productionStopped = false;
    
    private void Start()
    {
        _beltManager = FindObjectOfType<BeltManager>();
        
        // Find the belt we connect to
        connectedBelt = FindNextBelt();
        
        // Start spawning items
        StartCoroutine(SpawnItems());
    }
    
    private void Update()
    {
        // Keep looking for a belt if we don't have one
        if (connectedBelt == null)
            connectedBelt = FindNextBelt();
        
        // Try to move current item if we have one
        if (_currentItem != null && !_isMovingItem)
            StartCoroutine(MoveItemToBelt());
    }
    
    private IEnumerator SpawnItems()
    {
        while (true)
        {
            // Wait for spawn interval
            yield return new WaitForSeconds(spawnInterval);
            
            // Only spawn if we don't have a current item (production stops if we can't move)
            if (_currentItem == null && !_productionStopped)
            {
                SpawnItem();
            }
        }
    }
    
    private void SpawnItem()
    {
        if (itemPrefab == null)
            return;
            
        // Create the item at consistent height
        GameObject newItem = Instantiate(itemPrefab, GetSpawnPosition(), Quaternion.identity);

        // ADD TO OBJECTIVES
        ObjectivesManager.Instance.Produce(ProductType.Egg);

        // Create BeltItem wrapper
        _currentItem = newItem.GetComponent<BeltItem>();
        
        Debug.Log($"Spawner: Item spawned at {gameObject.name}");
    }
    
    /// <summary>
    /// Gets the spawn position using BeltManager for consistent height
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
        {
            // Use BeltManager to get consistent height for spawn point
            if (_beltManager != null)
                return _beltManager.GetItemPosition(spawnPoint);
            else
                return spawnPoint.position;
        }
            
        // Fallback to spawner position with BeltManager height
        if (_beltManager != null)
            return _beltManager.GetItemPosition(transform);
        
        // Final fallback
        var padding = 0.3f;
        var position = transform.position;
        return new Vector3(position.x, position.y + padding, position.z);
    }
    
    private IEnumerator MoveItemToBelt()
    {
        if (_currentItem?.item == null || connectedBelt == null)
        {
            Debug.Log($"Spawner: Cannot move item - currentItem: {_currentItem?.item}, connectedBelt: {connectedBelt}");
            yield break;
        }
            
        // Check if the connected belt can accept our item
        if (connectedBelt.isSpaceTaken)
        {
            _productionStopped = true;
            Debug.Log($"Spawner: Production stopped - belt is occupied");
            yield break;
        }
        
        _isMovingItem = true;
        _productionStopped = false;
        
        // Reserve space on the belt
        connectedBelt.isSpaceTaken = true;
        
        Vector3 startPosition = _currentItem.item.transform.position;
        Vector3 targetPosition = connectedBelt.GetItemPosition();
        
        Debug.Log($"Spawner: Moving item from {startPosition} to {targetPosition}");
        
        // Move the item to the belt
        while (Vector3.Distance(_currentItem.item.transform.position, targetPosition) > 0.01f)
        {
            var step = _beltManager.speed * Time.deltaTime;
            _currentItem.item.transform.position = 
                Vector3.MoveTowards(_currentItem.item.transform.position, targetPosition, step);
                
            yield return null;
        }
        
        // Ensure final position is exact
        _currentItem.item.transform.position = targetPosition;
        
        // Transfer the item to the belt
        connectedBelt.beltItem = _currentItem;
        _currentItem = null;
        _isMovingItem = false;
        
        Debug.Log($"Spawner: Item transferred to belt");
    }
    
    private Belt FindNextBelt()
    {
        Transform currentTransform = transform;
        RaycastHit hit;
        
        var forward = transform.forward;
        Ray ray = new Ray(currentTransform.position, forward);
        
        if (Physics.Raycast(ray, out hit, 1f))
        {
            Belt belt = hit.collider.GetComponent<Belt>();
            
            if (belt != null)
            {
                Debug.Log($"Spawner: Connected to {belt.gameObject.name}");
                return belt;
            }
        }
        
        return null;
    }
    
    // Public methods for external control
    public bool IsProductionStopped()
    {
        return _productionStopped;
    }
    
    public void ResumeProduction()
    {
        _productionStopped = false;
    }
    
    public void StopProduction()
    {
        _productionStopped = true;
    }
}