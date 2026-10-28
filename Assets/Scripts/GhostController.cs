using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private int ghostID; // 1..4
    [SerializeField] private Animator animator;
    [SerializeField] private Vector3 spawnPosition;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;
    public int GhostID => ghostID;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void SetState(GhostState state)
    {
        if (CurrentState == GhostState.Dead && state != GhostState.Dead) return;
        CurrentState = state;
        UpdateAnimatorState();
    }

    private void UpdateAnimatorState()
    {
        if (animator == null) return;
        animator.SetInteger("GhostState", (int)CurrentState);
    }

    public void OnPlayerCollide()
    {
        if (CurrentState == GhostState.Normal || CurrentState == GhostState.Recovering)
        {
            GameManager.Instance?.OnPacStudentCaught();
        }
        else if (CurrentState == GhostState.Scared)
        {
            SetState(GhostState.Dead);
            GameManager.Instance?.OnGhostEaten();
        }
    }

    public void ResetToSpawn()
    {
        transform.position = spawnPosition;
        SetState(GhostState.Normal);
    }
}