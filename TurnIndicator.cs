using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    void Update()
    {
        if (GameManager.instance.p1Turn) transform.position = new Vector3(-7.5f, -4, transform.position.z);
        else transform.position = new Vector3(-7.5f, 4, transform.position.z);
    }
}
