using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;         // 每秒 5 格
    [SerializeField] private float arriveThreshold = 0.01f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource moveAudioSource;
    [SerializeField] private ParticleSystem dustEffect;
    [SerializeField] private GameManager gameManager;

    [Header("Level Bounds")]
    [SerializeField] private Vector2Int minGridPos = new Vector2Int(-13, -14);
    [SerializeField] private Vector2Int maxGridPos = new Vector2Int(13, 14);

    // 移动状态
    private Vector3 startPos;
    private Vector3 targetPos;
    private float lerpProgress;
    private bool isMoving;

    // 输入缓存
    private Vector2 lastInput = Vector2.right;
    private Vector2 currentInput = Vector2.right;

    // 网格坐标
    private Vector2Int currentGridPos;

    // 传送冷却
    private float teleportCooldown = 0f;

    public bool IsMoving => isMoving;
    public Vector2 CurrentInput => currentInput;

    private void Start()
    {
        startPos = transform.position;
        targetPos = transform.position;
        currentGridPos = new Vector2Int(Mathf.RoundToInt(startPos.x), Mathf.RoundToInt(startPos.y));
        currentInput = Vector2.right;
        lastInput = Vector2.right;
        UpdateAnimator(Vector2.right);

        if (moveAudioSource != null) moveAudioSource.loop = true;
        if (dustEffect != null) dustEffect.Stop();
    }

    private void Update()
    {
        if (teleportCooldown > 0f) teleportCooldown -= Time.deltaTime;

        // 如果 gameManager 存在且游戏未激活，则不处理输入
        if (gameManager != null && !gameManager.IsGameActive)
        {
            if (isMoving) transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);
            return;
        }

        // 读取 WASD
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) input = Vector2.up;
        else if (Input.GetKey(KeyCode.S)) input = Vector2.down;
        else if (Input.GetKey(KeyCode.A)) input = Vector2.left;
        else if (Input.GetKey(KeyCode.D)) input = Vector2.right;

        if (input != Vector2.zero) lastInput = input;

        if (!isMoving)
        {
            if (IsWalkable(currentGridPos + Vector2Int.RoundToInt(lastInput)))
            {
                currentInput = lastInput;
                StartMove(currentInput);
            }
            else if (IsWalkable(currentGridPos + Vector2Int.RoundToInt(currentInput)))
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
            lerpProgress += moveSpeed * Time.deltaTime;

            if (lerpProgress >= 1f - arriveThreshold)
            {
                transform.position = targetPos;
                currentGridPos = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));
                lerpProgress = 0f;
                isMoving = false;

                if (IsWalkable(currentGridPos + Vector2Int.RoundToInt(currentInput)))
                    StartMove(currentInput);
                else
                {
                    if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
                    if (dustEffect != null && dustEffect.isPlaying) dustEffect.Stop();
                }
            }
            else
            {
                transform.position = Vector3.Lerp(startPos, targetPos, lerpProgress);
            }
        }

        UpdateMovementFeedback();
    }

    private bool IsWalkable(Vector2Int grid)
    {
        Vector2 world = new Vector2(grid.x, grid.y);
        Collider2D hit = Physics2D.OverlapCircle(world, 0.1f, LayerMask.GetMask("Wall"));
        return hit == null;
    }
    private void StartMove(Vector2 direction)
    {
        startPos = transform.position;
        targetPos = startPos + new Vector3(direction.x, direction.y, 0f);
        lerpProgress = 0f;
        isMoving = true;

        if (moveAudioSource != null && !moveAudioSource.isPlaying) moveAudioSource.Play();
        if (dustEffect != null && !dustEffect.isPlaying) dustEffect.Play();
    }

    private void UpdateMovementFeedback()
    {
        if (isMoving) UpdateAnimator(currentInput);
    }

    private void UpdateAnimator(Vector2 dir)
    {
        if (animator == null) return;
        if (dir == Vector2.right) animator.Play("WalkRight");
        else if (dir == Vector2.left) animator.Play("WalkLeft");
        else if (dir == Vector2.up) animator.Play("WalkUp");
        else if (dir == Vector2.down) animator.Play("WalkDown");
    }

    // ---- 碰撞处理 ----
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            if (isMoving)
            {
                transform.position = startPos;
                lerpProgress = 0f;
                isMoving = false;
                if (gameManager != null) gameManager.PlayWallHitSFX();
                if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
                if (dustEffect != null && dustEffect.isPlaying) dustEffect.Stop();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (gameManager == null) return;

        if (other.CompareTag("Pellet"))
        {
            Destroy(other.gameObject);
            gameManager.AddScore(10);
        }
        else if (other.CompareTag("PowerPellet"))
        {
            Destroy(other.gameObject);
            gameManager.AddScore(50);
            gameManager.ActivatePowerPellet();
        }
        else if (other.CompareTag("Cherry"))
        {
            Destroy(other.gameObject);
            gameManager.AddScore(100);
            gameManager.PlayCherryEatSFX();
            var ch = FindObjectOfType<CherryController>();
            if (ch != null) ch.OnCherryEaten();
        }
        else if (other.CompareTag("TunnelLeft") && teleportCooldown <= 0f)
        {
            transform.position = new Vector3(maxGridPos.x + 1, transform.position.y, 0f);
            startPos = transform.position;
            targetPos = transform.position;
            currentGridPos = new Vector2Int(maxGridPos.x + 1, Mathf.RoundToInt(transform.position.y));
            teleportCooldown = 0.5f;
        }
        else if (other.CompareTag("TunnelRight") && teleportCooldown <= 0f)
        {
            transform.position = new Vector3(minGridPos.x - 1, transform.position.y, 0f);
            startPos = transform.position;
            targetPos = transform.position;
            currentGridPos = new Vector2Int(minGridPos.x - 1, Mathf.RoundToInt(transform.position.y));
            teleportCooldown = 0.5f;
        }
        else if (other.CompareTag("Ghost"))
        {
            var ghost = other.GetComponent<GhostController>();
            if (ghost != null) ghost.OnPacStudentCollide();
        }
    }

    // 供 Ghost 复活/重置时调用
    public void ResetToStart(Vector3 pos)
    {
        transform.position = pos;
        startPos = pos;
        targetPos = pos;
        lerpProgress = 0f;
        isMoving = false;
        currentGridPos = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        currentInput = Vector2.right;
        lastInput = Vector2.right;
        if (moveAudioSource != null && moveAudioSource.isPlaying) moveAudioSource.Stop();
        if (dustEffect != null && dustEffect.isPlaying) dustEffect.Stop();
        UpdateAnimator(Vector2.right);
    }
}