using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int money;
    public int lives;
    public static GameManager Instance{ get; private set; }//全局访问，但是外部无法修改
    private void Awake()
    {
        if(Instance != null && Instance != this)//多个实例冲突
        {
            Destroy(this);//如果已经有实例了删除自己
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);//切换场景不删除
    }
    public void TakeDamage(int damage)
    {
        lives -= damage;
        if(lives <= 0)
        {
            //游戏结束
            //失败
            Time.timeScale = 0;
            UIManager.Instance.ShowGameOverPanel();
        }
    }
    public void AddMoney(int money)
    {
        this.money += money;
    }

    public void Victory()
    {
        //胜利
        Time.timeScale = 0; //暂停游戏
        UIManager.Instance.ShowVictoryPanel();
    }
}