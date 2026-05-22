using UnityEngine;
using TMPro;

public class CookableFood : MonoBehaviour
{
    [Header("Cook State")]
    [SerializeField] private string cookedText = "Cooked";
    [SerializeField] private string cookedTag = ""; 
    [SerializeField] private Material cookedMaterial;

    [Header("Targets")]
    [SerializeField] private string cubeChildName = "Cube";
    [SerializeField] private string textChildName = "textFood";

    private bool isCooked = false;

    public bool CanBeCooked()
    {
        return !isCooked;
    }

    public void Cook()
    {
        if (isCooked)
            return;

        isCooked = true;

        if (!string.IsNullOrEmpty(cookedTag))
        {
            gameObject.tag = cookedTag;
        }

        ApplyMaterialToCubes();
        UpdateTextChild();
    }

    private void ApplyMaterialToCubes()
    {
        if (cookedMaterial == null)
            return;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name != cubeChildName)
                continue;

            Renderer rend = child.GetComponent<Renderer>();
            if (rend != null)
                rend.material = cookedMaterial;
        }
    }

    private void UpdateTextChild()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name != textChildName)
                continue;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text != null)
                text.text = cookedText;
        }
    }
}