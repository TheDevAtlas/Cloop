using System.Collections;
using UnityEngine;

public class BeltManager : MonoBehaviour
{
    [Header("Belt System Settings")]
    public float speed = 2f;
    public float itemHeight = 0.3f; // Consistent height for all items above belts/spawners
    
    /// <summary>
    /// Gets the consistent item position for any belt or spawner
    /// </summary>
    /// <param name="beltTransform">The transform of the belt or spawner</param>
    /// <returns>Position with consistent Y height</returns>
    public Vector3 GetItemPosition(Transform beltTransform)
    {
        return new Vector3(beltTransform.position.x, beltTransform.position.y + itemHeight, beltTransform.position.z);
    }
    
    /// <summary>
    /// Sets an item to the correct height above a belt/spawner
    /// </summary>
    /// <param name="itemTransform">The item to position</param>
    /// <param name="beltTransform">The belt/spawner transform</param>
    public void SetItemHeight(Transform itemTransform, Transform beltTransform)
    {
        Vector3 correctPosition = GetItemPosition(beltTransform);
        itemTransform.position = correctPosition;
    }
    
    /// <summary>
    /// Starts a destruction sequence for a thrown object.
    /// First waits 1.5 seconds, then shrinks the object to scale 0 over 0.3 seconds, then destroys it.
    /// </summary>
    /// <param name="objectToDestroy">The GameObject to destroy</param>
    public void StartDestroyTimer(GameObject objectToDestroy)
    {
        if (objectToDestroy != null)
        {
            StartCoroutine(DestroySequence(objectToDestroy));
        }
    }
    
    private IEnumerator DestroySequence(GameObject objectToDestroy)
    {
        if (objectToDestroy == null)
            yield break;
            
        // Wait for 1.5 seconds
        yield return new WaitForSeconds(1.5f);
        
        // Check if object still exists (might have been destroyed by something else)
        if (objectToDestroy == null)
            yield break;
            
        // Store the original scale
        Vector3 originalScale = objectToDestroy.transform.localScale;
        Vector3 targetScale = Vector3.zero;
        
        // Shrink over 0.3 seconds
        float shrinkDuration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < shrinkDuration && objectToDestroy != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shrinkDuration;
            
            // Smooth interpolation from original scale to zero
            objectToDestroy.transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            
            yield return null;
        }
        
        // Final check and destroy
        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy);
        }
    }
}