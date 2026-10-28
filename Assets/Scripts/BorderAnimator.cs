using UnityEngine;
using UnityEngine.UI;

public class BorderAnimator : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 50f;   // 移动速度
    [SerializeField] private float moveRange = 20f;   // 移动范围

    private RectTransform rt;
    private Vector2 startPos;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    private void Update()
    {
        // 利用 Mathf.Sin 让元素在原始位置附近做正弦浮动
        float offset = Mathf.Sin(Time.time * moveSpeed * Mathf.Deg2Rad) * moveRange;
        rt.anchoredPosition = startPos + new Vector2(0, offset);
    }
}