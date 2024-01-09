using UnityEngine;

public class BoardIndication : MonoBehaviour
{
    public Vector2Int pos;

    private void OnMouseDown()
    {
        GameManager.instance.CreateBoard(pos);
    }
}
