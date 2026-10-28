using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f; // 每秒 5 格

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource moveAudioSource;
    [SerializeField] private ParticleSystem dustEffect;
    [SerializeField] private LayerMask wallLayer; // 在 Inspector 里勾选 Wall

    [Header("Grid Bounds")]
    [SerializeField] private Vector2Int minGridPos = new Vector2Int(0, -28);
    [SerializeField] private Vector2Int maxGridPos = new Vector2Int(27, 0);

    // 状态变量
    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress;
    private bool isMoving;
    private Vector2Int currentGridPos;
    private float teleportCooldown = 0f;

    // 输入缓存
    private Vector2 lastInput = Vector2.right;
    private Vector2 currentInput = Vector2.right;

    private Vector3 initialSpawn = new Vector3(1f, -1f, 0f); // 左上角起点

    private void Start()
    {
        ResetToStart();
    }

    public void ResetToStart()
    {
        transform.position = initialSpawn;
        startPos = initialSpawn;
        targetPos = initialSpawn;
        currentGridPos = new Vector2Int(Mathf.RoundToInt(initialSpawn.x), Mathf.RoundToInt(initialSpawn.y));
        lerpProgress = 0f;
        isMoving = false;
        lastInput = Vector2.right;
        currentInput = Vector2.right;
        UpdateAnimator(Vector2.right);

        if (dustEffect != null) dustEffect.Stop();
        if (moveAudioSource != null) moveAudioSource.Stop();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive) return;

        if (teleportCooldown > 0f) teleportCooldown -= Time.deltaTime;

        // 1. 获取输入
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) input = Vector2.up;
        else if (Input.GetKey(KeyCode.S)) input = Vector2.down;
        else if (Input.GetKey(KeyCode.A)) input = Vector2.left;
        else if (Input.GetKey(KeyCode.D)) input = Vector2.right;

        if (input != Vector2.zero) lastInput = input;

        // 2. 移动逻辑
        if (!isMoving)
        {
            if (IsWalkable(lastInput))
            {
                currentInput = lastInput;
                StartMove(currentInput);
            }
            else if (IsWalkable(currentInput))
            {
                StartMove(currentInput);
            }
            else
            {
                if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
                if (dustEffect != null && dustEffect.isPlaying) dustEffect.Stop();
            }
        }
        else
        {
            lerpProgress += Time.deltaTime * moveSpeed;
            if (lerpProgress >= 1f)
            {
                transform.position = targetPos;
                currentGridPos = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));
                lerpProgress = 0f;
                isMoving = false;

                if (IsWalkable(currentInput)) StartMove(currentInput);
            }
            else
            {
                transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);
            }
        }

        UpdateAnimator(currentInput);
    }

    private bool IsWalkable(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;
        Vector2Int nextGrid = currentGridPos + Vector2Int.RoundToInt(dir);

        // 边界检查（允许出界以进入隧道）
        if (nextGrid.x < minGridPos.x || nextGrid.x > maxGridPos.x) return true;
        if (nextGrid.y < minGridPos.y || nextGrid.y > maxGridPos.y) return false;

        Vector2 worldPos = new Vector2(nextGrid.x, nextGrid.y);
        // 检测是否碰到墙壁
        return !Physics2D.OverlapCircle(worldPos, 0.3f, wallLayer);
    }

    private void StartMove(Vector2 dir)
    {
        startPos = transform.position;
        targetPos = startPos + new Vector3(dir.x, dir.y, 0);
        lerpProgress = 0f;
        isMoving = true;

        if (moveAudioSource != null && !moveAudioSource.isPlaying) moveAudioSource.Play();
        if (dustEffect != null && !dustEffect.isPlaying) dustEffect.Play();
    }

    private void UpdateAnimator(Vector2 dir)
    {
        if (animator == null || dir == Vector2.zero) return;
        if (dir == Vector2.right) animator.Play("WalkRight");
        else if (dir == Vector2.left) animator.Play("WalkLeft");
        else if (dir == Vector2.up) animator.Play("WalkUp");
        else if (dir == Vector2.down) animator.Play("WalkDown");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null) return;

        if (other.CompareTag("Pellet"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.OnPelletEaten();
        }
        else if (other.CompareTag("PowerPellet"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.OnPowerPelletEaten();
        }
        else if (other.CompareTag("Cherry"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.OnCherryEaten();
            FindObjectOfType<CherryController>()?.OnCherryEaten();
        }
        else if (other.CompareTag("TunnelLeft") && teleportCooldown <= 0f)
        {
            // 从左侧出界，传送到右侧
            transform.position = new Vector3(maxGridPos.x + 0.5f, transform.position.y, 0f);
            startPos = targetPos = transform.position;
            currentGridPos = new Vector2Int(maxGridPos.x, currentGridPos.y);
            teleportCooldown = 0.3f;
        }
        else if (other.CompareTag("TunnelRight") && teleportCooldown <= 0f)
        {
            // 从右侧出界，传送到左侧
            transform.position = new Vector3(minGridPos.x - 0.5f, transform.position.y, 0f);
            startPos = targetPos = transform.position;
            currentGridPos = new Vector2Int(minGridPos.x, currentGridPos.y);
            teleportCooldown = 0.3f;
        }
        else if (other.CompareTag("Ghost"))
        {
            var ghost = other.GetComponent<GhostController>();
            if (ghost != null) ghost.OnPlayerCollide();
        }
    }
}