using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildableObject
{
    public string name;
    public GameObject prefab;
    public KeyCode hotkey;
}

public class BuildController : MonoBehaviour
{
    [Header("Buildable Objects")]
    [SerializeField] private List<BuildableObject> buildableObjects = new List<BuildableObject>();
    
    [Header("Build Mode Colors")]
    [SerializeField] private Color ghostPlaceColor = Color.blue;
    [SerializeField] private Color deleteHighlightColor = Color.red;
    [SerializeField] [Range(0f, 1f)] private float ghostTransparency = 0.5f;
    
    private bool buildMode = false;
    private GameObject ghostObject;
    private Camera playerCamera;
    private float rotationY = 0f;
    private Vector3 lastPlacedPosition;
    private bool isDragging = false;
    private bool isDeletingLine = false;
    private List<GameObject> placedObjects = new List<GameObject>();
    private GameObject currentHoveredObject;
    private Dictionary<GameObject, Color[]> originalColors = new Dictionary<GameObject, Color[]>();
    
    // Current selected object type
    private int selectedObjectIndex = 0;
    private BuildableObject CurrentBuildableObject => 
        buildableObjects.Count > 0 && selectedObjectIndex >= 0 && selectedObjectIndex < buildableObjects.Count 
            ? buildableObjects[selectedObjectIndex] 
            : null;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            
        // Initialize with default objects if none are set
        if (buildableObjects.Count == 0)
        {
            Debug.LogWarning("No buildable objects configured! Please set them up in the inspector.");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
        
        HandleObjectSelection();
        
        if (buildMode)
        {
            HandleBuildMode();
        }
    }
    
    void HandleObjectSelection()
    {
        // Check number keys for object selection
        for (int i = 0; i < buildableObjects.Count && i < 10; i++)
        {
            KeyCode numberKey = KeyCode.Alpha1 + i; // Alpha1 = 1, Alpha2 = 2, etc.
            
            if (Input.GetKeyDown(numberKey))
            {
                SelectObject(i);
                break;
            }
        }
        
        // Also check for configured hotkeys
        for (int i = 0; i < buildableObjects.Count; i++)
        {
            if (buildableObjects[i].hotkey != KeyCode.None && Input.GetKeyDown(buildableObjects[i].hotkey))
            {
                SelectObject(i);
                break;
            }
        }
    }
    
    void SelectObject(int index)
    {
        if (index >= 0 && index < buildableObjects.Count)
        {
            selectedObjectIndex = index;
            
            if (buildMode)
            {
                // Recreate ghost object with new type
                DestroyGhostObject();
                CreateGhostObject();
            }
            
            Debug.Log($"Selected: {buildableObjects[selectedObjectIndex].name}");
        }
    }
    
    void ToggleBuildMode()
    {
        buildMode = !buildMode;
        
        if (buildMode)
        {
            CreateGhostObject();
        }
        else
        {
            DestroyGhostObject();
            RestoreHoveredObjectColor();
            isDragging = false;
            isDeletingLine = false;
        }
    }
    
    void HandleBuildMode()
    {
        if (CurrentBuildableObject == null)
        {
            Debug.LogWarning("No buildable object selected!");
            return;
        }
        
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        
        RaycastHit[] hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        
        GameObject hoveredObject = null;
        Vector3 snapPosition = Vector3.zero;
        bool foundGround = false;
        
        if (hits.Length > 0 && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("=== Raycast Hits ===");
            for (int i = 0; i < hits.Length; i++)
            {
                Debug.Log($"Hit {i}: {hits[i].collider.gameObject.name} on layer {LayerMask.LayerToName(hits[i].collider.gameObject.layer)}");
            }
        }
        
        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = GetPlacedObjectFromHit(hit.collider.gameObject);
            if (hitObject != null)
            {
                hoveredObject = hitObject;
                break;
            }
            
            if (!foundGround)
            {
                snapPosition = SnapToGrid(hit.point);
                foundGround = true;
            }
        }
        
        if (hoveredObject != null)
        {
            HandleExistingObject(hoveredObject);
        }
        else if (foundGround)
        {
            HandleEmptySpace(snapPosition);
        }
        
        HandleRotation();
        HandleDragging();
    }
    
    void HandleExistingObject(GameObject obj)
    {
        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
        }
        
        SetHoveredObject(obj);
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartDeletingLine(obj);
            }
            else
            {
                placedObjects.Remove(obj);
                RestoreObjectColor(obj);
                Destroy(obj);
                currentHoveredObject = null;
            }
        }
    }
    
    void HandleEmptySpace(Vector3 position)
    {
        RestoreHoveredObjectColor();
        
        if (ghostObject != null)
        {
            ghostObject.SetActive(true);
            ghostObject.transform.position = position;
            ghostObject.transform.rotation = Quaternion.Euler(0, rotationY, 0);
            SetGhostColor(ghostPlaceColor);
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartDragging(position);
            }
            else
            {
                PlaceObject(position);
            }
        }
    }
    
    void SetHoveredObject(GameObject obj)
    {
        if (currentHoveredObject == obj)
            return;
        
        RestoreHoveredObjectColor();
        
        currentHoveredObject = obj;
        
        StoreOriginalColors(obj);
        SetObjectColor(obj, deleteHighlightColor);
    }
    
    void RestoreHoveredObjectColor()
    {
        if (currentHoveredObject != null)
        {
            RestoreObjectColor(currentHoveredObject);
            currentHoveredObject = null;
        }
    }
    
    void StoreOriginalColors(GameObject obj)
    {
        if (originalColors.ContainsKey(obj))
            return;
        
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        List<Color> colors = new List<Color>();
        
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                colors.Add(material.color);
            }
        }
        
        originalColors[obj] = colors.ToArray();
    }
    
    void RestoreObjectColor(GameObject obj)
    {
        if (!originalColors.ContainsKey(obj))
            return;
        
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        Color[] storedColors = originalColors[obj];
        int colorIndex = 0;
        
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (colorIndex < storedColors.Length)
                {
                    material.color = storedColors[colorIndex];
                    colorIndex++;
                }
            }
        }
        
        originalColors.Remove(obj);
    }
    
    void SetObjectColor(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                material.color = color;
            }
        }
    }
    
    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            rotationY -= 90f;
            if (rotationY < 0) rotationY = 270f;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            rotationY += 90f;
            if (rotationY >= 360f) rotationY = 0f;
        }
    }
    
    void HandleDragging()
    {
        if (isDragging && Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 currentPosition = SnapToGrid(hit.point);
                DrawLine(lastPlacedPosition, currentPosition);
            }
        }
        
        if (isDeletingLine && Input.GetKey(KeyCode.LeftShift))
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);
            
            RaycastHit[] hits = Physics.RaycastAll(ray);
            GameObject hoveredObject = null;
            
            foreach (RaycastHit hit in hits)
            {
                GameObject hitObject = GetPlacedObjectFromHit(hit.collider.gameObject);
                if (hitObject != null)
                {
                    hoveredObject = hitObject;
                    break;
                }
            }
            
            if (hoveredObject != null)
            {
                Vector3 currentPosition = hoveredObject.transform.position;
                DeleteLine(lastPlacedPosition, currentPosition);
            }
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            isDeletingLine = false;
        }
    }
    
    void StartDragging(Vector3 startPosition)
    {
        isDragging = true;
        lastPlacedPosition = startPosition;
        PlaceObject(startPosition);
    }
    
    void StartDeletingLine(GameObject startObject)
    {
        isDeletingLine = true;
        lastPlacedPosition = startObject.transform.position;
        placedObjects.Remove(startObject);
        RestoreObjectColor(startObject);
        Destroy(startObject);
        currentHoveredObject = null;
    }
    
    void DrawLine(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            direction = new Vector3(Mathf.Sign(direction.x), 0, 0);
        }
        else
        {
            direction = new Vector3(0, 0, Mathf.Sign(direction.z));
        }
        
        Vector3 currentPos = start + direction;
        
        while (Vector3.Distance(start, currentPos) <= distance)
        {
            if (GetObjectAtPosition(currentPos) == null)
            {
                PlaceObject(currentPos);
            }
            currentPos += direction;
        }
    }
    
    void DeleteLine(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            direction = new Vector3(Mathf.Sign(direction.x), 0, 0);
        }
        else
        {
            direction = new Vector3(0, 0, Mathf.Sign(direction.z));
        }
        
        Vector3 currentPos = start + direction;
        
        while (Vector3.Distance(start, currentPos) <= distance)
        {
            GameObject objectToDelete = GetObjectAtPosition(currentPos);
            if (objectToDelete != null)
            {
                placedObjects.Remove(objectToDelete);
                RestoreObjectColor(objectToDelete);
                Destroy(objectToDelete);
            }
            currentPos += direction;
        }
    }
    
    GameObject GetObjectAtPosition(Vector3 position)
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null && Vector3.Distance(obj.transform.position, position) < 0.1f)
            {
                return obj;
            }
        }
        return null;
    }
    
    void PlaceObject(Vector3 position)
    {
        if (CurrentBuildableObject == null || CurrentBuildableObject.prefab == null)
        {
            Debug.LogWarning("Cannot place object: no valid prefab selected!");
            return;
        }
        
        GameObject newObject = Instantiate(CurrentBuildableObject.prefab, position, Quaternion.Euler(0, rotationY, 0));
        placedObjects.Add(newObject);
    }
    
    GameObject GetPlacedObjectFromHit(GameObject hitObject)
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj == null) continue;
            
            if (hitObject == obj || hitObject.transform.IsChildOf(obj.transform))
            {
                return obj;
            }
        }
        return null;
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x),
            position.y,
            Mathf.Round(position.z)
        );
    }
    
    void CreateGhostObject()
    {
        if (CurrentBuildableObject != null && CurrentBuildableObject.prefab != null)
        {
            ghostObject = Instantiate(CurrentBuildableObject.prefab);
            RemoveGhostComponents();
            SetGhostColor(ghostPlaceColor);
        }
    }
    
    void RemoveGhostComponents()
    {
        if (ghostObject != null)
        {
            Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }
            
            MonoBehaviour[] scripts = ghostObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script.GetType() != typeof(Transform))
                {
                    Destroy(script);
                }
            }
        }
    }
    
    void DestroyGhostObject()
    {
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }
    }
    
    void SetGhostColor(Color color)
    {
        if (ghostObject != null)
        {
            Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.SetFloat("_Mode", 2);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    
                    material.color = new Color(color.r, color.g, color.b, ghostTransparency);
                }
            }
        }
    }
}