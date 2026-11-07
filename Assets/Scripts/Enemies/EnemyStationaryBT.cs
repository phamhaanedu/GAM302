using System;
using UnityEngine;

/// <summary>
/// Quái đứng yên (Stationary Enemy) áp dụng cấu trúc Cây hành vi (Behavior Tree - BT) tối giản trong GAM301.
/// Phù hợp với thời lượng thi 60 phút, không rườm rà nhưng chuẩn kiến trúc node:
/// - Node 1: Kiểm tra người chơi trong tầm ngắm (CheckTargetInRangeNode)
/// - Node 2: Xoay mặt nhìn về phía mục tiêu (LookAtTargetNode)
/// - Node 3: Định kỳ bắn đạn ngắm bắn (ShootAtTargetNode)
/// </summary>
[DisallowMultipleComponent]
public class EnemyStationaryBT : MonoBehaviour
{
    [Header("=== CẤU HÌNH QUÁI ĐỨNG YÊN (TURRET / TOWER) ===")]
    [Tooltip("Lượng máu tối đa")]
    [SerializeField] private float maxHealth = 60.0f;

    [Tooltip("Lượng máu hiện tại")]
    [SerializeField] private float currentHealth;

    [Tooltip("Bán kính phát hiện và tấn công (m)")]
    [SerializeField] private float detectionRange = 12.0f;

    [Tooltip("Tốc độ xoay nòng súng về phía người chơi")]
    [SerializeField] private float turnSpeed = 5.0f;

    [Header("=== CẤU HÌNH TẤN CÔNG (PROJECTILE) ===")]
    [Tooltip("Vị trí nòng súng của quái")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Pool key của đạn quái (khớp với ObjectPoolManager)")]
    [SerializeField] private string bulletPoolKey = "EnemyBullet";

    [Tooltip("Tốc độ đạn bay")]
    [SerializeField] private float bulletSpeed = 15.0f;

    [Tooltip("Thời gian chờ giữa 2 lần bắn (giây)")]
    [SerializeField] private float shootInterval = 2.0f;

    private Transform playerTarget;
    private float shootTimer = 0f;
    private bool isDead = false;

    // Public Getters
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Vận hành Cây hành vi (Tick Behavior Tree)
        TickBehaviorTree();
    }

    /// <summary>
    /// Vòng lặp cập nhật các Node của Behavior Tree đơn giản.
    /// </summary>
    private void TickBehaviorTree()
    {
        // NODE 1: Kiểm tra có mục tiêu trong tầm không?
        if (playerTarget == null || Vector3.Distance(transform.position, playerTarget.position) > detectionRange)
        {
            // Trạng thái nghỉ (Idle)
            return;
        }

        // NODE 2: Xoay thân/nòng về phía mục tiêu
        Vector3 targetDirection = (playerTarget.position - transform.position).normalized;
        targetDirection.y = 0f; // Giữ thăng bằng mặt phẳng ngang
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
        }

        // NODE 3: Bắn đạn định kỳ nếu mục tiêu nằm trong góc ngắm
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            ExecuteShootAction();
        }
    }

    /// <summary>
    /// Thực thi hành động bắn đạn ra từ firePoint bằng ObjectPoolManager.
    /// </summary>
    private void ExecuteShootAction()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward;
        Vector3 shootDir = playerTarget != null ? (playerTarget.position - spawnPos).normalized : transform.forward;

        if (ObjectPoolManager.Instance != null)
        {
            GameObject bulletObj = ObjectPoolManager.Instance.Spawn(bulletPoolKey, spawnPos, Quaternion.LookRotation(shootDir));
            if (bulletObj != null)
            {
                BulletProjectile bullet = bulletObj.GetComponent<BulletProjectile>();
                if (bullet != null)
                {
                    bullet.Initialize(shootDir, bulletSpeed, "Enemy");
                }
            }
        }

        Debug.Log("[EnemyStationaryBT] Quái đứng yên bắn đạn về phía người chơi!");

        // EXAM HOOK: Hiệu ứng âm thanh bắn súng hoặc tia laser ngắm bắn
        OnShootActionHook();
    }

    /// <summary>
    /// Nhận sát thương khi bị đạn bắn trúng.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[EnemyStationaryBT] Trúng đạn: -{damage} HP (Còn: {currentHealth})");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Xử lý khi trụ/quái đứng yên bị tiêu diệt.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("[EnemyStationaryBT] Trụ quái đã bị phá hủy!");

        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.AddScore(100);
        }

        gameObject.SetActive(false);

        // EXAM HOOK: Sinh hiệu ứng nổ tan mảnh
        OnDestroyedHook();
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    protected virtual void OnShootActionHook()
    {
        // VÍ DỤ: Tia sáng báo động trước khi bắn 0.5s
    }

    protected virtual void OnDestroyedHook()
    {
        // VÍ DỤ: Mở cửa ải bí mật khi trụ bị hạ
    }
    #endregion
}
