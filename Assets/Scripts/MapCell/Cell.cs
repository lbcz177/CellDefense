using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Cell : MonoBehaviour
{
    public bool isBuildable = true;//是否可建造
    public bool isOccupied = false;//是否被占用
    private SpriteRenderer spriteRenderer;
    public event Action<Cell> OnCellClicked;//定义一个事件，当单元格被点击时触发
    public event Action<Cell> OnCellHovered;//定义一个事件，当单元格被鼠标悬停时触发
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    void UpdateColor()//更新颜色
    {
        if(isOccupied)
        {
            spriteRenderer.color = Color.red;
        }
        else if (isBuildable)
        {
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.color = Color.gray;
        }
    }

    void OnMouseEnter()
    {
        if(!isBuildable || isOccupied)
        {
            return;
        }
        spriteRenderer.color = Color.green;
        OnCellHovered?.Invoke(this);//触发事件，通知监听者单元格被鼠标悬停
    }


    void OnMouseExit()
    {
        UpdateColor();
    }

    void OnMouseDown()
    {
        if(!isBuildable || isOccupied)
        {
            return;
        } 
        OnCellClicked?.Invoke(this);//触发事件，通知监听者单元格被点击
    }

    public void SetBuildable(bool buildable)
    {
        isBuildable = buildable;
        UpdateColor();
    }

    public void PlaceTower()
    {
        isOccupied = true;
        UpdateColor();
    }
}