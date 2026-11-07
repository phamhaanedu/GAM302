using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển nhân vật chính (Player_Hero) trong Đấu trường GAM301.
/// Quản lý di chuyển theo góc nhìn Camera, xoay nhân vật mượt mà theo hướng chạy,
/// xử lý trọng lực & nhảy bằng CharacterController, bắn đạn từ MuzzlePoint,
/// nhận sát thương, cập nhật thanh máu UI (HealthBar_Slider), hiệu ứng chớp đỏ (Image_DamageFlash)
/// và tương tác với CameraManager khi tử trận.
/// Đồng bộ kiến trúc và Input System Actions từ PlayerController chuẩn.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("=== CẤU HÌNH DI CHUYỂN & CAMERA ===")]
    [Tooltip("Tốc độ di chuyển cơ bản của người chơi (m/s)")]
    [SerializeField] private float moveSpeed = 6.0f;

    [Tooltip("Tốc độ xoay mặt nhân vật theo hướng di chuyển")]
    [SerializeField] private float rotationSpeed = 10.0f;

    [Tooltip("Lực nhảy của nhân vật")]
    [SerializeField] private float jumpForce = 5.0f;

    [Tooltip("Gia tốc trọng lực kéo xuống đất")]
    [SerializeField] private float gravity = -9.81f;

    [Tooltip("Kéo Main Camera (có CinemachineBrain) vào đây trong Inspector")]
    public Transform cam;

    [Header("=== CẤU HÌNH MÁU & SỨC KHỎE (HEALTH) ===")]
    [Tooltip("Lượng máu tối đa")]
    [SerializeField] private float maxHealth = 100.0f;

    [Tooltip("Lượng máu hiện tại")]
    [SerializeField] private float currentHealth;

    [Header("=== ĐIỂM XUẤT ĐẠN & FOCUS TỬ TRẬN (TRANSFORMS) ===")]
    [Tooltip("Vị trí đầu nòng súng để đạn xuất phát")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("Vị trí điểm Focus để Death Camera quay cận cảnh khi ngã xuống")]
    [SerializeField] private Transform deathCamTarget;

    [Tooltip("Anchor điểm đặt Camera góc nhìn thứ nhất (FPS Camera Root)")]
    [SerializeField] private Transform fpsCameraRoot;

    [Header("=== CẤU HÌNH GÓC NHÌN THỨ NHẤT (FPS CONTROLS) ===")]
    [Tooltip("Độ nhạy chuột khi ở góc nhìn thứ nhất")]
    [SerializeField] private float fpsMouseSensitivity = 0.15f;

    [Tooltip("Vẽ hồng tâm (Crosshair) ở giữa màn hình khi ở góc nhìn thứ nhất")]
    [SerializeField] private bool showFpsCrosshair = true;

    private float _fpsPitch = 0f;

    [Header("=== CẤU HÌNH BẮN ĐẠN (COMBAT) ===")]
    [Tooltip("Pool key của viên đạn người chơi (khớp với ObjectPoolManager)")]
    [SerializeField] private string bulletPoolKey = "PlayerBullet";

    [Tooltip("Tốc độ đạn bay xé gió cực lực")]
    [SerializeField] private float bulletSpeed = 45.0f;

    [Tooltip("Khoảng cách tối thiểu giữa 2 lần bắn (giây)")]
    [SerializeField] private float fireRate = 0.25f;

    [Header("=== GIAO DIỆN NGƯỜI DÙNG (UI ELEMENTS) ===")]
    [Tooltip("Thanh trượt hiển thị máu trên Canvas")]
    [SerializeField] private Slider healthBarSlider;

    [Tooltip("Hình ảnh chớp đỏ khi chịu sát thương")]
    [SerializeField] private Image damageFlashImage;

    [Tooltip("Tốc độ mờ dần của hiệu ứng chớp đỏ")]
    [SerializeField] private float flashFadeSpeed = 4.0f;

    [Header("=== HIỆU ỨNG BLUR MÀN HÌNH (CÂU 4) ===")]
    [Tooltip("Panel / Image làm mờ toàn màn hình khi trúng đạn")]
    [SerializeField] private Image screenBlurImage;

    [Tooltip("Thời gian tồn tại của hiệu ứng Blur khi dính đạn (đúng 1 giây theo đề bài)")]
    [SerializeField] private float blurDuration = 1.0f;

    [Header("=== GÓC NHÌN TỬ TRẬN (CÂU 1) ===")]
    [Tooltip("Góc nhìn camera khi nhân vật DIE (FirstPerson = góc nhìn của nhân vật)")]
    [SerializeField] private CameraManager.CameraViewMode deathCameraMode = CameraManager.CameraViewMode.FirstPerson;

    private Coroutine blurCoroutine;

    // Component nội bộ
    private CharacterController characterController;
    private Animator _anim;
    private InputSystem_Actions _input;

    // Vector và biến vận tốc di chuyển
    private Vector2 _dir;
    private Vector3 _move;
    private float _velocityY;
    private bool _isGrounded;

    // Trạng thái vận hành
    private float nextFireTime;
    private bool isDead = false;

    // Public Getters & Setters
    public Transform MuzzlePoint => muzzlePoint;
    public Transform DeathCamTarget => deathCamTarget;
    public Transform Cam { get => cam; set => cam = value; }
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsGrounded => _isGrounded;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();
        _input = new InputSystem_Actions();

        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        if (_input != null)
        {
            _input.Player.Enable();
            _input.Player.Move.performed += OnMoveInput;
            _input.Player.Move.canceled += OnMoveInput;
            _input.Player.Jump.performed += OnJumpInput;
            _input.Player.Attack.performed += OnAttackInput;
        }
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.Player.Move.performed -= OnMoveInput;
            _input.Player.Move.canceled -= OnMoveInput;
            _input.Player.Jump.performed -= OnJumpInput;
            _input.Player.Attack.performed -= OnAttackInput;
            _input.Player.Disable();
        }

        // Đảm bảo nhả chuột khi tắt component
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        // Tự động tìm Main Camera (có CinemachineBrain) nếu chưa kéo vào Inspector
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (_anim == null)
        {
            _anim = GetComponentInChildren<Animator>();
        }

        // Tự động tìm hoặc sinh FPS_CameraRoot
        if (fpsCameraRoot == null)
        {
            var existing = transform.Find("FPS_CameraRoot");
            if (existing != null)
            {
                fpsCameraRoot = existing;
            }
            else
            {
                var go = new GameObject("FPS_CameraRoot");
                fpsCameraRoot = go.transform;
                fpsCameraRoot.SetParent(transform, false);
                fpsCameraRoot.localPosition = new Vector3(0f, 0.75f, 0.25f);
                fpsCameraRoot.localRotation = Quaternion.identity;
            }
        }

        // Cập nhật thanh máu ban đầu
        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        if (PlayerHealthBarHUD.Instance != null)
        {
            PlayerHealthBarHUD.Instance.SetHealth(currentHealth, maxHealth);
        }

        // Ẩn chớp đỏ ban đầu
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }
    }

    private void Update()
    {
        if (isDead) return;

        HandleMovement();
        HandleShooting();
        HandleDamageFlashFade();
    }

    [Header("=== CẤU HÌNH HERO LOCOMOTION & ROTATION ===")]
    [Tooltip("Tốc độ quay mặt nhân vật khi di chuyển (độ/giây)")]
    [SerializeField] private float turnSpeed = 720f;

    [Tooltip("Gia tốc bắt nhịp di chuyển (độ nhạy phím)")]
    [SerializeField] private float acceleration = 35f;

    [Tooltip("Thời gian duy trì tư thế ngắm bắn sau khi bắn phát đạn cuối cùng (giây)")]
    [SerializeField] private float aimDurationAfterShoot = 0.5f;

    [Header("=== CẤU HÌNH DASH / SPRINT LƯỚT NÉ ĐÒN ===")]
    [Tooltip("Hệ số tăng tốc khi lướt né đòn")]
    [SerializeField] private float dashMultiplier = 2.2f;

    [Tooltip("Thời gian tồn tại của một cú lướt (giây)")]
    [SerializeField] private float dashDuration = 0.22f;

    [Tooltip("Thời gian hồi giữa 2 lần lướt (giây)")]
    [SerializeField] private float dashCooldown = 0.8f;

    private float lastShootTime = -10f;
    private float dashTimer = 0f;
    private float lastDashTime = -10f;
    private bool isDashing = false;
    private Vector3 dashDirection;
    private Vector3 _currentAimPoint;
    private bool _hasAimPoint = false;

    /// <summary>
    /// Xử lý di chuyển theo hệ quy chiếu Camera và xoay nhân vật theo hướng di chuyển chuẩn giáo trình GAM301.
    /// Tích hợp 2 chế độ điều khiển riêng biệt:
    /// 1. First-Person Mode (FPS): Chuột điều khiển Yaw/Pitch, WASD đi tiến lùi & đi ngang (Strafe).
    /// 2. Third-Person Mode: Xoay mặt theo hướng chạy WASD hoặc lia súng theo con trỏ chuột.
    /// </summary>
    private void HandleMovement()
    {
        bool isFirstPerson = CameraManager.Instance != null && CameraManager.Instance.CurrentMode == CameraManager.CameraViewMode.FirstPerson;

        // Fallback đọc phím từ bàn phím nếu InputSystem_Actions chưa nhận giá trị
        if (_input == null || _dir == Vector2.zero)
        {
            Vector2 fallback = ReadMoveInputFallback();
            if (fallback != Vector2.zero) _dir = fallback;
        }

        if (cam == null && Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        // ==================== 1. CHẾ ĐỘ GÓC NHÌN THỨ NHẤT (FPS) ====================
        if (isFirstPerson)
        {
            HandleFPSLook();

            // FPS Movement: Tiến lùi theo mặt thân người, di ngang theo mạn sườn
            Vector3 camF = transform.forward;
            Vector3 camR = transform.right;
            camF.y = 0f;
            camR.y = 0f;
            camF.Normalize();
            camR.Normalize();

            Vector3 move = camF * _dir.y + camR * _dir.x;
            move = Vector3.ClampMagnitude(move, 1f);

            // Trọng lực
            if (characterController.isGrounded && _velocityY < 0f)
            {
                _velocityY = -2f;
            }
            _velocityY += gravity * Time.deltaTime;
            move.y = _velocityY;

            characterController.Move(new Vector3(move.x * moveSpeed, move.y, move.z * moveSpeed) * Time.deltaTime);

            // Cập nhật Animator
            float speed = _dir.magnitude;
            if (_anim != null)
            {
                _anim.SetFloat("Speed", speed);
                _anim.SetBool("isRun", speed > 0.01f);
            }

            OnMovementHook(move);
            return;
        }

        // ==================== 2. CHẾ ĐỘ GÓC NHÌN THỨ 3 (THIRD PERSON) ====================
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (fpsCameraRoot != null && fpsCameraRoot.localEulerAngles != Vector3.zero)
        {
            fpsCameraRoot.localEulerAngles = Vector3.zero;
            _fpsPitch = 0f;
        }

        if (cam == null) return;

        // 1. Lấy hướng tiến/phải của camera, bỏ trục Y để không bay lên/xuống theo góc nhìn
        Vector3 cam3rdF = cam.forward;
        Vector3 cam3rdR = cam.right;
        cam3rdF.y = 0f;
        cam3rdR.y = 0f;
        cam3rdF.Normalize();
        cam3rdR.Normalize();

        // 2. Tính hướng di chuyển thực tế theo camera
        Vector3 move3rd = cam3rdF * _dir.y + cam3rdR * _dir.x;
        move3rd = Vector3.ClampMagnitude(move3rd, 1f); // tránh đi chéo bị nhanh hơn đi thẳng

        // 3. Xử lý trọng lực
        if (characterController.isGrounded && _velocityY < 0f)
        {
            _velocityY = -2f; // giữ nhân vật bám sát mặt đất thay vì để = 0
        }
        _velocityY += gravity * Time.deltaTime;
        move3rd.y = _velocityY;

        // 4. Di chuyển nhân vật
        characterController.Move(new Vector3(move3rd.x * moveSpeed, move3rd.y, move3rd.z * moveSpeed) * Time.deltaTime);

        // 5. Cập nhật Animator: Speed = độ lớn input (0 = đứng yên, 1 = đi bộ full)
        float speed3rd = _dir.magnitude;
        if (_anim != null)
        {
            _anim.SetFloat("Speed", speed3rd);
            _anim.SetBool("isRun", speed3rd > 0.01f);
        }

        // 6. Xoay hướng nhân vật trong góc nhìn thứ 3:
        bool isAiming = (Time.time - lastShootTime <= aimDurationAfterShoot) || ReadShootInputFallback();

        if (isAiming || _dir.magnitude <= 0.01f)
        {
            RotateTowardsMouse(false);
        }
        else if (_dir.magnitude > 0f)
        {
            move3rd.y = 0f;
            if (move3rd.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move3rd, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }

        // EXAM HOOK: Bổ sung hành vi di chuyển mở rộng
        OnMovementHook(move3rd);
    }

    /// <summary>
    /// Xử lý quay góc nhìn chuột mượt mà và trực tiếp theo chuẩn Game Bắn Súng Góc Nhìn Thứ Nhất (FPS).
    /// </summary>
    private void HandleFPSLook()
    {
        // 1. Quản lý khóa con trỏ chuột
        if (Cursor.lockState != CursorLockMode.Locked)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#endif
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
#endif
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        // 2. Đọc độ di chuyển chuột (Mouse Delta)
        Vector2 mouseDelta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }
#endif
        if (mouseDelta == Vector2.zero)
        {
            try
            {
                mouseDelta = new Vector2(Input.GetAxis("Mouse X") * 8f, Input.GetAxis("Mouse Y") * 8f);
            }
            catch { }
        }

        // 3. Xoay thân người theo phương ngang (Yaw)
        float yaw = mouseDelta.x * fpsMouseSensitivity;
        transform.Rotate(Vector3.up, yaw, Space.World);

        // 4. Ngửa đầu / chĩa camera lên xuống (Pitch)
        _fpsPitch = Mathf.Clamp(_fpsPitch - mouseDelta.y * fpsMouseSensitivity, -75f, 75f);
        if (fpsCameraRoot != null)
        {
            fpsCameraRoot.localEulerAngles = new Vector3(_fpsPitch, 0f, 0f);
        }

        // 5. Căn chỉnh điểm ngắm bắn thẳng vào tâm màn hình (Center Crosshair)
        Camera activeCam = Camera.main;
        if (activeCam == null && cam != null) activeCam = cam.GetComponent<Camera>();
        if (activeCam != null)
        {
            Ray centerRay = activeCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int layerMask = ~LayerMask.GetMask("Ignore Raycast");
            if (Physics.Raycast(centerRay, out RaycastHit hit, 300f, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.root != transform.root) _currentAimPoint = hit.point;
                else _currentAimPoint = centerRay.origin + centerRay.direction * 100f;
            }
            else
            {
                _currentAimPoint = centerRay.origin + centerRay.direction * 100f;
            }
            _hasAimPoint = true;

            if (muzzlePoint != null)
            {
                Vector3 muzzleAim = _currentAimPoint - muzzlePoint.position;
                if (muzzleAim.sqrMagnitude > 0.01f)
                {
                    muzzlePoint.rotation = Quaternion.LookRotation(muzzleAim);
                }
            }
        }
    }

    /// <summary>
    /// Xoay hướng nhân vật và nòng súng về phía con trỏ chuột với độ chính xác cao.
    /// Chỉ dùng cho góc nhìn thứ 3.
    /// </summary>
    /// <param name="instant">True nếu cần xoay tức thời khi bắn đạn</param>
    private void RotateTowardsMouse(bool instant = false)
    {
        // Khi ở góc nhìn thứ nhất, chuột đã trực tiếp điều khiển quay thân người trong HandleFPSLook()
        if (CameraManager.Instance != null && CameraManager.Instance.CurrentMode == CameraManager.CameraViewMode.FirstPerson)
        {
            return;
        }
        Camera activeCam = Camera.main;
        if (activeCam == null && cam != null) activeCam = cam.GetComponent<Camera>();
        if (activeCam == null) return;

        Vector2 mousePos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
#endif
        if (mousePos == Vector2.zero)
        {
            try { mousePos = Input.mousePosition; } catch { }
        }

        Ray ray = activeCam.ScreenPointToRay(mousePos);
        _hasAimPoint = false;

        // 1. Raycast kiểm tra va chạm với vật thể/quái vật trong Scene trước
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        if (Physics.Raycast(ray, out RaycastHit hit, 300f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Tránh tự bắn trúng bản thân
            if (hit.collider.transform.root != transform.root)
            {
                _currentAimPoint = hit.point;
                _hasAimPoint = true;
            }
        }

        // 2. Nếu không trúng vật thể nào, chiếu tia lên mặt phẳng ngang cùng độ cao với súng
        if (!_hasAimPoint)
        {
            float aimHeight = muzzlePoint != null ? muzzlePoint.position.y : transform.position.y + 1.33f;
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, aimHeight, 0f));
            if (aimPlane.Raycast(ray, out float enterDistance))
            {
                _currentAimPoint = ray.GetPoint(enterDistance);
                _hasAimPoint = true;
            }
            else
            {
                // Fallback mặt đất
                Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (groundPlane.Raycast(ray, out float gDist))
                {
                    _currentAimPoint = ray.GetPoint(gDist);
                    _currentAimPoint.y = aimHeight;
                    _hasAimPoint = true;
                }
            }
        }

        if (_hasAimPoint)
        {
            // Xoay thân người theo mặt phẳng ngang
            Vector3 lookDirection = _currentAimPoint - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                if (instant)
                {
                    transform.rotation = targetRot;
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }
            }

            // Xoay MuzzlePoint trực tiếp về điểm ngắm
            if (muzzlePoint != null)
            {
                Vector3 muzzleAim = _currentAimPoint - muzzlePoint.position;
                if (muzzleAim.sqrMagnitude > 0.05f)
                {
                    muzzlePoint.rotation = Quaternion.LookRotation(muzzleAim);
                }
            }
        }
    }

    /// <summary>
    /// Xử lý trọng lực, kiểm tra chạm đất và ổn định độ bám sàn cho nhân vật.
    /// </summary>
    private void Gravity()
    {
        _isGrounded = characterController.isGrounded;

        if (_isGrounded && _velocityY < 0f)
        {
            _velocityY = -2f; // Cố định một giá trị nhỏ để giữ nhân vật bám sát mặt sàn dốc, tránh bị nảy
        }
        else if (!_isGrounded)
        {
            _velocityY += gravity * Time.deltaTime; // Tích lũy gia tốc trọng lực kéo xuống
        }
    }

    /// <summary>
    /// Thực hiện hành vi nhảy lên khỏi mặt đất theo công thức vật lý.
    /// </summary>
    public void Jump()
    {
        if (!_isGrounded) return;

        _velocityY = Mathf.Sqrt(jumpForce * -2f * gravity);
        if (_anim != null) _anim.SetTrigger("isJump");
    }

    #region INPUT SYSTEM CALLBACKS & FALLBACKS
    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        _dir = ctx.ReadValue<Vector2>();
    }

    private void OnJumpInput(InputAction.CallbackContext ctx)
    {
        Jump();
    }

    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        TryShoot();
    }

    /// <summary>
    /// Đọc phím di chuyển dự phòng nếu hệ thống Input Actions bị ngắt kết nối.
    /// </summary>
    private Vector2 ReadMoveInputFallback()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        }
#endif

        if (input == Vector2.zero)
        {
            try
            {
                input.x = Input.GetAxisRaw("Horizontal");
                input.y = Input.GetAxisRaw("Vertical");
            }
            catch
            {
                // Bỏ qua lỗi nếu Unity tắt hoàn toàn Input Manager cũ
            }
        }

        return input;
    }

    /// <summary>
    /// Đọc tín hiệu bắn dự phòng (Chuột trái / Phím Space).
    /// </summary>
    private bool ReadShootInputFallback()
    {
        bool shoot = false;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (mouse != null && (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame))
        {
            shoot = true;
        }
        if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.spaceKey.wasPressedThisFrame))
        {
            shoot = true;
        }
#endif

        if (!shoot)
        {
            try
            {
                shoot = Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space);
            }
            catch
            {
                // Bỏ qua lỗi nếu Unity tắt hoàn toàn Input Manager cũ
            }
        }

        return shoot;
    }
    #endregion

    #region COMBAT & HEALTH
    /// <summary>
    /// Kiểm tra thời gian hồi chiêu và thực hiện bắn đạn liên tục khi giữ chuột.
    /// </summary>
    private void HandleShooting()
    {
        if (ReadShootInputFallback() && Time.time >= nextFireTime)
        {
            TryShoot();
        }
    }

    /// <summary>
    /// Kiểm tra fire rate và bắn đạn.
    /// </summary>
    public void TryShoot()
    {
        if (isDead) return;

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    /// <summary>
    /// Thực thi lệnh bắn đạn từ MuzzlePoint và kích hoạt trigger Animator.
    /// </summary>
    public void Shoot()
    {
        lastShootTime = Time.time;

        bool isFirstPerson = CameraManager.Instance != null && CameraManager.Instance.CurrentMode == CameraManager.CameraViewMode.FirstPerson;
        if (isFirstPerson)
        {
            Camera activeCam = Camera.main;
            if (activeCam == null && cam != null) activeCam = cam.GetComponent<Camera>();
            if (activeCam != null)
            {
                Ray centerRay = activeCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                int layerMask = ~LayerMask.GetMask("Ignore Raycast");
                if (Physics.Raycast(centerRay, out RaycastHit hit, 300f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.root != transform.root) _currentAimPoint = hit.point;
                    else _currentAimPoint = centerRay.origin + centerRay.direction * 100f;
                }
                else
                {
                    _currentAimPoint = centerRay.origin + centerRay.direction * 100f;
                }
                _hasAimPoint = true;
            }
        }
        else
        {
            // Cập nhật hướng ngắm và xoay tức thời về hướng chuột trong góc nhìn thứ 3
            RotateTowardsMouse(true);
        }

        Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position + transform.forward + Vector3.up * 1.33f;
        
        // Hướng bắn chính xác từ nòng súng đến điểm con trỏ chuột
        Vector3 shootDirection = transform.forward;
        if (_hasAimPoint)
        {
            Vector3 aimVec = _currentAimPoint - spawnPos;
            if (aimVec.sqrMagnitude > 0.05f)
            {
                shootDirection = aimVec.normalized;
            }
        }
        else if (muzzlePoint != null)
        {
            shootDirection = muzzlePoint.forward;
        }

        Quaternion spawnRot = Quaternion.LookRotation(shootDirection);

        if (ObjectPoolManager.Instance != null)
        {
            GameObject bulletObj = ObjectPoolManager.Instance.Spawn(bulletPoolKey, spawnPos, spawnRot);
            if (bulletObj != null)
            {
                BulletProjectile bullet = bulletObj.GetComponent<BulletProjectile>();
                if (bullet != null)
                {
                    bullet.Initialize(shootDirection, bulletSpeed, "Player");
                }
            }
        }

        // Kích hoạt hoạt ảnh tấn công
        if (_anim != null)
        {
            _anim.SetTrigger("isAtk");
        }

        // EXAM HOOK: Kích hoạt âm thanh bắn súng hoặc giật camera tại đây
        OnShootHook();
    }

    /// <summary>
    /// Người chơi nhận sát thương từ đối tượng khác (quái, đạn quái, bẫy).
    /// </summary>
    /// <param name="damage">Lượng sát thương phải gánh chịu</param>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Cập nhật Slider UI
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }

        // Cập nhật giao diện thanh máu kép PlayerHealthBarHUD chuẩn AAA
        if (PlayerHealthBarHUD.Instance != null)
        {
            PlayerHealthBarHUD.Instance.SetHealth(currentHealth, maxHealth);
        }

        // Kích hoạt chớp đỏ màn hình
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0.6f;
            damageFlashImage.color = c;
        }

        // CÂU 4: Kích hoạt hiệu ứng Blur toàn màn hình xuất hiện trong 1s
        TriggerScreenBlur(blurDuration);

        Debug.Log($"[PlayerController] Nhận sát thương: {damage} (Máu còn: {currentHealth}/{maxHealth})");

        // EXAM HOOK: Gọi hook khi nhận sát thương
        OnTakeDamageHook(damage);

        // Kiểm tra điều kiện tử trận
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// CÂU 4: Kích hoạt hiệu ứng làm mờ màn hình trong khoảng thời gian xác định (mặc định 1s).
    /// </summary>
    public void TriggerScreenBlur(float duration)
    {
        if (screenBlurImage != null)
        {
            if (blurCoroutine != null) StopCoroutine(blurCoroutine);
            blurCoroutine = StartCoroutine(ScreenBlurRoutine(duration));
        }
    }

    private System.Collections.IEnumerator ScreenBlurRoutine(float duration)
    {
        screenBlurImage.gameObject.SetActive(true);
        Color baseColor = screenBlurImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Độ mờ đạt đỉnh 0.8f rồi dịu dần về 0 khi hết 1s
            baseColor.a = Mathf.Sin(t * Mathf.PI) * 0.8f;
            screenBlurImage.color = baseColor;
            yield return null;
        }

        baseColor.a = 0f;
        screenBlurImage.color = baseColor;
        blurCoroutine = null;
    }

    /// <summary>
    /// CÂU 1: Xử lý nhân vật khi hết máu và tử trận (DIE).
    /// Chuyển cảnh sang góc nhìn của nhân vật bằng Cinemachine Camera.
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red>[PlayerController] Người chơi đã tử trận (DIE)!</color>");

        // Tước quyền di chuyển theo quy tắc phế võ công AGENTS.md
        _dir = Vector2.zero;
        _move = Vector3.zero;
        if (characterController != null) characterController.enabled = false;

        // Xóa sạch chớp đỏ sát thương để không kẹt màn hình đỏ lòm khi chết
        if (damageFlashImage != null)
        {
            Color c = damageFlashImage.color;
            c.a = 0f;
            damageFlashImage.color = c;
        }

        // CÂU 1: Chuyển sang góc nhìn của nhân vật bằng Cinemachine Camera
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SetCameraMode(deathCameraMode);
            Debug.Log($"[CameraManager] Đã chuyển Cinemachine Camera sang góc nhìn: {deathCameraMode}");
        }

        // Báo hiệu kết thúc trận cho GameMaster
        if (GameMaster.Instance != null)
        {
            GameMaster.Instance.TriggerGameOver();
        }

        // Kích hoạt ngay Popup Game Over chuẩn AAA
        var popup = FindAnyObjectByType<GameOverPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.ShowGameOverPopup();
        }

        if (_anim != null)
        {
            _anim.SetTrigger("isDie");
        }

        // EXAM HOOK: Kích hoạt animation chết hoặc hiệu ứng giải thể nhân vật
        OnDieHook();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Làm mờ dần hiệu ứng chớp đỏ theo thời gian.
    /// </summary>
    private void HandleDamageFlashFade()
    {
        if (damageFlashImage != null && damageFlashImage.color.a > 0f)
        {
            Color c = damageFlashImage.color;
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * flashFadeSpeed);
            damageFlashImage.color = c;
        }
    }
    #endregion

    #region EXAM HOOKS - ĐIỂM MỞ RỘNG CHO ĐỀ THI GAM301
    /// <summary>
    /// Hook gọi trong lúc di chuyển.
    /// </summary>
    protected virtual void OnMovementHook(Vector3 moveVector)
    {
        // VÍ DỤ: Cập nhật biến Speed vào Animator của Model nhân vật
    }

    /// <summary>
    /// Hook gọi mỗi khi thực hiện bắn đạn thành công.
    /// </summary>
    protected virtual void OnShootHook()
    {
        // Kích hoạt âm thanh bắn súng
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShoot();
        }
    }

    /// <summary>
    /// Hook gọi khi nhận sát thương.
    /// </summary>
    protected virtual void OnTakeDamageHook(float damageAmount)
    {
        // Kích hoạt âm thanh dính sát thương
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHit();
        }
    }

    /// <summary>
    /// Hook gọi khi người chơi tử trận.
    /// </summary>
    protected virtual void OnDieHook()
    {
        // Kích hoạt âm thanh tử trận
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }
    }
    #endregion

    #region FPS CROSSHAIR GUI
    private void OnGUI()
    {
        bool isFirstPerson = CameraManager.Instance != null && CameraManager.Instance.CurrentMode == CameraManager.CameraViewMode.FirstPerson;
        if (!isFirstPerson || !showFpsCrosshair) return;

        // Vẽ hồng tâm chuẩn FPS ở chính giữa màn hình
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float size = 10f;
        float thickness = 2f;

        Color crosshairColor = new Color(0f, 1f, 1f, 0.85f); // Cyan neon sắc nét

        // Thanh ngang
        DrawGuiRect(new Rect(centerX - size, centerY - thickness * 0.5f, size * 2f, thickness), crosshairColor);
        // Thanh dọc
        DrawGuiRect(new Rect(centerX - thickness * 0.5f, centerY - size, thickness, size * 2f), crosshairColor);
        // Chấm tâm
        DrawGuiRect(new Rect(centerX - 1.5f, centerY - 1.5f, 3f, 3f), Color.white);
    }

    private static Texture2D _guiTexture;
    private static void DrawGuiRect(Rect position, Color color)
    {
        if (_guiTexture == null)
        {
            _guiTexture = new Texture2D(1, 1);
            _guiTexture.SetPixel(0, 0, Color.white);
            _guiTexture.Apply();
        }
        GUI.color = color;
        GUI.DrawTexture(position, _guiTexture);
        GUI.color = Color.white;
    }
    #endregion
}
