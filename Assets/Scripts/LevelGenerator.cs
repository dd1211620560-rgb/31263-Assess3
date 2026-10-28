using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Map Data (左上象限)")]
    private int[,] levelMap = new int[,]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    [Header("Prefabs (在 Inspector 拖入)")]
    [SerializeField] private GameObject outerCornerPrefab; // 1
    [SerializeField] private GameObject outerWallPrefab;   // 2
    [SerializeField] private GameObject innerCornerPrefab; // 3
    [SerializeField] private GameObject innerWallPrefab;   // 4
    [SerializeField] private GameObject pelletPrefab;      // 5
    [SerializeField] private GameObject powerPelletPrefab; // 6
    [SerializeField] private GameObject tJunctionPrefab;   // 7
    [SerializeField] private GameObject ghostGatePrefab;   // 8

    private GameObject generatedContainer;

    private void Start()
    {
        // 1. 删除旧的手动关卡（如果你有名为 Level_Manual 的物体）
        GameObject manual = GameObject.Find("Level_Manual");
        if (manual != null) Destroy(manual);

        // 2. 生成新关卡
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        // 清空旧的生成物，避免重复生成
        if (generatedContainer != null) Destroy(generatedContainer);

        generatedContainer = new GameObject("Level_Generated");

        int[,] fullMap = BuildFullMap(levelMap);
        int rows = fullMap.GetLength(0);
        int cols = fullMap.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int type = fullMap[r, c];
                if (type == 0) continue; // 0 是空，跳过

                Vector3 pos = new Vector3(c, -r, 0); // 注意：左上角为 (0,0)，Y 轴向下递减
                GameObject prefab = GetPrefabByType(type);
                if (prefab == null) continue;

                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, generatedContainer.transform);
                obj.name = $"Tile_{r}_{c}_type{type}";

                // 如果是墙类，需要根据邻居计算旋转
                if (IsWall(type))
                {
                    float angle = DecideRotation(fullMap, r, c, type);
                    obj.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
    }

    private int[,] BuildFullMap(int[,] quadrant)
    {
        int qRows = quadrant.GetLength(0);
        int qCols = quadrant.GetLength(1);
        int effectiveRows = qRows - 1; // 忽略最后一行（空行）

        int fullRows = effectiveRows * 2 + 1;
        int fullCols = qCols * 2;
        int[,] full = new int[fullRows, fullCols];

        for (int r = 0; r < effectiveRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                int val = quadrant[r, c];
                // 左上 (原样)
                full[r, c] = val;
                // 右上 (水平镜像)
                full[r, fullCols - 1 - c] = val;
                // 左下 (垂直镜像)
                full[fullRows - 1 - r, c] = val;
                // 右下 (XY 镜像)
                full[fullRows - 1 - r, fullCols - 1 - c] = val;
            }
        }
        return full;
    }

    private bool IsWall(int type)
    {
        return type == 1 || type == 2 || type == 3 || type == 4 || type == 7 || type == 8;
    }

    private float DecideRotation(int[,] map, int r, int c, int type)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        bool hasUp = IsWallNeighbor(map, r - 1, c, rows, cols);
        bool hasRight = IsWallNeighbor(map, r, c + 1, rows, cols);
        bool hasDown = IsWallNeighbor(map, r + 1, c, rows, cols);
        bool hasLeft = IsWallNeighbor(map, r, c - 1, rows, cols);

        // 外墙 (2) / 内墙 (4)
        if (type == 2 || type == 4)
        {
            if (hasLeft && hasRight) return 0f;    // 横向
            if (hasUp && hasDown) return 90f;      // 纵向
            return 0f;
        }

        // 外角 (1) / 内角 (3) - 假设原始 Sprite 是左上 "┌"
        if (type == 1 || type == 3)
        {
            if (hasRight && hasDown) return 0f;    // 左上
            if (hasUp && hasRight) return 90f;     // 右上
            if (hasUp && hasLeft) return 180f;     // 右下
            if (hasDown && hasLeft) return 270f;   // 左下
            return 0f;
        }

        // T 型 (7) - 假设原始 Sprite 是"上 T"（缺下）
        if (type == 7)
        {
            if (hasUp && hasRight && hasLeft) return 0f;   // 上 T
            if (hasRight && hasDown && hasLeft) return 180f; // 下 T
            if (hasUp && hasDown && hasLeft) return 90f;   // 左 T
            if (hasUp && hasDown && hasRight) return 270f; // 右 T
            return 0f;
        }

        return 0f;
    }

    private bool IsWallNeighbor(int[,] map, int r, int c, int rows, int cols)
    {
        if (r < 0 || r >= rows || c < 0 || c >= cols) return false;
        int t = map[r, c];
        return t == 1 || t == 2 || t == 3 || t == 4 || t == 7 || t == 8;
    }

    private GameObject GetPrefabByType(int type)
    {
        switch (type)
        {
            case 1: return outerCornerPrefab;
            case 2: return outerWallPrefab;
            case 3: return innerCornerPrefab;
            case 4: return innerWallPrefab;
            case 5: return pelletPrefab;
            case 6: return powerPelletPrefab;
            case 7: return tJunctionPrefab;
            case 8: return ghostGatePrefab;
            default: return null;
        }
    }
}