using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject reloadLevel;
    public GameObject newLevel;
    public GameObject saveLevel;
    public GameObject startGame;
    public GameObject loadLevel;
    public GameObject plusBtn;
    public GameObject minusBtn;
    public TextMeshProUGUI loadInputField;
    public TextMeshProUGUI saveInputField;
    public TextMeshProUGUI difficoultyText;
    public TextMeshProUGUI debugUI;
    public TextMeshProUGUI victoryUI;


    //private ref
    public static UIManager instance;

    private void Awake()
    {
        if (instance != null && instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        StartGameSetup();
    }

   
    void Update()
    {
        
    }

    public void StartGameSetup()
    {
        reloadLevel.SetActive(false);
        newLevel.SetActive(true);
        saveLevel.SetActive(true);
        startGame.SetActive(true);
    }

    public void ShowDebugUI(string text)
    {
        debugUI.text = text;
        debugUI.gameObject.SetActive(true);
        StartCoroutine(CloseDebugUI());
    }


    private IEnumerator CloseDebugUI()
    {
        yield return new WaitForSeconds(2);
        debugUI.gameObject.SetActive(false);
    }
}
