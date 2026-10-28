using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Vector2 minBounds = new Vector2(0f, -28f);
    [SerializeField] private Vector2 maxBounds = new Vector2(27f, 0f);

    private GameObject currentCherry;
    private Vector3 targetPos;
    private bool isMoving;
    private float timer;

    private void Start()
    {
        timer = spawnDelay;
    }

    private void Update()
    {
        if (!isMoving)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) SpawnCherry();
        }
        else if (currentCherry != null)
        {
            currentCherry.transform.position = Vector3.MoveTowards(
                currentCherry.transform.position, targetPos, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(currentCherry.transform.position, targetPos) < 0.1f)
            {
                Destroy(currentCherry);
                isMoving = false;
                timer = spawnDelay;
            }
        }
    }

    public void OnCherryEaten()
    {
        if (currentCherry != null) Destroy(currentCherry);
        isMoving = false;
        timer = spawnDelay;
    }

    private void SpawnCherry()
    {
        int side = Random.Range(0, 4);
        Vector3 startPos = Vector3.zero;

        switch (side)
        {
            case 0: // 上方
                startPos = new Vector3(Random.Range(minBounds.x, maxBounds.x), maxBounds.y + 2f, 0);
                targetPos = new Vector3(startPos.x, minBounds.y - 2f, 0);
                break;
            case 1: // 下方
                startPos = new Vector3(Random.Range(minBounds.x, maxBounds.x), minBounds.y - 2f, 0);
                targetPos = new Vector3(startPos.x, maxBounds.y + 2f, 0);
                break;
            case 2: // 左方
                startPos = new Vector3(minBounds.x - 2f, Random.Range(minBounds.y, maxBounds.y), 0);
                targetPos = new Vector3(maxBounds.x + 2f, startPos.y, 0);
                break;
            default: // 右方
                startPos = new Vector3(maxBounds.x + 2f, Random.Range(minBounds.y, maxBounds.y), 0);
                targetPos = new Vector3(minBounds.x - 2f, startPos.y, 0);
                break;
        }

        currentCherry = Instantiate(cherryPrefab, startPos, Quaternion.identity);
        var sr = currentCherry.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 100;
        isMoving = true;
    }
}