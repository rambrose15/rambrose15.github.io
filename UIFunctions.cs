using UnityEngine;
using UnityEngine.SceneManagement;

public class UIFunctions : MonoBehaviour
{
    public void VBoardButtonClick() {
        if (GameManager.instance.hPlayers == 0 || (GameManager.instance.hPlayers == 1 && !GameManager.instance.p1Turn)) return;
        GameManager.instance.bvActive = true;
        GameManager.instance.bhActive = false;
    }
    public void HBoardButtonClick() {
    if (GameManager.instance.hPlayers == 0 || (GameManager.instance.hPlayers == 1 && !GameManager.instance.p1Turn)) return;
    GameManager.instance.bvActive = false;
    GameManager.instance.bhActive = true;
    }

    public void P1ButtonClick()
    {
        if (GameManager.instance.p1Turn && GameManager.instance.hPlayers != 0) GameManager.instance.p1Active = true;
    }
    public void P2ButtonClick()
    {
        if (!GameManager.instance.p1Turn && GameManager.instance.hPlayers == 2) GameManager.instance.p2Active = true;
    }

    public void P2GameClick()
    {
        GameManager.instance.hPlayers = 2;
        GameManager.instance.menu.SetActive(false);
        GameManager.instance.p1Turn = (Random.Range(0, 2) == 0 ? true : false);
    }
    public void P1GameClick()
    {
        GameManager.instance.hPlayers = 1;
        GameManager.instance.menu.SetActive(false);
        GameManager.instance.p1Turn = (Random.Range(0, 2) == 0 ? true : false);
    }

    public void CPUGameClick()
    {
        GameManager.instance.hPlayers = 0;
        GameManager.instance.menu.SetActive(false);
        GameManager.instance.p1Turn = (Random.Range(0, 2) == 0 ? true : false);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
