using UnityEngine;
using System.Collections.Generic;

public class BuildController : MonoBehaviour
{
    [SerializeField] private GameObject conveyorPrefab;
    
    [Header("Build Mode Colors")]
    [SerializeField] private Color ghostPlaceColor = Color.blue;
    [SerializeField] private Color deleteHighlightColor = Color.red;
    [SerializeField] [Range(0f, 1f)] private float ghostTransparency = 0.5f;
    
    private bool buildMode = false;
    private GameObject ghostConveyor;
    private Camera playerCamera;
    private float rotationY = 0f;
    private Vector3 lastPlacedPosition;
    private bool isDragging = false;
    private bool isDeletingLine = false;
    private List<GameObject> placedConveyors = new List<GameObject>();
    private GameObject currentHoveredConveyor;
    private Dictionary<GameObject, Color[]> originalColors = new Dictionary<GameObject, Color[]>();
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
        
        if (buildMode)
        {
            HandleBuildMode();
        }
    }
    
    void ToggleBuildMode()
    {
        buildMode = !buildMode;
        
        if (buildMode)
        {
            CreateGhostConveyor();
        }
        else
        {
            DestroyGhostConveyor();
            RestoreHoveredConveyorColor();
            isDragging = false;
            isDeletingLine = false;
        }
    }
    
    void HandleBuildMode()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePosition);
        
        RaycastHit[] hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        
        GameObject hoveredBelt = null;
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
            GameObject hitConveyor = GetConveyorFromHit(hit.collider.gameObject);
            if (hitConveyor != null)
            {
                hoveredBelt = hitConveyor;
                break;
            }
            
            if (!foundGround)
            {
                snapPosition = SnapToGrid(hit.point);
                foundGround = true;
            }
        }
        
        if (hoveredBelt != null)
        {
            HandleExistingConveyor(hoveredBelt);
        }
        else if (foundGround)
        {
            HandleEmptySpace(snapPosition);
        }
        
        HandleRotation();
        HandleDragging();
    }
    
    void HandleExistingConveyor(GameObject conveyor)
    {
        if (ghostConveyor != null)
        {
            ghostConveyor.SetActive(false);
        }
        
        SetHoveredConveyor(conveyor);
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartDeletingLine(conveyor);
            }
            else
            {
                placedConveyors.Remove(conveyor);
                RestoreConveyorColor(conveyor);
                Destroy(conveyor);
                currentHoveredConveyor = null;
            }
        }
    }
    
    void HandleEmptySpace(Vector3 position)
    {
        RestoreHoveredConveyorColor();
        
        if (ghostConveyor != null)
        {
            ghostConveyor.SetActive(true);
            ghostConveyor.transform.position = position;
            ghostConveyor.transform.rotation = Quaternion.Euler(0, rotationY, 0);
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
                PlaceConveyor(position);
            }
        }
    }
    
    void SetHoveredConveyor(GameObject conveyor)
    {
        if (currentHoveredConveyor == conveyor)
            return;
        
        RestoreHoveredConveyorColor();
        
        currentHoveredConveyor = conveyor;
        
        StoreOriginalColors(conveyor);
        SetConveyorColor(conveyor, deleteHighlightColor);
    }
    
    void RestoreHoveredConveyorColor()
    {
        if (currentHoveredConveyor != null)
        {
            RestoreConveyorColor(currentHoveredConveyor);
            currentHoveredConveyor = null;
        }
    }
    
    void StoreOriginalColors(GameObject conveyor)
    {
        if (originalColors.ContainsKey(conveyor))
            return;
        
        Renderer[] renderers = conveyor.GetComponentsInChildren<Renderer>();
        List<Color> colors = new List<Color>();
        
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                colors.Add(material.color);
            }
        }
        
        originalColors[conveyor] = colors.ToArray();
    }
    
    void RestoreConveyorColor(GameObject conveyor)
    {
        if (!originalColors.ContainsKey(conveyor))
            return;
        
        Renderer[] renderers = conveyor.GetComponentsInChildren<Renderer>();
        Color[] storedColors = originalColors[conveyor];
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
        
        originalColors.Remove(conveyor);
    }
    
    void SetConveyorColor(GameObject conveyor, Color color)
    {
        Renderer[] renderers = conveyor.GetComponentsInChildren<Renderer>();
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
            GameObject hoveredConveyor = null;
            
            foreach (RaycastHit hit in hits)
            {
                GameObject hitConveyor = GetConveyorFromHit(hit.collider.gameObject);
                if (hitConveyor != null)
                {
                    hoveredConveyor = hitConveyor;
                    break;
                }
            }
            
            if (hoveredConveyor != null)
            {
                Vector3 currentPosition = hoveredConveyor.transform.position;
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
        PlaceConveyor(startPosition);
    }
    
    void StartDeletingLine(GameObject startConveyor)
    {
        isDeletingLine = true;
        lastPlacedPosition = startConveyor.transform.position;
        placedConveyors.Remove(startConveyor);
        RestoreConveyorColor(startConveyor);
        Destroy(startConveyor);
        currentHoveredConveyor = null;
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
            if (GetConveyorAtPosition(currentPos) == null)
            {
                PlaceConveyor(currentPos);
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
            GameObject conveyorToDelete = GetConveyorAtPosition(currentPos);
            if (conveyorToDelete != null)
            {
                placedConveyors.Remove(conveyorToDelete);
                RestoreConveyorColor(conveyorToDelete);
                Destroy(conveyorToDelete);
            }
            currentPos += direction;
        }
    }
    
    GameObject GetConveyorAtPosition(Vector3 position)
    {
        foreach (GameObject conveyor in placedConveyors)
        {
            if (conveyor != null && Vector3.Distance(conveyor.transform.position, position) < 0.1f)
            {
                return conveyor;
            }
        }
        return null;
    }
    
    void PlaceConveyor(Vector3 position)
    {
        GameObject newConveyor = Instantiate(conveyorPrefab, position, Quaternion.Euler(0, rotationY, 0));
        placedConveyors.Add(newConveyor);
    }
    
    GameObject GetConveyorFromHit(GameObject hitObject)
    {
        foreach (GameObject conveyor in placedConveyors)
        {
            if (conveyor == null) continue;
            
            if (hitObject == conveyor || hitObject.transform.IsChildOf(conveyor.transform))
            {
                return conveyor;
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
    
    void CreateGhostConveyor()
    {
        if (conveyorPrefab != null)
        {
            ghostConveyor = Instantiate(conveyorPrefab);
            RemoveGhostComponents();
            SetGhostColor(ghostPlaceColor);
        }
    }
    
    void RemoveGhostComponents()
    {
        if (ghostConveyor != null)
        {
            Collider[] colliders = ghostConveyor.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }
            
            MonoBehaviour[] scripts = ghostConveyor.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script.GetType() != typeof(Transform))
                {
                    Destroy(script);
                }
            }
        }
    }
    
    void DestroyGhostConveyor()
    {
        if (ghostConveyor != null)
        {
            Destroy(ghostConveyor);
            ghostConveyor = null;
        }
    }
    
    void SetGhostColor(Color color)
    {
        if (ghostConveyor != null)
        {
            Renderer[] renderers = ghostConveyor.GetComponentsInChildren<Renderer>();
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