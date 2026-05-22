using System.Collections;
using UnityEngine;

public class CookStation : MonoBehaviour
{
    [Header("Cooking")]
    [SerializeField] private float cookTime = 5f;

    private Coroutine cookRoutine;
    private GameObject currentFood;

    private void OnTriggerEnter(Collider other)
    {
        if (currentFood != null)
            return;

        CookableFood cookable = other.GetComponent<CookableFood>();
        if (cookable == null)
            return;

        if (!cookable.CanBeCooked())
            return;

        currentFood = other.gameObject;
        cookRoutine = StartCoroutine(CookRoutine(cookable));
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentFood == null)
            return;

        if (other.gameObject != currentFood)
            return;

        if (cookRoutine != null)
        {
            StopCoroutine(cookRoutine);
            cookRoutine = null;
        }

        currentFood = null;
    }

    private IEnumerator CookRoutine(CookableFood cookable)
    {
        yield return new WaitForSeconds(cookTime);

        if (cookable != null)
            cookable.Cook();

        currentFood = null;
        cookRoutine = null;
    }
}