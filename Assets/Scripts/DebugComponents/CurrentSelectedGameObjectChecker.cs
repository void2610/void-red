using UnityEngine;
using UnityEngine.EventSystems;

public class CurrentSelectedGameObjectChecker : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Update()
    {
        targetObject = EventSystem.current.currentSelectedGameObject;
    }
}
