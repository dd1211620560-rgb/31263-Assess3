using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private float spawnDelay = 5f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Vector2 minBounds = new Vector2(-13f, -14f);
    [SerializeField] private Vector2 maxBounds = new Vector2(13f, 14f);

    private GameObject currentCherry;
    private Vector3 startPos, targetPos;
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
        float cx = (minBounds.x + maxBounds.x) * 0.5f;
        float cy = (minBounds.y + maxBounds.y) * 0.5f;

        switch (side)
        {
            case 0: // 从上方
                startPos = new Vector3(Random.Range(minBounds.x, maxBounds.x), maxBounds.y + 1f, 0);
                targetPos = new Vector3(startPos.x, minBounds.y - 1f, 0);
                break;
            case 1: // 下方
                startPos = new Vector3(Random.Range(minBounds.x, maxBounds.x), minBounds.y - 1f, 0);
                targetPos = new Vector3(startPos.x, maxBounds.y + 1f, 0);
                break;
            case 2: // 左方
                startPos = new Vector3(minBounds.x - 1f, Random.Range(minBounds.y, maxBounds.y), 0);
                targetPos = new Vector3(maxBounds.x + 1f, startPos.y, 0);
                break;
            default: // 右方
                startPos = new Vector3(maxBounds.x + 1f, Random.Range(minBounds.y, maxBounds.y), 0);
                targetPos = new Vector3(minBounds.x - 1f, startPos.y, 0);
                break;
        }

        currentCherry = Instantiate(cherryPrefab, startPos, Quaternion.identity);
        var sr = currentCherry.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 100;
        isMoving = true;
    }
}