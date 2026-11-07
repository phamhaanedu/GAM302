using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Quái tuần tra sử dụng máy trạng thái hữu hạn (Finite State Machine - FSM) trong GAM301.
/// Tuần tra tuần tự hoặc ngẫu nhiên giữa 3 điểm mốc Waypoint_A, Waypoint_B, Waypoint_C bằng NavMeshAgent.
/// Có sẵn các trạng thái: Patrol (Tuần tra), Chase (Truy đuổi), Attack (Tấn công) để sinh viên dễ dàng mở rộng.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrolFSM : MonoBehaviour
{
    public enum FSMState
    {
        Patrol,         // Tuần tra giữa các mốc
        Chase,          // Truy đuổi người chơi
        Attack,         // Tấn công người chơi
        ExamCoroutine   // [CÂU 1 ĐỀ THI] Di chuyển A -> B -> Chờ 1s -> Quay 90° -> Chờ 1s -> Fire
    }

    [Header("=== CẤU HÌNH MÁY TRẠNG THÁI (FSM) ===")]
    [Tooltip("Trạng thái hiện tại của quái")]
    [SerializeField] private FSMState currentState = FSMState.ExamCoroutine;

    [Header("=== ĐỀ THI GAM301 - CÂU 1 (COROUTINE MOVER) ===")]
    [Tooltip("Bật chế độ thực hiện Câu 1 đề thi GAM301 (A -> B -> Chờ 1s -> Quay 90° -> Chờ 1s -> In 'Fire')")]
    [SerializeField] private bool useExamCoroutineMode = true;

    [Tooltip("Điểm xuất phát A cho Câu 1")]
    [SerializeField] private Transform examPointA;

    [Tooltip("Điểm đến B cho Câu 1")]
    [SerializeField] private Transform examPointB;

    [Tooltip("Góc quay tại điểm B (độ)")]
    [SerializeField] private float examRotateAngle = 90.0f;

    [Tooltip("Tự động lặp lại quy trình để giám khảo quan sát")]
    [SerializeField] private bool loopExamSequence = true;

    private Coroutine _examRoutine;

    [Header("=== DANH SÁCH WAYPOINTS TUẦN TRA ===")]
    [Tooltip("Danh sách các mốc định vị tuần tra (A, B, C)")]
    [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();

    [Tooltip("Khoảng cách tối thiểu để nhận diện đã đến mốc (m)")]
    [SerializeField] private float waypointTolerance = 1.2f;

    [Tooltip("Thời gian đứng chờ tại mỗi mốc trước khi đi tiếp (giây)")]
    [SerializeField] private float waitTimeAtWaypoint = 1.0f;

    [Header("=== CẤU HÌNH PHÁT HIỆN & CHIẾN ĐẤU ===")]
    [Tooltip("Tầm nhìn phát hiện người chơi (m)")]
    [SerializeField] private float sightRange = 8.0f;

    [Tooltip("Tầm tấn công người chơi (m)")]
    [SerializeField] private float attackRange = 2.0f;

    [Tooltip("Lượng máu tối đa của quái")]
    [SerializeField] private float maxHealth = 50.0f;

    [Tooltip("Máu hiện tại")]
    [SerializeField] private float currentHealth;

    [Tooltip("Sát thương gây ra cho Player")]
    [SerializeField] private float attackDamage = 15.0f;

    [Tooltip("Khoảng cách giữa các đòn đánh (giây)")]
    [SerializeField] private float attackRate = 1.5f;

    [Header("=== CẤU HÌNH TỐC ĐỘ DI CHUYỂN ===")]
    [Tooltip("Tốc độ khi đi tuần tra")]
    [SerializeField] private float patrolSpeed = 2.0f;

    [Tooltip("Tốc độ khi rượt đuổi người chơi")]
    [SerializeField] private float chaseSpeed = 3.8f;

    [Header("=== THAM CHIẾU VISUAL & ANIMATION ===")]
    [Tooltip("Animator của model quái (tự động gán nếu để trống)")]
    [SerializeField] private Animator animator;

    [Tooltip("Thanh máu 3D hiển thị trên đỉnh đầu quái")]
    [SerializeField] private GAM301.UI.WorldSpaceHealthBar healthBar;

    private NavMeshAgent agent;
    private Transform playerTarget;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float nextAttackTime = 0f;
    private bool isDead = false;

    // Animator Hashes for maximum performance & zero GC
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int GetHitHash = Animator.StringToHash("GetHit");
    private static readonly int IsDieHash = Animator.StringToHash("isDie");

    // Public Getters
    public FSMState CurrentState => currentState;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        // Tự động tìm Player theo Tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        // Tự động tìm Waypoints nếu chưa được gán từ Inspector
        if (patrolWaypoints.Count == 0)
        {
            FindDefaultWaypoints();
        }

        // Tự động tìm và khởi tạo WorldSpaceHealthBar
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<GAM301.UI.WorldSpaceHealthBar>();
        }
        if (healthBar != null)
        {
            healthBar.Initialize(maxHealth, new Vector3(0f, 3.2f, 0f));
        }

        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }

        if (useExamCoroutineMode)
        {
            if (examPointA == null)
            {
                var wpA = GameObject.Find("Waypoint_A");
                if (wpA != null) examPointA = wpA.transform;
            }
            if (examPointB == null)
            {
                var wpB = GameObject.Find("Waypoint_B");
                if (wpB != null) examPointB = wpB.transform;
            }

            SetState(FSMState.ExamCoroutine);
            StartExamSequence();
        }
        else
        {
            MoveToNextWaypoint();
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Cập nhật tốc độ vào Animator để chuyển mượt giữa Idle và Walk
        UpdateAnimatorParams();

        switch (currentState)
        {
            case FSMState.ExamCoroutine:
                // Trạng thái Câu 1: Coroutine đang toàn quyền điều khiển di chuyển và quay 90 độ
                break;
            case FSMState.Patrol:
                UpdatePatrolState();
                break;
            case FSMState.Chase:
                UpdateChaseState();
                break;
            case FSMState.Attack:
                UpdateAttackState();
                break;
        }
    }

    #region EXAM GAM301 - CÂU 1 COROUTINE
    /// <summary>
    /// [ĐỀ THI GAM301 - CÂU 1]
    /// Kích hoạt chuỗi Coroutine: A -> B -> Chờ 1s -> Quay 90° -> Chờ 1s -> In "Fire".
    /// </summary>
    [ContextMenu("Start Exam Coroutine Sequence")]
    public void StartExamSequence()
    {
        if (_examRoutine != null) StopCoroutine(_examRoutine);
        _examRoutine = StartCoroutine(ExamMoveSequenceRoutine());
    }

    private System.Collections.IEnumerator ExamMoveSequenceRoutine()
    {
        do
        {
            Vector3 posA = examPointA != null ? examPointA.position : transform.position;
            Vector3 posB = examPointB != null ? examPointB.position : (posA + transform.forward * 10f);

            // 1. Đặt quái tại vị trí A
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(posA);
            }
            else
            {
                transform.position = posA;
            }

            Debug.Log($"<color=cyan>[Câu 1 - Enemy_Patrol_FSM] Bắt đầu di chuyển từ A ({posA}) tới B ({posB})...</color>");

            // Ra lệnh cho NavMeshAgent di chuyển tới B
            if (agent != null)
            {
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.5f;
                agent.SetDestination(posB);
            }

            // Chờ cho đến khi đối tượng đến vị trí B
            while (agent != null && (agent.pathPending || agent.remainingDistance > 0.8f))
            {
                yield return null;
            }

            // Đã đến vị trí B -> Dừng lại
            if (agent != null)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0f);
                animator.SetBool(IsMovingHash, false);
            }

            Debug.Log("<color=yellow>[Câu 1 - Enemy_Patrol_FSM] Đã đến vị trí B! Đang chờ 1 giây...</color>");

            // 2. Khi đối tượng đến vị trí B thì chờ 1 giây
            yield return new WaitForSeconds(1.0f);

            // 3. Sau đó quay 90 độ (hướng xoay quanh trục Y)
            Debug.Log("<color=yellow>[Câu 1 - Enemy_Patrol_FSM] Đang quay 90 độ...</color>");
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, examRotateAngle, 0f);

            while (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 180f * Time.deltaTime);
                yield return null;
            }
            transform.rotation = targetRot;

            // 4. Sau đó chờ 1 giây
            yield return new WaitForSeconds(1.0f);

            // 5. In ra cửa sổ console thông báo "Fire" (ĐÚNG NGUYÊN VĂN YÊU CẦU ĐỀ THI)
            Debug.Log("Fire");
          

            // Kích hoạt animation Attack của Golem
            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }

            if (loopExamSequence)
            {
                yield return new WaitForSeconds(2.0f);
            }

        } while (loopExamSequence);
    }
    #endregion

    /// <summary>
    /// Đồng bộ hóa trạng thái di chuyển với Animator.
    /// </summary>
    private void UpdateAnimatorParams()
    {
        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsMovingHash, speed > 0.15f);
    }

    /// <summary>
    /// Cập nhật logic khi đang ở trạng thái tuần tra (Patrol).
    /// </summary>
    private void UpdatePatrolState()
    {
        // Kiểm tra nếu phát hiện Player trong tầm ngắm (sqrMagnitude tối ưu CPU)
        if (playerTarget != null && (transform.position - playerTarget.position).sqrMagnitude <= sightRange * sightRange)
        {
            SetState(FSMState.Chase);
            return;
        }

        // Kiểm tra xem đã đến mốc tuần tra hiện tại chưa
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtWaypoint)
            {
                waitTimer = 0f;
                MoveToNextWaypoint();
            }
        }
    }

    /// <summary>
    /// Cập nhật logic khi đang ở trạng thái truy đuổi (Chase).
    /// </summary>
    private void UpdateChaseState()
    {
        if (playerTarget == null)
        {
            SetState(FSMState.Patrol);
            return;
        }

        float sqrDist = (transform.position - playerTarget.position).sqrMagnitude;
        float maxChaseDist = sightRange * 1.5f;

        // Nếu Player chạy quá xa khỏi tầm nhìn, quay lại tuần tra
        if (sqrDist > maxChaseDist * maxChaseDist)
        {
            SetState(FSMState.Patrol);
            MoveToNextWaypoint();
            return;
        }

        // Nếu Player đã nằm trong tầm tấn công, chuyển sang trạng thái Attack
        if (sqrDist <= attackRange * attackRange)
        {
            SetState(FSMState.Attack);
            return;
        }

        // Tối ưu hóa: Chỉ refresh destination khi Player di chuyển > 0.8m hoặc chưa có path (tránh nghẽn NavMesh async)
        if (agent != null && agent.isOnNavMesh && (!agent.hasPath || (agent.destination - playerTarget.position).sqrMagnitude > 0.64f))
        {
            agent.stoppingDistance = attackRange * 0.8f;
            agent.SetDestination(playerTarget.position);
        }
    }

    /// <summary>
    /// Cập nhật logic khi đang ở trạng thái tấn công (Attack).
    /// </summary>
    private void UpdateAttackState()
    {
        if (playerTarget == null)
        {
            SetState(FSMState.Patrol);
            return;
        }

        // Dừng di chuyển để tung đòn đánh
        agent.ResetPath();
        Vector3 targetLook = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        transform.LookAt(targetLook);

        float sqrDist = (transform.position - playerTarget.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange)
        {
            SetState(FSMState.Chase);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackRate;
            PerformAttack();
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái FSM.
    /// </summary>
    public void SetState(FSMState newState)
    {
        currentState = newState;
        Debug.Log($"[EnemyPatrolFSM] Chuyển trạng thái sang: {newState}");

        if (agent != null)
        {
            switch (newState)
            {
                case FSMState.Patrol:
                    agent.speed = patrolSpeed;
                    break;
                case FSMState.Chase:
                    agent.speed = chaseSpeed;
                    break;
                case FSMState.Attack:
                    agent.speed = 0f;
                    break;
            }
        }

        OnStateChangedHook(newState);
    }

    /// <summary>
    /// Thực hiện đòn tấn công lên người chơi.
    /// </summary>
    private void PerformAttack()
    {
        // Kích hoạt animation tấn công của Golem
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        if (playerTarget != null)
        {
            PlayerController player = playerTarget.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }

        // EXAM HOOK: Kích hoạt animation cào/bắn hoặc hiệu ứng âm thanh
        OnAttackHook();
    }

    /// <summary>
    /// Chuyển mốc tuần tra tiếp theo (tuần tự A -> B -> C -> A).
    /// </summary>
    private void MoveToNextWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Count == 0) return;

        Transform targetWp = patrolWaypoints[currentWaypointIndex];
        if (targetWp != null && agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(targetWp.position);
        }
        currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Count;
    }

    /// <summary>
    /// Tự động tìm các GameObject có tên Waypoint_A, Waypoint_B, Waypoint_C trong Scene.
    /// </summary>
    private void FindDefaultWaypoints()
    {
        string[] names = { "Waypoint_A", "Waypoint_B", "Waypoint_C" };
        foreach (string n in names)
        {
            GameObject wp = GameObject.Find(n);
            if (wp != null)
            {
                patrolWaypoints.Add(wp.transform);
            }
        }
    }

    /// <summary>
    /// Quái nhận sát thương từ đạn của Player.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"[EnemyPatrolFSM] Quái trúng đạn: -{damage} HP (Còn lại: {currentHealth})");

        // Cập nhật thanh máu 3D
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        // Kích hoạt phản ứng bị đạn bắn trúng (GetHit)
        if (animator != null && currentHealth > 0f)
        {
            animator.SetTrigger(GetHitHash);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Xử lý khi quái bị tiêu diệt.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("[EnemyPatrolFSM] Quái đã bị tiêu diệt!");

        // Ẩn thanh máu khi quái chết
        if (healthBar != null)
        {
            healthBar.SetHealth(0f);
        }

        // Kích hoạt animation chết
        if (animator != null)
        {
            animator.SetBool(IsDieHash, true);
        }

        // Tắt di chuyển và va chạm để không cản Player
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Cộng điểm cho GameMaster
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.AddScore(50);
        }

        // Biến mất sau khi kết thúc animation ngã xuống đất (3 giây)
        Destroy(gameObject, 3.0f);

        // EXAM HOOK: Sinh vật phẩm hồi máu hoặc hiệu ứng nổ lớn
        OnDeathHook();
    }

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    protected virtual void OnStateChangedHook(FSMState state)
    {
        // VÍ DỤ: Đổi màu đèn mắt của quái khi chuyển sang Chase/Attack
    }

    protected virtual void OnAttackHook()
    {
        // VÍ DỤ: Kích hoạt âm thanh gầm rú hoặc vfx chém
    }

    protected virtual void OnDeathHook()
    {
        // VÍ DỤ: Rơi tiền thưởng hoặc chìa khóa mở cửa
    }
    #endregion
}
