using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
public class UIManager : MonoBehaviour
{
    [SerializeField]Text goldText;
    [SerializeField]Text livesText;
    [SerializeField]Text waveText;
    [SerializeField] GameObject gameOverPanel;   // 面板引用
    [SerializeField] GameObject victoryPanel;     // 胜利面板（一起做）

    public static UIManager Instance { get; private set; }
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    void Update()
    {
        goldText.text = "金币：" + GameManager.Instance.money;
        livesText.text = "生命值：" + GameManager.Instance.lives;
        //waveText.text = "波次：" + WaveSpawner.Instance.waveIndex + "/" + WaveSpawner.Instance.totalWaves;
    }

    public void ShowGameOverPanel()
    {
        gameOverPanel.SetActive(true);
    }
    public void ShowVictoryPanel()
    {
        victoryPanel.SetActive(true);
    }  
    public void ResetGame()
    {
        Time.timeScale = 1;
        Destroy(GameManager.Instance.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
