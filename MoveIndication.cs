using UnityEngine;

public class MoveIndication : MonoBehaviour
{
    public Vector2Int pos;

    private void OnMouseDown()
    {
        GameManager.instance.MovePiece(pos);
    }
}
