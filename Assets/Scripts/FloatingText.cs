using UnityEngine;
using TMPro; // 必须引用

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 100f;  // 上升速度 (因为是UI，单位是像素，要大点)
    public float fadeDuration = 1.0f; // 消失时间

    private TMP_Text textComp;
    private float timer = 0f;
    private Color startColor;

    void Awake()
    {
        textComp = GetComponent<TMP_Text>();
        startColor = textComp.color;
    }

    void Start()
    {
        // 确保时间到了自动销毁，防止无限堆积
        Destroy(gameObject, fadeDuration);
    }

    void Update()
    {
        // 1. 向上移动 (对于UI物体，通常修改 anchoredPosition 或直接 translate)
        // 这里为了简单适配不同 Canvas 模式，直接用世界坐标移动即可
        transform.Translate(Vector3.up * moveSpeed * Time.unscaledDeltaTime); // 注意用 unscaledDeltaTime，因为商店暂停了时间

        // 2. 处理淡出透明
        timer += Time.unscaledDeltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        textComp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
    }

    // 公共方法：供外部设置显示的文字内容
    public void SetText(string message)
    {
        if (textComp == null) textComp = GetComponent<TMP_Text>();
        textComp.text = message;
    }
}