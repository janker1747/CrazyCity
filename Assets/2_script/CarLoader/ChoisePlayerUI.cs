using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChoisePlayerUI : MonoBehaviour
{
    public void StartGame()
    {
        CarSelectionManager.Instance?.ConfirmSelection();
        SceneManager.LoadScene("MainGame");
    }
}
