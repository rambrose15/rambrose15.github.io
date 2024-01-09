using UnityEngine;
using UnityEngine.EventSystems;

public class EventManager : MonoBehaviour
{
    EventSystem es;
    [SerializeField] GameObject vButton, hButton, p1, p2;

    private void Start()
    {
        es = gameObject.GetComponent<EventSystem>();
    }
    void Update()
    {
        if (es.currentSelectedGameObject != vButton) GameManager.instance.bvActive = false;
        if (es.currentSelectedGameObject != hButton) GameManager.instance.bhActive = false;
        if (es.currentSelectedGameObject != p1) GameManager.instance.p1Active = false;
        if (es.currentSelectedGameObject != p2) GameManager.instance.p2Active = false;
    }
}
