using UnityEngine;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }

    [SerializeField] private int ghostID;
    [SerializeField] private Animator animator;
    [SerializeField] private Vector3 spawnPosition;

    public GhostState CurrentState { get; private set; } = GhostState.Normal;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spawnPosition == Vector3.zero) spawnPosition = transform.position;
    }

    public void SetState(GhostState state)
    {
        if (CurrentState == GhostState.Dead && state != GhostState.Dead) return;
        CurrentState = state;
        if (animator != null) animator.SetInteger("GhostState", (int)CurrentState);
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