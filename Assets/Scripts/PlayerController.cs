using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // 定义一个基础速度，可以在Unity面板里调整
    [Header("设置")]
    public float baseMoveSpeed = 5f; // 改名为 baseMoveSpeed 以示区分

    // 实际运行时的速度 (基础 + 存档加成)
    private float currentMoveSpeed;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // 游戏开始时，立即计算一次最终速度
        UpdateMoveSpeed();
    }

    //速度刷新接口
    // 这个方法会被 GameLevelManager 在购买物品后调用
    public void UpdateMoveSpeed()
    {
        // 检查属性管理器是否存在
        if (PlayerStats.instance != null)
        {
            // 1. 获取存档里的速度加成 
            float bonus = PlayerStats.instance.GetStat(StatType.MoveSpeed);

            // 2. 计算最终速度 = 基础 + 加成
            currentMoveSpeed = baseMoveSpeed + bonus;

            // Debug.Log($"[玩家移动] 速度已刷新: {currentMoveSpeed} (基础{baseMoveSpeed} + 加成{bonus})");
        }
        else
        {
            // 如果没找到管理器，就用基础速度兜底
            currentMoveSpeed = baseMoveSpeed;
        }
    }

    void Update()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            // 水平移动 (A / D)
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

            // 垂直移动 (W / S)
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;

            // 方向键支持
            if (Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;
            if (Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
        }

        moveInput = moveInput.normalized;

        if (moveInput.x != 0)
        {
            sr.flipX = moveInput.x < 0;
        }
    }

    void FixedUpdate()
    {
        // 使用计算后的 currentMoveSpeed 进行移动
        rb.linearVelocity = moveInput * currentMoveSpeed;
    }
}