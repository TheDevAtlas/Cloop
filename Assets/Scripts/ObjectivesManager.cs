using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

[System.Serializable]
public class Objective
{
    public string description;
    public ProductType requiredProduct;
    public int targetAmount;
    public int currentAmount;
    public bool isCompleted;
    
    public bool TryIncrement(ProductType productType)
    {
        if (productType == requiredProduct && !isCompleted)
        {
            currentAmount++;
            if (currentAmount >= targetAmount)
            {
                isCompleted = true;
                return true; // Objective completed
            }
            return true; // Progress made
        }
        return false; // No progress
    }
    
    public string GetProgressText()
    {
        return $"{description} ({currentAmount}/{targetAmount})";
    }
}

public enum ProductType
{
    Egg,
    Chicken,
    Nugget
}

public class ObjectivesManager : MonoBehaviour
{
    public static ObjectivesManager Instance { get; private set; }
    
    [Header("Objectives Setup")]
    [SerializeField] private List<Objective> allObjectives = new List<Objective>();
    
    [Header("UI Settings")]
    [SerializeField] private GameObject objectiveUIPrefab;
    [SerializeField] private Transform uiParent;
    [SerializeField] private Vector3 offScreenPosition = new Vector3(-400f, 0f, 0f);
    [SerializeField] private Vector3 onScreenPosition = new Vector3(50f, 0f, 0f);
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Objective Display")]
    [SerializeField] private int maxActiveObjectives = 2;
    
    [Header("Completion Counter")]
    [SerializeField] private TextMeshProUGUI completionCounterText;
    
    // Private variables
    private Queue<Objective> objectiveQueue = new Queue<Objective>();
    private List<Objective> activeObjectives = new List<Objective>();
    private List<ObjectiveUI> activeObjectiveUIs = new List<ObjectiveUI>();
    private int currentObjectiveIndex = 0;
    private int totalObjectivesCount = 0;
    private int completedObjectivesCount = 0;
    
    // Events
    public event Action<Objective> OnObjectiveCompleted;
    public event Action<Objective> OnObjectiveProgress;
    public event Action<Objective> OnNewObjectiveStarted;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeObjectives();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartInitialObjectives();
        UpdateCompletionCounter();
    }
    
    private void InitializeObjectives()
    {
        // Clear any completed objectives and reset progress
        foreach (var objective in allObjectives)
        {
            objective.currentAmount = 0;
            objective.isCompleted = false;
        }
        
        // Set total objectives count
        totalObjectivesCount = allObjectives.Count;
        completedObjectivesCount = 0;
        
        // Add all objectives to queue
        objectiveQueue.Clear();
        foreach (var objective in allObjectives)
        {
            objectiveQueue.Enqueue(objective);
        }
    }
    
    private void StartInitialObjectives()
    {
        // Start with the first objectives up to maxActiveObjectives
        for (int i = 0; i < maxActiveObjectives && objectiveQueue.Count > 0; i++)
        {
            ActivateNextObjective();
        }
    }
    
    private void ActivateNextObjective()
    {
        if (objectiveQueue.Count > 0)
        {
            Objective nextObjective = objectiveQueue.Dequeue();
            activeObjectives.Add(nextObjective);
            CreateObjectiveUI(nextObjective);
            OnNewObjectiveStarted?.Invoke(nextObjective);
        }
    }
    
    private void CreateObjectiveUI(Objective objective)
    {
        if (objectiveUIPrefab == null || uiParent == null)
        {
            Debug.LogError("ObjectiveUI prefab or UI parent not assigned!");
            return;
        }
        
        GameObject uiObject = Instantiate(objectiveUIPrefab, uiParent);
        ObjectiveUI objectiveUI = uiObject.GetComponent<ObjectiveUI>();
        
        if (objectiveUI == null)
        {
            objectiveUI = uiObject.AddComponent<ObjectiveUI>();
        }
        
        objectiveUI.Initialize(objective);
        activeObjectiveUIs.Add(objectiveUI);
        
        // Position and animate
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.localPosition = offScreenPosition;
        
        // Calculate target position based on number of active objectives
        Vector3 targetPosition = onScreenPosition;
        targetPosition.y = onScreenPosition.y - (activeObjectiveUIs.Count - 1) * 95f; // Offset each objective
        
        StartCoroutine(AnimateObjectiveIn(rectTransform, targetPosition));
    }
    
    private IEnumerator AnimateObjectiveIn(RectTransform rectTransform, Vector3 targetPosition)
    {
        Vector3 startPosition = rectTransform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(t);
            
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }
        
        rectTransform.localPosition = targetPosition;
    }
    
    private IEnumerator AnimateObjectiveOut(RectTransform rectTransform, System.Action onComplete = null)
    {
        Vector3 startPosition = rectTransform.localPosition;
        Vector3 endPosition = new Vector3(Screen.width + 400f, startPosition.y, startPosition.z);
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(t);
            
            rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
            yield return null;
        }
        
        onComplete?.Invoke();
    }
    
    public void Produce(ProductType productType)
    {
        bool anyProgress = false;
        List<Objective> completedObjectives = new List<Objective>();
        
        // Check all active objectives
        for (int i = 0; i < activeObjectives.Count; i++)
        {
            Objective objective = activeObjectives[i];
            
            if (objective.TryIncrement(productType))
            {
                anyProgress = true;
                
                // Update UI
                if (i < activeObjectiveUIs.Count)
                {
                    activeObjectiveUIs[i].UpdateDisplay();
                }
                
                OnObjectiveProgress?.Invoke(objective);
                
                if (objective.isCompleted)
                {
                    completedObjectives.Add(objective);
                    OnObjectiveCompleted?.Invoke(objective);
                }
            }
        }
        
        // Handle completed objectives
        foreach (var completedObjective in completedObjectives)
        {
            CompleteObjective(completedObjective);
        }
    }
    
    private void CompleteObjective(Objective completedObjective)
    {
        int objectiveIndex = activeObjectives.IndexOf(completedObjective);
        
        if (objectiveIndex >= 0 && objectiveIndex < activeObjectiveUIs.Count)
        {
            ObjectiveUI uiToRemove = activeObjectiveUIs[objectiveIndex];
            RectTransform rectTransform = uiToRemove.GetComponent<RectTransform>();
            
            // Increment completed objectives counter
            completedObjectivesCount++;
            UpdateCompletionCounter();
            
            // Animate out and then destroy
            StartCoroutine(AnimateObjectiveOut(rectTransform, () => 
            {
                activeObjectives.RemoveAt(objectiveIndex);
                activeObjectiveUIs.RemoveAt(objectiveIndex);
                Destroy(uiToRemove.gameObject);
                
                // Reposition remaining objectives
                RepositionObjectiveUIs();
                
                // Activate next objective if available
                ActivateNextObjective();
            }));
        }
    }
    
    private void UpdateCompletionCounter()
    {
        if (completionCounterText != null)
        {
            completionCounterText.text = $"{completedObjectivesCount}/{totalObjectivesCount} Objectives Completed";
        }
    }
    
    private void RepositionObjectiveUIs()
    {
        for (int i = 0; i < activeObjectiveUIs.Count; i++)
        {
            if (activeObjectiveUIs[i] != null)
            {
                RectTransform rectTransform = activeObjectiveUIs[i].GetComponent<RectTransform>();
                Vector3 newPosition = onScreenPosition;
                newPosition.y = onScreenPosition.y - i * 95f;
                
                StartCoroutine(AnimateToPosition(rectTransform, newPosition));
            }
        }
    }
    
    private IEnumerator AnimateToPosition(RectTransform rectTransform, Vector3 targetPosition)
    {
        Vector3 startPosition = rectTransform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(t);
            
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            yield return null;
        }
        
        rectTransform.localPosition = targetPosition;
    }
    
    // Public utility methods
    public List<Objective> GetActiveObjectives()
    {
        return new List<Objective>(activeObjectives);
    }
    
    public int GetRemainingObjectivesCount()
    {
        return objectiveQueue.Count;
    }
    
    public bool HasActiveObjectives()
    {
        return activeObjectives.Count > 0;
    }
    
    public int GetCompletedObjectivesCount()
    {
        return completedObjectivesCount;
    }
    
    public int GetTotalObjectivesCount()
    {
        return totalObjectivesCount;
    }
    
    public void ResetAllObjectives()
    {
        // Clear active objectives and UIs
        foreach (var ui in activeObjectiveUIs)
        {
            if (ui != null && ui.gameObject != null)
            {
                Destroy(ui.gameObject);
            }
        }
        
        activeObjectives.Clear();
        activeObjectiveUIs.Clear();
        
        // Reinitialize
        InitializeObjectives();
        StartInitialObjectives();
        UpdateCompletionCounter();
    }
    
    // Method to add objectives at runtime
    public void AddObjective(Objective newObjective)
    {
        allObjectives.Add(newObjective);
        totalObjectivesCount++;
        UpdateCompletionCounter();
        
        if (activeObjectives.Count < maxActiveObjectives)
        {
            ActivateNextObjective();
        }
        else
        {
            objectiveQueue.Enqueue(newObjective);
        }
    }
}