using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f; // 世界单位/秒

    [Header("Path (Clockwise)")]
    [SerializeField] private Vector3[] waypoints;

    [Header("Audio")]
    [SerializeField] private AudioSource moveAudioSource;

    private int currentTargetIndex = 0;
    private Animator animator;
    private Vector3 currentDirection;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("PacStudentMovement: need at least 2 waypoints.");
            return;
        }

        // 从第一个 waypoint 开始
        transform.position = waypoints[0];
        currentTargetIndex = 1;

        // 播放移动音效（循环）
        if (moveAudioSource != null && !moveAudioSource.isPlaying)
        {
            moveAudioSource.loop = true;
            moveAudioSource.Play();
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        // 1. 计算到目标点的方向和距离
        Vector3 target = waypoints[currentTargetIndex];
        Vector3 toTarget = target - transform.position;

        // 2. 到达目标 → 切换下一个 waypoint
        float step = speed * Time.deltaTime;
        if (toTarget.magnitude <= step)
        {
            // 精确贴到目标点，避免抖动
            transform.position = target;
            currentTargetIndex = (currentTargetIndex + 1) % waypoints.Length;
        }
        else
        {
            // 3. 帧率无关移动：方向 × 速度 × deltaTime
            currentDirection = toTarget.normalized;
            transform.position += currentDirection * step;
        }

        // 4. 更新动画状态
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // 根据方向切换动画状态
        if (Mathf.Abs(currentDirection.x) > Mathf.Abs(currentDirection.y))
        {
            animator.Play(currentDirection.x > 0 ? "WalkRight" : "WalkLeft");
        }
        else if (Mathf.Abs(currentDirection.y) > 0f)
        {
            animator.Play(currentDirection.y > 0 ? "WalkUp" : "WalkDown");
        }
    }
}