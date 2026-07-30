using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Pause : MonoBehaviour
{
   [SerializeField] private Button pauseButton;
   [SerializeField] private Button MenuButton;
   [SerializeField] private Button ResumeButton;

   [SerializeField] private GameObject pausePanel;

   private void Start()
   {
     pauseButton.onClick.AddListener(ActivatePausePanel);
     MenuButton.onClick.AddListener(GoToMenu);
     ResumeButton.onClick.AddListener(DeactivatePausePanel);
     pausePanel.SetActive(false); 
   }
   
   private void ActivatePausePanel()
   {
     pausePanel.SetActive(true);
     Time.timeScale = 0f;
   }
   
   private void DeactivatePausePanel()
   {
     Time.timeScale = 1f;
     pausePanel.SetActive(false);
   }

   private void GoToMenu()
   {
     Time.timeScale = 1f;
     SceneManager.LoadScene("ChoiseCar3D");
   }
}
