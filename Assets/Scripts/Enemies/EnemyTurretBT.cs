using System;
using UnityEngine;

/// <summary>
/// CÂU 2 ĐỀ THI GAM301: Quái mới điều khiển bằng Behavior Tree (BT).
/// Các trạng thái theo yêu cầu:
/// a) Xoay tròn tại chỗ (Rotate on spot khi ngoài phạm vi 10m).
/// b) Bắn đạn vào Player nếu nhân vật trong phạm vi 10 đơn vị.
/// </summary>
[DisallowMultipleComponent]
public class EnemyTurretBT : MonoBehaviour
{
    [Header("=== CẤU HÌNH BEHAVIOR TREE ===")]
    [Tooltip("Phạm vi phát hiện Player (mặc định đúng 10 đơn vị theo đề bài)")]
    [SerializeField] private float attackRange = 10.0f;

    [Tooltip("Tốc độ xoay tròn tại chỗ (độ/giây)")]
    [SerializeField] private float idleRotateSpeed = 45.0f;

    [Tooltip("Tốc độ xoay ngắm mục tiêu (độ/giây)")]
    [SerializeField] private float aimSpeed = 8.0f;

    [Header("=== THÔNG SỐ TẤN CÔNG (PROJECTILE) ===")]
    [Tooltip("Tần suất bắn đạn (giây)")]
    [SerializeField] private float fireRate = 1.5f;

    [Tooltip("Tốc độ bay của viên đạn")]
    [SerializeField] private float bulletSpeed = 16.0f;

    [Tooltip("Vị trí nòng súng bắn đạn")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Pool key của viên đạn quái")]
    [SerializeField] private string bulletPoolKey = "EnemyBullet";

    [Header("=== CẤU HÌNH MÁU ===")]
    [Tooltip("Lượng máu tối đa")]
    [SerializeField] private float maxHealth = 60.0f;

    [Tooltip("Lượng máu hiện tại")]
    [SerializeField] private float currentHealth;

    [Tooltip("Thanh máu 3D hiển thị trên đỉnh đầu quái")]
    [SerializeField] private GAM301.UI.WorldSpaceHealthBar healthBar;

    private Transform playerTarget;
    private float nextFireTime;
    private float sqrAttackRange;
    private bool isDead = false;

    // Trạng thái Node của Behavior Tree
    public enum NodeStatus { Success, Failure, Running }

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        sqrAttackRange = attackRange * attackRange; // Tối ưu: bình phương cự ly, tránh dùng Vector3.Distance
        if (firePoint == null) firePoint = transform;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<GAM301.UI.WorldSpaceHealthBar>();
        }
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth, new Vector3(0f, 2.2f, 0f));
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Vận hành Cây hành vi (Tick Behavior Tree)
        TickBehaviorTree();
    }

    /// <summary>
    /// Vòng lặp Behavior Tree gồm 1 Root Selector:
    /// - Nhánh 1 (Sequence Tấn công): Kiểm tra Player <= 10m -> Xoay ngắm Player -> Bắn đạn
    /// - Nhánh 2 (Fallback): Xoay tròn tại chỗ
    /// </summary>
    private void TickBehaviorTree()
    {
        // 1. Selector: Thử thực thi nhánh Tấn công trước nếu Player trong cự ly 10m
        if (CheckPlayerInRange() == NodeStatus.Success)
        {
            AimAtPlayer();
            ShootAtPlayer();
            return;
        }

        // 2. Selector Fallback: Nếu không trong tầm 10m -> Xoay tròn tại chỗ (Yêu cầu a)
        RotateOnSpot();
    }

    #region BEHAVIOR TREE NODES
    /// <summary>
    /// Node Condition: Kiểm tra mục tiêu có trong phạm vi 10 đơn vị không?
    /// </summary>
    private NodeStatus CheckPlayerInRange()
    {
        if (playerTarget == null) return NodeStatus.Failure;

        // Tối ưu hóa: Dùng bình phương khoảng cách sqrMagnitude
        float sqrDist = (playerTarget.position - transform.position).sqrMagnitude;
        return sqrDist <= sqrAttackRange ? NodeStatus.Success : NodeStatus.Failure;
    }

    /// <summary>
    /// Node Action 1: Xoay mặt ngắm vào người chơi
    /// </summary>
    private void AimAtPlayer()
    {
        Vector3 dir = (playerTarget.position - transform.position);
        dir.y = 0f; // Khóa trục Y để quái không bị ngửa hoặc chúi đầu
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * aimSpeed);
        }
    }

    /// <summary>
    /// Node Action 2: Bắn đạn vào Player (Yêu cầu b)
    /// </summary>
    private void ShootAtPlayer()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward;
        Vector3 shootDir = (playerTarget.position - spawnPos).normalized;

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
    }

    /// <summary>
    /// Node Action 3: Xoay tròn tại chỗ (Yêu cầu a)
    /// </summary>
    private void RotateOnSpot()
    {
        transform.Rotate(Vector3.up, idleRotateSpeed * Time.deltaTime);
    }
    #endregion

    /// <summary>
    /// Nhận sát thương khi bị Player bắn trúng
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[EnemyTurretBT] Quái nhận {damage} sát thương (Máu còn: {currentHealth}/{maxHealth})");

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[EnemyTurretBT] Quái đã bị tiêu diệt!");

        if (healthBar != null)
        {
            healthBar.SetHealth(0f);
        }
        
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.Spawn("VFX_Explosion", transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn phạm vi 10 đơn vị trực quan trong Scene
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
