using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI progressText;
    
    [Header("Visual Effects")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject completionEffect;
    
    private Objective currentObjective;
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Auto-find TextMeshPro components if not assigned
        if (descriptionText == null || progressText == null)
        {
            TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>();
            
            if (textComponents.Length >= 2)
            {
                descriptionText = textComponents[0];
                progressText = textComponents[1];
            }
            else
            {
                Debug.LogError("ObjectiveUI prefab needs at least 2 TextMeshProUGUI components!");
            }
        }
        
        // Auto-find CanvasGroup if not assigned
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
    
    public void Initialize(Objective objective)
    {
        currentObjective = objective;
        isInitialized = true;
        UpdateDisplay();
    }
    
    public void UpdateDisplay()
    {
        if (!isInitialized || currentObjective == null)
            return;
            
        if (descriptionText != null)
        {
            descriptionText.text = currentObjective.description;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{currentObjective.currentAmount}/{currentObjective.targetAmount}";
            
            // Change color based on completion status
            if (currentObjective.isCompleted)
            {
                progressText.color = Color.green;
            }
            else if (currentObjective.currentAmount > 0)
            {
                progressText.color = Color.yellow;
            }
            else
            {
                progressText.color = Color.white;
            }
        }
        
        // Trigger completion effect if objective is completed
        if (currentObjective.isCompleted && completionEffect != null)
        {
            Instantiate(completionEffect, transform);
        }
    }
    
    public Objective GetObjective()
    {
        return currentObjective;
    }
    
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
    
    public void PlayCompletionAnimation()
    {
        // You can add additional completion animations here
        // For example, scale animation, color flash, etc.
        StartCoroutine(CompletionFlash());
    }
    
    private System.Collections.IEnumerator CompletionFlash()
    {
        Color originalColor = progressText.color;
        
        for (int i = 0; i < 3; i++)
        {
            progressText.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            progressText.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
        
        progressText.color = Color.green;
    }
}