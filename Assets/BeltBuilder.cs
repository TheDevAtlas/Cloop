using System.Collections.Generic;
using UnityEngine;

public class BeltBuilder : MonoBehaviour
{
    [Header("Belt Pieces")]
    public GameObject straightPiece;
    public GameObject cornerPiece;
    public GameObject tPiece;
    public GameObject crossPiece;
    
    [Header("Build Settings")]
    public Material ghostMaterial;
    public LayerMask groundLayer = 1;
    
    private bool buildModeActive = false;
    private int currentRotation = 0;
    private GameObject ghostObject;
    private Camera playerCamera;
    
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private List<Vector3> dragPath = new List<Vector3>();
    private List<GameObject> dragGhosts = new List<GameObject>();
    
    private Dictionary<Vector3, BeltData> placedBelts = new Dictionary<Vector3, BeltData>();
    
    [System.Serializable]
    public class BeltData
    {
        public GameObject beltObject;
        public BeltType type;
        public int rotation;
        
        public BeltData(GameObject obj, BeltType beltType, int rot)
        {
            beltObject = obj;
            type = beltType;
            rotation = rot;
        }
    }
    
    public enum BeltType
    {
        Straight,
        Corner,
        T,
        Cross
    }
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }
    
    void Update()
    {
        HandleInput();
        UpdateGhost();
        HandleDragBuilding();
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
        
        if (!buildModeActive) return;
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotatePiece();
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartDragBuilding();
            }
            else
            {
                PlaceSinglePiece();
            }
        }
        
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDragBuilding();
        }
    }
    
    void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;
        
        if (buildModeActive)
        {
            CreateGhost();
        }
        else
        {
            DestroyGhost();
            CancelDragBuilding();
        }
    }
    
    void RotatePiece()
    {
        currentRotation = (currentRotation + 90) % 360;
        if (ghostObject != null)
        {
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        }
    }
    
    void CreateGhost()
    {
        if (straightPiece == null) return;
        
        ghostObject = Instantiate(straightPiece);
        SetupGhostObject(ghostObject);
    }
    
    void SetupGhostObject(GameObject ghost)
    {
        ghost.name = "Ghost";
        
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        
        if (ghostMaterial != null)
        {
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMaterial;
                }
                renderer.materials = materials;
            }
        }
        
        ghost.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
    }
    
    void DestroyGhost()
    {
        if (ghostObject != null)
        {
            DestroyImmediate(ghostObject);
            ghostObject = null;
        }
    }
    
    void UpdateGhost()
    {
        if (!buildModeActive || ghostObject == null) return;
        
        Vector3 mousePos = Input.mousePosition;
        Ray ray = playerCamera.ScreenPointToRay(mousePos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 snapPos = SnapToGrid(hit.point);
            ghostObject.transform.position = snapPos;
            ghostObject.SetActive(true);
        }
        else
        {
            ghostObject.SetActive(false);
        }
    }
    
    Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x),
            position.y,
            Mathf.Round(position.z)
        );
    }
    
    void PlaceSinglePiece()
    {
        if (ghostObject == null || !ghostObject.activeInHierarchy) return;
        
        Vector3 position = ghostObject.transform.position;
        Vector3 gridPos = new Vector3(position.x, 0, position.z);
        
        if (placedBelts.ContainsKey(gridPos))
        {
            bool[] currentConnections = GetConnections(gridPos);
            int connectionCount = 0;
            foreach (bool conn in currentConnections) if (conn) connectionCount++;
            
            if (connectionCount >= 2)
            {
                UpdateSingleBelt(gridPos);
                return;
            }
        }
        
        if (placedBelts.ContainsKey(gridPos))
        {
            DestroyImmediate(placedBelts[gridPos].beltObject);
        }
        
        GameObject newBelt = Instantiate(straightPiece, position, Quaternion.Euler(0, currentRotation, 0));
        placedBelts[gridPos] = new BeltData(newBelt, BeltType.Straight, currentRotation);
        
        UpdateConnections(gridPos);
    }
    
    void StartDragBuilding()
    {
        if (ghostObject == null || !ghostObject.activeInHierarchy) return;
        
        isDragging = true;
        dragStartPos = ghostObject.transform.position;
        dragPath.Clear();
        dragPath.Add(dragStartPos);
        
        ClearDragGhosts();
    }
    
    void HandleDragBuilding()
    {
        if (!isDragging || ghostObject == null) return;
        
        Vector3 currentPos = ghostObject.transform.position;
        Vector3 startGrid = new Vector3(dragStartPos.x, 0, dragStartPos.z);
        Vector3 currentGrid = new Vector3(currentPos.x, 0, currentPos.z);
        
        if (startGrid == currentGrid) return;
        
        List<Vector3> newPath = CalculateDragPath(startGrid, currentGrid);
        
        if (!PathsEqual(dragPath, newPath))
        {
            dragPath = newPath;
            UpdateDragGhosts();
        }
    }
    
    List<Vector3> CalculateDragPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(start);
        
        Vector3 diff = end - start;
        
        if (Mathf.Abs(diff.x) > 0 && Mathf.Abs(diff.z) > 0)
        {
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.z))
            {
                Vector3 corner = new Vector3(end.x, start.y, start.z);
                AddStraightLine(path, start, corner);
                AddStraightLine(path, corner, end);
            }
            else
            {
                Vector3 corner = new Vector3(start.x, start.y, end.z);
                AddStraightLine(path, start, corner);
                AddStraightLine(path, corner, end);
            }
        }
        else
        {
            AddStraightLine(path, start, end);
        }
        
        return path;
    }
    
    void AddStraightLine(List<Vector3> path, Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 current = from;
        
        while (Vector3.Distance(current, to) > 0.1f)
        {
            current += direction;
            current = SnapToGrid(current);
            
            if (!path.Contains(current))
            {
                path.Add(current);
            }
            
            if (Vector3.Distance(current, to) < 0.1f) break;
        }
    }
    
    bool PathsEqual(List<Vector3> path1, List<Vector3> path2)
    {
        if (path1.Count != path2.Count) return false;
        
        for (int i = 0; i < path1.Count; i++)
        {
            if (Vector3.Distance(path1[i], path2[i]) > 0.1f) return false;
        }
        
        return true;
    }
    
    void UpdateDragGhosts()
    {
        ClearDragGhosts();
        
        for (int i = 0; i < dragPath.Count; i++)
        {
            Vector3 pos = dragPath[i];
            Vector3 gridPos = new Vector3(pos.x, 0, pos.z);
            
            int rotation = CalculateRotationForPath(i);
            BeltType beltType = CalculateBeltTypeForPath(i);
            
            GameObject prefab = GetPrefabForType(beltType);
            if (prefab == null) continue;
            
            GameObject ghost = Instantiate(prefab, pos, Quaternion.Euler(0, rotation, 0));
            SetupGhostObject(ghost);
            dragGhosts.Add(ghost);
        }
    }
    
    int CalculateRotationForPath(int index)
    {
        if (dragPath.Count < 2) return currentRotation;
        
        if (index == 0)
        {
            Vector3 dir = (dragPath[1] - dragPath[0]).normalized;
            return GetRotationFromDirection(dir);
        }
        else if (index == dragPath.Count - 1)
        {
            Vector3 dir = (dragPath[index] - dragPath[index - 1]).normalized;
            return GetRotationFromDirection(dir);
        }
        else
        {
            Vector3 prevDir = (dragPath[index] - dragPath[index - 1]).normalized;
            Vector3 nextDir = (dragPath[index + 1] - dragPath[index]).normalized;
            
            if (Vector3.Dot(prevDir, nextDir) < 0.1f)
            {
                return GetCornerRotation(prevDir, nextDir);
            }
            else
            {
                return GetRotationFromDirection(prevDir);
            }
        }
    }
    
    BeltType CalculateBeltTypeForPath(int index)
    {
        if (dragPath.Count < 2) return BeltType.Straight;
        
        if (index == 0 || index == dragPath.Count - 1)
        {
            return BeltType.Straight;
        }
        
        Vector3 prevDir = (dragPath[index] - dragPath[index - 1]).normalized;
        Vector3 nextDir = (dragPath[index + 1] - dragPath[index]).normalized;
        
        if (Vector3.Dot(prevDir, nextDir) < 0.1f)
        {
            return BeltType.Corner;
        }
        
        return BeltType.Straight;
    }
    
    GameObject GetPrefabForType(BeltType type)
    {
        switch (type)
        {
            case BeltType.Straight: return straightPiece;
            case BeltType.Corner: return cornerPiece;
            case BeltType.T: return tPiece;
            case BeltType.Cross: return crossPiece;
            default: return straightPiece;
        }
    }
    
    int GetRotationFromDirection(Vector3 direction)
    {
        if (Vector3.Dot(direction, Vector3.forward) > 0.7f) return 0;
        if (Vector3.Dot(direction, Vector3.right) > 0.7f) return 90;
        if (Vector3.Dot(direction, Vector3.back) > 0.7f) return 180;
        if (Vector3.Dot(direction, Vector3.left) > 0.7f) return 270;
        return 0;
    }
    
    int GetCornerRotation(Vector3 from, Vector3 to)
    {
        if ((Vector3.Dot(from, Vector3.forward) > 0.7f && Vector3.Dot(to, Vector3.right) > 0.7f) ||
            (Vector3.Dot(from, Vector3.left) > 0.7f && Vector3.Dot(to, Vector3.back) > 0.7f)) return 0;
        
        if ((Vector3.Dot(from, Vector3.right) > 0.7f && Vector3.Dot(to, Vector3.back) > 0.7f) ||
            (Vector3.Dot(from, Vector3.forward) > 0.7f && Vector3.Dot(to, Vector3.left) > 0.7f)) return 90;
        
        if ((Vector3.Dot(from, Vector3.back) > 0.7f && Vector3.Dot(to, Vector3.left) > 0.7f) ||
            (Vector3.Dot(from, Vector3.right) > 0.7f && Vector3.Dot(to, Vector3.forward) > 0.7f)) return 180;
        
        if ((Vector3.Dot(from, Vector3.left) > 0.7f && Vector3.Dot(to, Vector3.forward) > 0.7f) ||
            (Vector3.Dot(from, Vector3.back) > 0.7f && Vector3.Dot(to, Vector3.right) > 0.7f)) return 270;
        
        return 0;
    }
    
    void EndDragBuilding()
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        for (int i = 0; i < dragPath.Count; i++)
        {
            Vector3 pos = dragPath[i];
            Vector3 gridPos = new Vector3(pos.x, 0, pos.z);
            
            int rotation = CalculateRotationForPath(i);
            BeltType beltType = CalculateBeltTypeForPath(i);
            
            GameObject prefab = GetPrefabForType(beltType);
            if (prefab == null) continue;
            
            if (placedBelts.ContainsKey(gridPos))
            {
                DestroyImmediate(placedBelts[gridPos].beltObject);
            }
            
            GameObject newBelt = Instantiate(prefab, pos, Quaternion.Euler(0, rotation, 0));
            placedBelts[gridPos] = new BeltData(newBelt, beltType, rotation);
        }
        
        HashSet<Vector3> positionsToUpdate = new HashSet<Vector3>();
        foreach (Vector3 pos in dragPath)
        {
            Vector3 gridPos = new Vector3(pos.x, 0, pos.z);
            positionsToUpdate.Add(gridPos);
            
            Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            foreach (Vector3 dir in directions)
            {
                Vector3 neighborPos = gridPos + dir;
                if (placedBelts.ContainsKey(neighborPos))
                {
                    positionsToUpdate.Add(neighborPos);
                }
            }
        }
        
        foreach (Vector3 pos in positionsToUpdate)
        {
            UpdateSingleBelt(pos);
        }
        
        ClearDragGhosts();
        dragPath.Clear();
    }
    
    void CancelDragBuilding()
    {
        isDragging = false;
        ClearDragGhosts();
        dragPath.Clear();
    }
    
    void ClearDragGhosts()
    {
        foreach (GameObject ghost in dragGhosts)
        {
            if (ghost != null)
                DestroyImmediate(ghost);
        }
        dragGhosts.Clear();
    }
    
    void UpdateConnections(Vector3 centerPos)
    {
        Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        
        UpdateSingleBelt(centerPos);
        
        foreach (Vector3 dir in directions)
        {
            Vector3 adjacentPos = centerPos + dir;
            if (placedBelts.ContainsKey(adjacentPos))
            {
                UpdateSingleBelt(adjacentPos);
            }
        }
    }
    
    void UpdateSingleBelt(Vector3 position)
    {
        if (!placedBelts.ContainsKey(position)) return;
        
        BeltData beltData = placedBelts[position];
        bool[] connections = GetConnections(position);
        int connectionCount = 0;
        foreach (bool conn in connections) if (conn) connectionCount++;
        
        BeltType newType = BeltType.Straight;
        int newRotation = 0;
        
        switch (connectionCount)
        {
            case 0:
            case 1:
                newType = BeltType.Straight;
                newRotation = GetStraightRotationFromConnections(connections);
                break;
                
            case 2:
                if (IsOppositeConnections(connections))
                {
                    newType = BeltType.Straight;
                    newRotation = GetStraightRotationFromConnections(connections);
                }
                else
                {
                    newType = BeltType.Corner;
                    newRotation = GetCornerRotationFromConnections(connections);
                }
                break;
                
            case 3:
                newType = BeltType.T;
                newRotation = GetTRotationFromConnections(connections);
                break;
                
            case 4:
                newType = BeltType.Cross;
                newRotation = 0;
                break;
        }
        
        if (newType != beltData.type || newRotation != beltData.rotation)
        {
            Vector3 pos = beltData.beltObject.transform.position;
            DestroyImmediate(beltData.beltObject);
            
            GameObject prefab = GetPrefabForType(newType);
            GameObject newBelt = Instantiate(prefab, pos, Quaternion.Euler(0, newRotation, 0));
            
            placedBelts[position] = new BeltData(newBelt, newType, newRotation);
        }
    }
    
    bool[] GetConnections(Vector3 position)
    {
        bool[] connections = new bool[4];
        Vector3[] directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        
        for (int i = 0; i < 4; i++)
        {
            Vector3 checkPos = position + directions[i];
            connections[i] = placedBelts.ContainsKey(checkPos);
        }
        
        return connections;
    }
    
    bool IsOppositeConnections(bool[] connections)
    {
        return (connections[0] && connections[2]) || (connections[1] && connections[3]);
    }
    
    int GetStraightRotationFromConnections(bool[] connections)
    {
        if (connections[0] || connections[2]) return 0;
        if (connections[1] || connections[3]) return 90;
        
        for (int i = 0; i < 4; i++)
        {
            if (connections[i]) return i * 90;
        }
        
        return 0;
    }
    
    int GetCornerRotationFromConnections(bool[] connections)
    {
        if (connections[0] && connections[1]) return 0;
        if (connections[1] && connections[2]) return 90;
        if (connections[2] && connections[3]) return 180;
        if (connections[3] && connections[0]) return 270;
        
        return 0;
    }
    
    int GetTRotationFromConnections(bool[] connections)
    {
        if (connections[1] && connections[2] && connections[3]) return 0;
        if (connections[0] && connections[2] && connections[3]) return 90;
        if (connections[0] && connections[1] && connections[3]) return 180;
        if (connections[0] && connections[1] && connections[2]) return 270;
        
        return 0;
    }
}