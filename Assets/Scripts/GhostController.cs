using System.Collections.Generic;
using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [Header("Identity")]
    public int ghostID = 1; // 1,2,3,4

    [Header("State")]
    public GhostState currentState = GhostState.Normal;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform spawnAreaCenter;
    [SerializeField] private Vector2Int spawnAreaExitTop;
    [SerializeField] private Vector2Int spawnAreaExitBottom;

    [Header("Movement")]
    [SerializeField] private float pacSpeedReference = 5f; // PacStudent 的速度基准
    [SerializeField] private Vector2Int minGridPos = new Vector2Int(-13, -14);
    [SerializeField] private Vector2Int maxGridPos = new Vector2Int(13, 14);
    [SerializeField] private LayerMask wallMask;

    [Header("Spawn")]
    [SerializeField] private float ghostRespawnDwellTime = 1.0f;

    // 移动状态
    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress;
    private Vector2 currentDir = Vector2.left;
    private Vector2 lastDir = Vector2.left;
    private Vector2Int currentGridPos;

    // 状态
    private bool hasExitedSpawn = false;
    private GhostState globalState = GhostState.Normal; // 全局状态（用于复活匹配）
    private float dwellTimer = 0f;

    public GhostState CurrentState => currentState;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        startPos = transform.position;
        targetPos = transform.position;
        currentGridPos = new Vector2Int(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.y));
        currentDir = Vector2.left;
        lastDir = Vector2.left;
        hasExitedSpawn = false;

        UpdateAnimator();
    }

    private void Update()
    {
        if (GameManagerRef != null && !GameManagerRef.IsGameActive) return;

        // Scared / Recovering 的时间由 GameManager 广播；
        // 这里只处理 Dead 状态到达中心的判断
        if (currentState == GhostState.Dead)
        {
            HandleDeadArrival();
        }

        // 移动推进
        float speed = GetCurrentSpeed();
        if (lerpProgress < 1f)
        {
            lerpProgress += (speed / pacSpeedReference) * Time.deltaTime;
            if (lerpProgress >= 1f)
            {
                transform.position = targetPos;
                currentGridPos = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));
                lerpProgress = 0f;
                OnReachedGrid();
            }
            else
            {
                transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);
            }
        }
    }

    private void OnReachedGrid()
    {
        if (dwellTimer > 0f)
        {
            dwellTimer -= Time.deltaTime;
            return;
        }

        // 复活期间原地等待
        if (currentState != GhostState.Dead && !hasExitedSpawn && AtSpawnExit())
        {
            hasExitedSpawn = true;
        }

        Vector2 nextDir = ChooseDirection();
        StartMove(nextDir);
    }

    private bool AtSpawnExit()
    {
        // 如果已经在出口位置，或已经离开出生区
        if (hasExitedSpawn) return true;
        return false;
    }

    private void StartMove(Vector2 dir)
    {
        lastDir = currentDir;
        currentDir = dir;

        startPos = transform.position;
        targetPos = startPos + new Vector3(dir.x, dir.y, 0f);
        lerpProgress = 0f;

        UpdateAnimator();
    }

    // ---------------- 速度 ----------------
    private float GetCurrentSpeed()
    {
        float normal = pacSpeedReference * 0.9f;
        switch (currentState)
        {
            case GhostState.Normal: return normal;
            case GhostState.Scared:
            case GhostState.Recovering:
            case GhostState.Dead: return normal * 0.5f;
            default: return normal;
        }
    }

    // ---------------- 状态切换 ----------------
    public void SetState(GhostState newState)
    {
        // 规则：Dead 状态下不能被改写
        if (currentState == GhostState.Dead && newState != GhostState.Dead) return;

        currentState = newState;
        globalState = newState;

        if (newState == GhostState.Dead)
        {
            // 立刻以直线方式飞回中心
            startPos = transform.position;
            targetPos = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
            lerpProgress = 0f;
        }
        else
        {
            UpdateAnimator();
        }
    }

    private void HandleDeadArrival()
    {
        if (spawnAreaCenter == null) return;
        float dist = Vector3.Distance(transform.position, spawnAreaCenter.position);
        if (dist < 0.15f)
        {
            // 复活到全局状态（或 Normal）
            transform.position = spawnAreaCenter.position;
            startPos = transform.position;
            targetPos = transform.position;
            lerpProgress = 0f;
            hasExitedSpawn = false;

            // 根据全局状态恢复
            currentState = (globalState == GhostState.Dead) ? GhostState.Normal : globalState;
            UpdateAnimator();
        }
    }

    // ---------------- PacStudent 碰撞 ----------------
    public void OnPacStudentCollide()
    {
        if (currentState == GhostState.Normal)
        {
            var gm = GameManagerRef;
            if (gm != null) gm.PlayerCaught();
        }
        else if (currentState == GhostState.Scared)
        {
            SetState(GhostState.Dead);
            var gm = GameManagerRef;
            if (gm != null) gm.AddScore(300);
        }
    }

    // ---------------- 方向决策 ----------------
    private Vector2 ChooseDirection()
    {
        if (currentState == GhostState.Dead)
        {
            Vector3 dirToCenter = (spawnAreaCenter.position - transform.position).normalized;
            return GetClosestCardinal(dirToCenter);
        }

        if (!hasExitedSpawn) return GetDirectionToExitSpawn();

        List<Vector2> valid = new List<Vector2>();
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (var d in dirs)
            if (IsValidMove(d)) valid.Add(d);

        if (valid.Count == 0) return -lastDir; // 唯一的选择：回头

        if (currentState == GhostState.Scared || currentState == GhostState.Recovering)
            return Ghost1Logic(valid);

        switch (ghostID)
        {
            case 1: return Ghost1Logic(valid);
            case 2: return Ghost2Logic(valid);
            case 3: return Ghost3Logic(valid);
            case 4: return Ghost4Logic(valid);
            default: return valid[Random.Range(0, valid.Count)];
        }
    }

    // ---------------- 4 只鬼的行为逻辑 ----------------
    private Vector2 Ghost1Logic(List<Vector2> valid)
    {
        float curDist = Vector2.Distance(currentGridPos, GetPacPos());
        List<Vector2> good = valid.FindAll(d => GetDistAfterStep(d) >= curDist);
        if (good.Count > 0) return good[Random.Range(0, good.Count)];
        return valid[Random.Range(0, valid.Count)];
    }

    private Vector2 Ghost2Logic(List<Vector2> valid)
    {
        float curDist = Vector2.Distance(currentGridPos, GetPacPos());
        List<Vector2> good = valid.FindAll(d => GetDistAfterStep(d) <= curDist);
        if (good.Count > 0) return good[Random.Range(0, good.Count)];
        return valid[Random.Range(0, valid.Count)];
    }

    private Vector2 Ghost3Logic(List<Vector2> valid)
    {
        return valid[Random.Range(0, valid.Count)];
    }

    private Vector2 Ghost4Logic(List<Vector2> valid)
    {
        // 顺时针优先级：右 -> 下 -> 左 -> 上（贴外圈）
        Vector2[] clockwise = { Vector2.right, Vector2.down, Vector2.left, Vector2.up };
        foreach (var d in clockwise)
            if (valid.Contains(d)) return d;
        return valid[Random.Range(0, valid.Count)];
    }

    private float GetDistAfterStep(Vector2 dir)
    {
        return Vector2.Distance(currentGridPos + dir, GetPacPos());
    }

    private Vector2 GetPacPos()
    {
        var p = FindObjectOfType<PacStudentController>();
        return p != null ? (Vector2)p.transform.position : Vector2.zero;
    }

    private Vector2 GetClosestCardinal(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x > 0 ? Vector2.right : Vector2.left;
        return dir.y > 0 ? Vector2.up : Vector2.down;
    }

    // ---------------- 出生区 ----------------
    private Vector2 GetDirectionToExitSpawn()
    {
        bool topExit = (ghostID == 1 || ghostID == 3);
        Vector2Int target = topExit ? spawnAreaExitTop : spawnAreaExitBottom;

        if (currentGridPos == target)
        {
            hasExitedSpawn = true;
            return topExit ? Vector2.up : Vector2.down;
        }

        // 简单：先水平对齐，再上下
        if (currentGridPos.x != target.x)
            return currentGridPos.x < target.x ? Vector2.right : Vector2.left;

        return currentGridPos.y < target.y ? Vector2.up : Vector2.down;
    }

    // ---------------- 通用有效移动判断 ----------------
    private bool IsValidMove(Vector2 dir)
    {
        if (dir == -lastDir) return false; // 不能回头
        Vector2Int next = currentGridPos + Vector2Int.RoundToInt(dir);
        return IsWalkable(next);
    }

    private bool IsWalkable(Vector2Int grid)
    {
        // 隧道不能走
        if (grid.x <= minGridPos.x || grid.x >= maxGridPos.x) return false;

        Vector2 world = new Vector2(grid.x, grid.y);
        var hit = Physics2D.OverlapCircle(world, 0.1f, wallMask);
        return hit == null;
    }

    // ---------------- 动画 ----------------
    private void UpdateAnimator()
    {
        if (animator == null) return;

        string prefix;
        switch (currentState)
        {
            case GhostState.Normal: prefix = "Walk"; break;
            case GhostState.Scared: prefix = "Scared"; break;
            case GhostState.Recovering: prefix = "Recovering"; break;
            case GhostState.Dead: prefix = "Dead"; break;
            default: prefix = "Walk"; break;
        }

        string dirSuffix = "Down";
        if (currentDir == Vector2.right) dirSuffix = "Right";
        else if (currentDir == Vector2.left) dirSuffix = "Left";
        else if (currentDir == Vector2.up) dirSuffix = "Up";

        string stateName = prefix + dirSuffix;

        // 若 Animator 不存在该状态，退回基础状态
        if (animator.HasState(0, Animator.StringToHash(stateName)))
            animator.Play(stateName);
        else if (animator.HasState(0, Animator.StringToHash(prefix)))
            animator.Play(prefix);
    }

    // ---------------- 外部接口 ----------------
    private GameManager GameManagerRef
    {
        get
        {
            if (_gm == null) _gm = FindObjectOfType<GameManager>();
            return _gm;
        }
    }
    private GameManager _gm;

    public void ResetToStart(Vector3 pos)
    {
        transform.position = pos;
        startPos = pos;
        targetPos = pos;
        lerpProgress = 0f;
        hasExitedSpawn = false;
        currentState = GhostState.Normal;
        globalState = GhostState.Normal;
        currentDir = Vector2.left;
        lastDir = Vector2.left;
        currentGridPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        UpdateAnimator();
    }
}