using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;

public class Movement : MonoBehaviour
{
    [Header("Odblokowane Moduły Robota")]
    public bool hasDashModule = false;

    [Header("Checkpointy i Odradzanie (Respawn)")]
    [Tooltip("Poniżej jakiej wysokości Y robocik uznaje, że spadł w przepaść")]
    public float fallLimitY = -15.0f;
    private Vector3 _respawnPoint;

    [Header("Kamera TPP i Obrót")]
    public Transform cameraTransform;
    public float rotationSpeed = 15.0f;

    [Header("Efekt FOV podczas Dasha (Cinemachine)")]
    public CinemachineCamera freeLookCamera;
    public float normalFOV = 60.0f;
    public float dashFOV = 75.0f;
    public float fovChangeSpeed = 12.0f;

    [Header("Efekty Wizualne (Smugi i Silniczki)")]
    [Tooltip("Przeciągnij obiekt DashTrail (TrailRenderer)")]
    public TrailRenderer dashTrail;
    [Tooltip("Przeciągnij obiekt JumpThrusters (ParticleSystem)")]
    public ParticleSystem jumpThrusterParticles;
    [Tooltip("Prefab fali uderzeniowej (Sonic Boom) przy drugim skoku")]
    public GameObject doubleJumpBoomPrefab;
    [Tooltip("Kąt obrotu efektu Double Jumpa (90 lub -90 na osi X kładzie efekt płasko pod stopami)")]
    public Vector3 doubleJumpVfxRotation = new Vector3(90f, 0f, 0f);
    [Tooltip("Prefab dla uderzenia Ground Poundem o ziemię")]
    public GameObject groundPoundImpactPrefab;

    [Header("Animacje Robota")]
    [Tooltip("Przeciągnij tutaj obiekt modelu robota posiadający komponent Animator")]
    public Animator animator;
    private CapsuleCollider _capsuleCollider;
    private float _startColliderHeight;

    [Header("Prędkości ruchu")]
    public float walkVelocity = 6.0f;
    public float sprintVelocity = 10.0f;
    public float crouchVelocity = 3.0f;
    public float jumpForce = 8.0f;

    [Header("Moduł Double Jump (Silniki Odrzutowe)")]
    public bool hasDoubleJumpModule = false;
    public float doubleJumpForce = 8.5f;
    private bool _canDoubleJump;
    private bool _doubleJumping;
    [Tooltip("Czas odnowienia Podwójnego Skoku w sekundach")]
    public float doubleJumpCooldown = 1.2f;
    private float _doubleJumpCooldownTimer;

    [Header("Wydłużony skok przy trzymaniu spacji")]
    [Tooltip("Przez ile sekund trzymanie spacji dodatkowo wynosi robota w górę")]
    public float maxJumpHoldTime = 0.3f;
    [Tooltip("Siła ciągu silniczków podczas trzymania spacji")]
    public float jumpHoldForce = 22.0f;
    private float _jumpHoldTimer;

    [Header("Zaawansowana Fizyka Skoku")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;
    public float coyoteTime = 0.15f;
    private float _coyoteTimeCounter;
    private bool _isJumpButtonHeld;

    [Header("Kontrola w powietrzu (Platformówka)")]
    public float airControl = 5.0f;

    [Header("Dynamiczny Dash")]
    public float dashVelocity = 20.0f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.6f;

    [Header("Moduł Ground Pound (Ciężki Żyroskop)")]
    public bool hasGroundPoundModule = false;
    [Tooltip("Czas zawieszenia w powietrzu przed uderzeniem w dół")]
    public float groundPoundHangTime = 0.15f;
    [Tooltip("Prędkość uderzenia w dół")]
    public float groundPoundCooldown = 1.5f;
    private float _groundPoundCooldownTimer;
    public float groundPoundSpeed = 25.0f;

    private bool _isGroundPounding;
    private float _groundPoundHangTimer;
    private float _groundPoundLandTimer;

    // Właściwości odczytywane przez niszczalne obiekty i pancerne przyciski
    private float _dashGraceTimer;

    // Zwraca true podczas Dasha ORAZ przez 0.15s po jego zakończeniu (bufor zderzenia)
    public bool IsDashing => _isDashing || _dashGraceTimer > 0f;
    public bool IsGroundPounding => _isGroundPounding;
    

    [Header("Kucanie i skradanie")]
    public float crouchYScale = 0.5f;
    public float ceilingCheckDistance = 1.5f;
    private float _startYScale;

    [Header("Wykrywanie podłoża (Ground Check)")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody _rigidbody;
    private Vector2 _moveInput;
    private bool _jumping;
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _wantsToCrouch;
    private bool _isCrouching;

    private MovingPlatform _currentPlatform;

    // Zmienne stanu dla Dasha
    private bool _wantsToDash;
    private bool _isDashing;
    private bool _canDash = true;
    private float _dashTimeLeft;
    private float _dashCooldownTimer;
    private Vector3 _dashDirection;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _startYScale = transform.localScale.y;

        // Domyślnym punktem odrodzenia jest miejsce, w którym robocik zaczyna grę
        _respawnPoint = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (freeLookCamera != null)
        {
            freeLookCamera.Lens.FieldOfView = normalFOV;
        }

        if (dashTrail != null) dashTrail.emitting = false;
        if (jumpThrusterParticles != null) jumpThrusterParticles.Stop();


        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        _capsuleCollider = GetComponent<CapsuleCollider>();
        if (_capsuleCollider != null)
        {
            _startColliderHeight = _capsuleCollider.height;
        }
    }

    private void Update()
    {
        HandleCameraFOV();
        HandleVisualEffects();
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Obliczamy poziomą prędkość gracza (na osiach X i Z) na podstawie wciśniętych klawiszy
        float currentSpeed = 0f;
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            currentSpeed = _isCrouching ? crouchVelocity : (_isSprinting ? sprintVelocity : walkVelocity);
        }

        // Płynnie przekazujemy wartości do parametrów w oknie Animator
        animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", _isGrounded);
        animator.SetBool("IsCrouching", _isCrouching);
    }

    private void FixedUpdate()
    {
        // 0. Sprawdzamy, czy robocik nie spadł w przepaść
        if (transform.position.y < fallLimitY)
        {
            Respawn();
            return;
        }
        // Jeśli to był Double Jump, tworzymy falę uderzeniową pod stopami robota
        if (_doubleJumping && doubleJumpBoomPrefab != null)
        {
            Vector3 spawnPos = groundCheck != null ? groundCheck.position : transform.position;
            GameObject vfx = Instantiate(
                doubleJumpBoomPrefab,
                spawnPos,
                Quaternion.Euler(doubleJumpVfxRotation) // <-- Obrót o 90 stopni w osi X
            );
            Destroy(vfx, 2.0f);
        }

        // 1. Sprawdzamy podłoże ORAZ czy stoimy na ruchomej platformie
        CheckGroundAndPlatform();

        if (_isGrounded)
        {
            _coyoteTimeCounter = coyoteTime;
            _canDoubleJump = true;
        }
        else
        {
            _coyoteTimeCounter -= Time.fixedDeltaTime;
        }


        // 2. Odliczanie Cooldownów wszystkich umiejętności i aktualizacja HUD
        HandleAbilityCooldowns();

        if (_isGrounded && !_isDashing && _dashCooldownTimer <= 0f)
        {
            _canDash = true;
        }

        // 3. Rozpoczęcie Dasha
        if (_wantsToDash)
        {
            StartDash();
            _wantsToDash = false;
        }

        // 4. Obsługa trwającego Dasha
        if (_isDashing)
        {
            _dashGraceTimer = 0.15f;
            _rigidbody.linearVelocity = _dashDirection * dashVelocity;
            _dashTimeLeft -= Time.fixedDeltaTime;

            if (_dashTimeLeft <= 0f)
            {
                _isDashing = false;
            }

            return;
        }
        else if (_dashGraceTimer > 0f)
        {
            _dashGraceTimer -= Time.fixedDeltaTime;
        }

        if (_isGroundPounding)
        {
            if (_groundPoundHangTimer > 0f)
            {
                // Chwilowe zatrzymanie w powietrzu przed uderzeniem
                _rigidbody.linearVelocity = Vector3.zero;
                _groundPoundHangTimer -= Time.fixedDeltaTime;
            }
            else
            {
                // Gwałtowne uderzenie pionowo w dół
                _rigidbody.linearVelocity = Vector3.down * groundPoundSpeed;

                // Kończymy Ground Pound chwilę po dotknięciu ziemi (bufor gwarantuje wykrycie kolizji z podłogą)
                if (_isGrounded)
                {
                    _groundPoundLandTimer -= Time.fixedDeltaTime;
                    if (_groundPoundLandTimer <= 0f)
                    {
                        _isGroundPounding = false;

                        // Wybuch energii / pęknięcie ziemi w miejscu lądowania!
                        if (groundPoundImpactPrefab != null)
                        {
                            Vector3 impactPos = groundCheck != null ? groundCheck.position : transform.position;
                            GameObject impactVfx = Instantiate(groundPoundImpactPrefab, impactPos, Quaternion.identity);
                            Destroy(impactVfx, 2.0f);
                        }
                    }
                }
            }
            return;
        }


        // 6. Kierunek ruchu względem kamery
        Vector3 moveDirection = GetCameraRelativeDirection();

        // 7. Obracanie postaci
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        // 8. Wybór prędkości
        float currentSpeed = walkVelocity;
        if (_isCrouching)
        {
            currentSpeed = crouchVelocity;
        }
        else if (_isSprinting)
        {
            currentSpeed = sprintVelocity;
        }

        Vector3 targetHorizontalVelocity = moveDirection * currentSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

        Vector3 platformVel = _currentPlatform != null ? _currentPlatform.CurrentVelocity : Vector3.zero;

        // 9. Ruch na ziemi vs Ruch w powietrzu
        if (_isGrounded)
        {
            _rigidbody.linearVelocity = new Vector3(
                targetHorizontalVelocity.x + platformVel.x,
                _rigidbody.linearVelocity.y,
                targetHorizontalVelocity.z + platformVel.z
            );
        }
        else
        {
            Vector3 newHorizontalVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                airControl * Time.fixedDeltaTime
            );
            _rigidbody.linearVelocity = new Vector3(newHorizontalVelocity.x, _rigidbody.linearVelocity.y, newHorizontalVelocity.z);
        }

        // 10. Wykonanie skoku
        if (_jumping || _doubleJumping)
        {
            float currentJumpForce = _doubleJumping ? doubleJumpForce : jumpForce;

            // Zerujemy prędkość pionową (Y), aby drugi skok w powietrzu wybił pełną mocą nawet podczas spadania
            _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            _rigidbody.AddForce(Vector3.up * currentJumpForce, ForceMode.Impulse);

            // Uruchamiamy licznik dodatkowego ciągu przy trzymaniu spacji
            _jumpHoldTimer = maxJumpHoldTime;
            _coyoteTimeCounter = 0f;

            if (animator != null) animator.SetTrigger("Jump");

            _jumping = false;
            _doubleJumping = false;
        }

        // 10b. Dodatkowe wynoszenie w górę podczas trzymania spacji
        if (_isJumpButtonHeld && _jumpHoldTimer > 0f)
        {
            _rigidbody.AddForce(Vector3.up * jumpHoldForce, ForceMode.Acceleration);
            _jumpHoldTimer -= Time.fixedDeltaTime;
        }

        // 11. Lepsza grawitacja opadania
        ApplyBetterJumpGravity();
    }

    private void HandleAbilityCooldowns()
    {
        // 1. Cooldown Dasha
        if (hasDashModule)
        {
            if (_dashCooldownTimer > 0f)
                _dashCooldownTimer -= Time.fixedDeltaTime;

            float progress = dashCooldown > 0f ? 1f - (_dashCooldownTimer / dashCooldown) : 1f;
            if (ModuleHUD.Instance != null)
                ModuleHUD.Instance.UpdateCooldownUI(ModulePickup.ModuleType.Dash, progress);
        }

        // 2. Cooldown Ground Pounda
        if (hasGroundPoundModule)
        {
            if (_groundPoundCooldownTimer > 0f)
                _groundPoundCooldownTimer -= Time.fixedDeltaTime;

            float progress = groundPoundCooldown > 0f ? 1f - (_groundPoundCooldownTimer / groundPoundCooldown) : 1f;
            if (ModuleHUD.Instance != null)
                ModuleHUD.Instance.UpdateCooldownUI(ModulePickup.ModuleType.GroundPound, progress);
        }

        // 3. Cooldown Double Jumpa
        if (hasDoubleJumpModule)
        {
            if (_doubleJumpCooldownTimer > 0f)
                _doubleJumpCooldownTimer -= Time.fixedDeltaTime;

            float progress = doubleJumpCooldown > 0f ? 1f - (_doubleJumpCooldownTimer / doubleJumpCooldown) : 1f;
            if (ModuleHUD.Instance != null)
                ModuleHUD.Instance.UpdateCooldownUI(ModulePickup.ModuleType.DoubleJump, progress);
        }
    }

    private void HandleVisualEffects()
    {
        // 1. Smuga podczas Dasha
        if (dashTrail != null)
        {
            dashTrail.emitting = _isDashing;
        }

        // 2. Silniczki robota podczas dłuższego skoku (gdy trzymamy spację i lecimy w górę)
        if (jumpThrusterParticles != null)
        {
            bool shouldFireThrusters = !_isGrounded && _isJumpButtonHeld && _rigidbody.linearVelocity.y > 0.5f;

            if (shouldFireThrusters && !jumpThrusterParticles.isPlaying)
            {
                jumpThrusterParticles.Play();
            }
            else if (!shouldFireThrusters && jumpThrusterParticles.isPlaying)
            {
                jumpThrusterParticles.Stop();
            }
        }
    }


    // Zapisuje nową Stację Dokującą (Checkpoint)
    public void SetCheckpoint(Vector3 newPoint)
    {
        _respawnPoint = newPoint;
    }

    // Teleportuje robocika do ostatniego Checkpointu i zeruje pęd
    public void Respawn()
    {
        _isDashing = false;
        _jumping = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.position = _respawnPoint;
        transform.position = _respawnPoint;
    }

    private void CheckGroundAndPlatform()
    {
        _currentPlatform = null;
        if (groundCheck == null) return;

        Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundDistance, groundLayer);
        _isGrounded = colliders.Length > 0;

        foreach (Collider col in colliders)
        {
            MovingPlatform platform = col.GetComponent<MovingPlatform>();
            if (platform != null)
            {
                _currentPlatform = platform;
                break;
            }
        }
    }

    private void ApplyBetterJumpGravity()
    {
        if (_rigidbody.linearVelocity.y < 0f)
        {
            _rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (_rigidbody.linearVelocity.y > 0f && !_isJumpButtonHeld)
        {
            _rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    public void UnlockDashModule()
    {
        hasDashModule = true;
        if (ModuleHUD.Instance != null)
            ModuleHUD.Instance.UnlockModuleUI(ModulePickup.ModuleType.Dash);
    }

    private void HandleCameraFOV()
    {
        if (freeLookCamera == null) return;

        float targetFOV = _isDashing ? dashFOV : normalFOV;
        freeLookCamera.Lens.FieldOfView = Mathf.Lerp(
            freeLookCamera.Lens.FieldOfView,
            targetFOV,
            fovChangeSpeed * Time.deltaTime
        );
    }

    private Vector3 GetCameraRelativeDirection()
    {
        if (cameraTransform == null)
        {
            return new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * _moveInput.y + camRight * _moveInput.x).normalized;
    }

    private void StartDash()
    {
        _isDashing = true;
        _canDash = false;
        _dashTimeLeft = dashDuration;
        _dashCooldownTimer = dashCooldown;

        Vector3 moveDir = GetCameraRelativeDirection();
        _dashDirection = moveDir.sqrMagnitude > 0.01f ? moveDir : transform.forward;

        if (_dashDirection.sqrMagnitude > 0.01f)
        {
            _rigidbody.MoveRotation(Quaternion.LookRotation(_dashDirection));
        }
        if (animator != null) animator.SetTrigger("Dash");
    }



    public void OnJump(InputAction.CallbackContext context)
    {
        _isJumpButtonHeld = context.ReadValueAsButton();

        // Gdy gracz puści spację, natychmiast przerywamy dodatkowe wznoszenie
        if (context.canceled)
        {
            _jumpHoldTimer = 0f;
        }

        if (context.performed && !_isCrouching && !_isGroundPounding)
        {
            // 1. Pierwszy skok z ziemi (lub w trakcie Coyote Time)
            if (_coyoteTimeCounter > 0f)
            {
                _jumping = true;
            }
            // 2. Drugi skok w powietrzu (tylko jeśli mamy odblokowany moduł)
            
            else if (hasDoubleJumpModule && _canDoubleJump && _doubleJumpCooldownTimer <= 0f)
            {
                _doubleJumping = true;
                _canDoubleJump = false;
                _doubleJumpCooldownTimer = doubleJumpCooldown;
            }
        }
    }

    public void UnlockDoubleJumpModule()
    {
        hasDoubleJumpModule = true;
        if (ModuleHUD.Instance != null)
            ModuleHUD.Instance.UnlockModuleUI(ModulePickup.ModuleType.DoubleJump);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.ReadValueAsButton();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed && !_isGrounded && hasGroundPoundModule && _groundPoundCooldownTimer <= 0f && !_isGroundPounding && !_isDashing)
        {
            StartGroundPound();
            return;
        }

        _wantsToCrouch = context.ReadValueAsButton();
    }
    private void StartGroundPound()
    {
        _isGroundPounding = true;
        _groundPoundHangTimer = groundPoundHangTime;
        _groundPoundLandTimer = 0.05f;
        _groundPoundCooldownTimer = groundPoundCooldown; 
        _rigidbody.linearVelocity = Vector3.zero;
    }
    public void Bounce(float force)
    {
        // 1. Natychmiast wyłączamy Ground Pound, aby nie ściągał gracza z powrotem w dół
        _isGroundPounding = false;
        _canDoubleJump = true; // Opcjonalnie: odnawia możliwość użycia Double Jumpa po odbiciu!

        // 2. Zerujemy prędkość spadania i wybijamy robota w górę
        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(Vector3.up * force, ForceMode.Impulse);

        // 3. Uruchamiamy animację skoku oraz efekt uderzenia o ziemię
        if (animator != null) animator.SetTrigger("Jump");

        if (groundPoundImpactPrefab != null)
        {
            Vector3 impactPos = groundCheck != null ? groundCheck.position : transform.position;
            GameObject impactVfx = Instantiate(groundPoundImpactPrefab, impactPos, Quaternion.identity);
            Destroy(impactVfx, 2.0f);
        }
    }

    public void UnlockGroundPoundModule()
    {
        hasGroundPoundModule = true;
        if (ModuleHUD.Instance != null)
            ModuleHUD.Instance.UnlockModuleUI(ModulePickup.ModuleType.GroundPound);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (hasDashModule && context.performed && _canDash && !_isDashing && !_isCrouching)
        {
            _wantsToDash = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * ceilingCheckDistance);
    }
}