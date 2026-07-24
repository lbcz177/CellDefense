using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width, height;
    public float cellSize;
    public GameObject cellPerfab;
    public Transform pathParent;

    Cell[,] cells;

    void Awake()
    {
        width = 20;
        height = 20;
        cellSize = 1f;

        GenerateGrid();
        MarkPathCells();
    }
    
    void GenerateGrid()
    {
        cells = new Cell[width, height];
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                Vector3 pos = new Vector3(i * cellSize, j * cellSize, 0);
                GameObject cellObject = Instantiate(cellPerfab, pos, Quaternion.identity);
                Cell cell = cellObject.GetComponent<Cell>();
                cells[i, j] = cell;
            }
        }
    }

    void MarkPathCells()
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0 ; j < height; j++)
            {
                Cell cell = cells[i, j];
                foreach(Transform point in pathParent)
                {
                    float x = Vector3.Distance(cell.transform.position, point.position);
                    if(x < cellSize / 2f)
                    {
                        cell.SetBuildable(false);
                        break;
                    }
                }
            }
        }
    }
}
