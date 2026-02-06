using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("1. 이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float maxJumpPower = 40f; 
    public float maxChargeTime = 1.0f;

    [Header("2. 화살표 연결")]
    public Transform arrowIndicator;   
    public SpriteRenderer arrowSprite; 
    public float rotationOffset = 0f;  

    [Header("3. 땅 체크")]
    public LayerMask groundLayer;      

    [Header("4. 카메라 및 기믹 매니저")]
    public Camera mainCamera; 
    public FrozenTrap zoneManager; 

    [Header("5. 애니메이션")]
    public Animator anim; 

    [Header("6. 물리 설정")]
    public float groundDecel = 50f;
    public float iceDecel = 0.5f;
    public float iceAcceleration = 5f;
    public float noControlDecel = 0.05f;

    [Header("7. 사운드 설정")]
    [Range(0f, 1f)] public float masterVolumeCap = 0.4f; // 전체 소리 제한
    [Range(0f, 1f)] public float bgmVolume = 0.5f; 
    [Range(0f, 1f)] public float sfxVolume = 1.0f; 
    
    public AudioSource sfxAudioSource; 
    public AudioClip jumpSfx;
    public AudioClip landSfx; // [중요] 착지 효과음 파일 연결 필수!
    public float landSoundThreshold = 10f; // [중요] 이 속도보다 빠르게 떨어져야 소리남 (숫자가 클수록 높은곳에서 떨어져야 함)

    [Header("8. 상태")]
    public bool isFrozen = false; 

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private bool wasGrounded; // [추가] 직전 프레임에 땅에 있었나?
    private float lastVelocityY; // [추가] 착지 직전의 낙하 속도 저장

    private bool isOnIce; 
    private bool isOnNoControlIce; 
    private float chargeTime;
    private bool isCharging;
    private Vector3 originalScale;
    private float jumpCooldown = 0f;
    private float currentFriction;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        
        if (mainCamera == null) mainCamera = Camera.main; 
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);

        PhysicsMaterial2D slipperyMaterial = new PhysicsMaterial2D("CodeSlippery") { friction = 0f, bounciness = 0f };
        rb.sharedMaterial = slipperyMaterial;
    }

    void Update()
    {
        if (jumpCooldown > 0) { jumpCooldown -= Time.deltaTime; isGrounded = false; }
        else
        {
            Bounds bounds = col.bounds;
            Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.1f);
            RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, bounds.extents.y + 0.1f, groundLayer);
            isGrounded = hit.collider != null;

            // [핵심 수정] 착지 순간 감지 로직
            if (isGrounded && !wasGrounded) 
            {
                // 방금 땅에 닿았음!
                // 떨어지는 속도(음수)가 임계값보다 컸다면 (즉, 세게 떨어졌다면)
                if (lastVelocityY < -landSoundThreshold)
                {
                    PlaySfx(landSfx);
                    // Debug.Log($"쾅! 착지 속도: {lastVelocityY}"); // 테스트용 로그
                }
            }

            if (isGrounded)
            {
                string footTag = hit.collider.tag;
                HandleFootstepTag(footTag);

                if (footTag == "NoControlIce") { isOnNoControlIce = true; isOnIce = true; currentFriction = noControlDecel; }
                else if (footTag == "Ice") { isOnNoControlIce = false; isOnIce = true; currentFriction = iceDecel; }
                else { isOnNoControlIce = false; isOnIce = false; currentFriction = groundDecel; }
            }

            // 상태 업데이트 (다음 프레임 비교용)
            wasGrounded = isGrounded;
        }

        if (anim != null) anim.SetBool("isJumping", !isGrounded || Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        if (!isFrozen) HandleJumpInput(); 
    }

    void HandleFootstepTag(string tag)
    {
        if (tag == "FreezeTrigger")
        {
            if (zoneManager) zoneManager.ActivateTrap(this);
        }
        else if (tag == "zone2")
        {
            if (zoneManager) zoneManager.DeactivateTrap();
        }
    }

    void FixedUpdate()
    {
        // [추가] 물리 연산 전에 현재 낙하 속도를 저장해둠 (Update에서 착지 판정에 씀)
        lastVelocityY = rb.linearVelocity.y;

        if (jumpCooldown > 0) return;
        if (isFrozen) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

        float xInput = Input.GetAxisRaw("Horizontal");
        if (isOnNoControlIce || (isCharging && !isOnIce)) xInput = 0f;

        float targetSpeed = xInput * moveSpeed;
        float changeRate = isGrounded ? (isOnNoControlIce ? noControlDecel : (Mathf.Abs(targetSpeed) > 0.01f ? (isOnIce ? iceAcceleration : groundDecel) : currentFriction)) : 5f;

        rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, changeRate * Time.fixedDeltaTime), rb.linearVelocity.y);

        if (xInput != 0 && !isCharging && !isOnNoControlIce)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * (xInput > 0 ? 1 : -1), originalScale.y, originalScale.z);
        }
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (isFrozen) return;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    public void PlaySfx(AudioClip clip) 
    { 
        if (sfxAudioSource != null && clip != null) 
            sfxAudioSource.PlayOneShot(clip, sfxVolume * masterVolumeCap); 
    }

    void HandleJumpInput() 
    { 
        if (mainCamera == null || jumpCooldown > 0 || isOnNoControlIce) return;
        if (Input.GetMouseButtonDown(0) && isGrounded) 
        { 
            isCharging = true; 
            chargeTime = 0f; 
            if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(true); 
        } 
        
        if (isCharging && Input.GetMouseButton(0)) 
        { 
            chargeTime += Time.deltaTime; 
            RotateArrow(); 
            UpdateArrowVisual(); 
        } 
        
        if (isCharging && Input.GetMouseButtonUp(0)) 
        { 
            Jump(); 
            isCharging = false; 
            if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false); 
        } 
    }
    
    void RotateArrow() 
    { 
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); 
        mousePos.z = 0; 
        Vector2 direction = (mousePos - transform.position); 
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; 
        arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + 180f + rotationOffset); 
    }

    void UpdateArrowVisual() 
    { 
        float ratio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; 
        float targetScaleX = 1f + (ratio * 2f); 
        arrowIndicator.localScale = new Vector3(transform.localScale.x < 0 ? -targetScaleX : targetScaleX, 1f, 1f); 
        if (arrowSprite != null) arrowSprite.color = Color.Lerp(Color.yellow, Color.red, ratio); 
    }

    void Jump() 
    { 
        PlaySfx(jumpSfx); 
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); 
        Vector2 direction = ((Vector2)mousePos - (Vector2)transform.position).normalized; 
        float powerRatio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; 
        float finalPower = Mathf.Max(powerRatio * maxJumpPower, 5f); 
        jumpCooldown = 0.2f; 
        isGrounded = false; 
        wasGrounded = false; // [추가] 점프했으니 공중 상태로 변경
        rb.linearVelocity = direction * finalPower; 
    }
}