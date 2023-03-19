using UnityEngine;

public class VisibilityCheck : MonoBehaviour
{
    private void OnBecameVisible()
    {
        Debug.Log($"{gameObject.name} became visible.");
    }

    private void OnBecameInvisible()
    {
        Debug.Log($"{gameObject.name} became invisible.");
    }
}