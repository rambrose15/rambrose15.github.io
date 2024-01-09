using UnityEngine;
using TMPro;

public class BoardCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tp1, tp2;

    void Update()
    {
        tp1.text = "P1: " + GameManager.instance.p1Boards.ToString();
        tp2.text = "P2: " + GameManager.instance.p2Boards.ToString();
    }
}
