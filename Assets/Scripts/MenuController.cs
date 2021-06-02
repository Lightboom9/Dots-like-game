using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _bestScoreTextbox;
    [SerializeField] private Button _startButton;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("BestScore"))
        {
            PlayerPrefs.SetInt("BestScore", 0);
        }

        _bestScoreTextbox.text = "Best score: " + PlayerPrefs.GetInt("BestScore");

        _startButton.onClick.AddListener(OnStartButtonClick);
    }

    private void OnStartButtonClick()
    {
        SceneManager.LoadScene(1);
    }
}
