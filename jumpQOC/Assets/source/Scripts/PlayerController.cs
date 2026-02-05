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

    [Header("4. 카메라")]
    public Camera mainCamera; 

    [Header("5. 애니메이션")]
    public Animator anim; 

    [Header("6. 물리/마찰력 설정")]
    public float groundDecel = 50f;
    public float iceDecel = 0.5f;
    public float iceAcceleration = 5f;
    public float noControlDecel = 0.05f;

    [Header("7. 특수 효과 연결")]
    public ParticleSystem snowParticle;
    public TypewriterUI typewriterUI;

    [Header("8. 넉백 설정")]
    public float knockbackDuration = 0.2f; 

    [Header("9. 경사로 설정")]
    public float slopeThreshold = 10f;

    [Header("10. 주기적 빙결 함정")]
    public float freezeDuration = 2.0f;     
    public float freeTime = 3.0f;           
    public Color freezeColor = new Color(0.5f, 0.8f, 1f, 1f); 
    [Range(0f, 1f)] public float realFreezeChance = 0.5f; 

    [Header("11. 오디오 설정")]
    public AudioSource bgmAudioSource; 
    public AudioSource sfxAudioSource; 
    [Range(0f, 1f)] public float bgmVolume = 0.5f; 
    [Range(0f, 1f)] public float sfxVolume = 1.0f; 
    public float bgmFadeDuration = 1.0f;
    public float landSoundThreshold = 20f; // 에러 해결용 변수 추가

    [Space(10)]
    public AudioClip area1Bgm;  
    public AudioClip jumpSfx;   
    public AudioClip landSfx;   
    public AudioClip freezeSfx; 

    // 내부 변수
    private float currentFriction; 
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool isOnIce; 
    private bool isOnNoControlIce; 
    private float chargeTime;
    private bool isCharging;
    private Vector3 originalScale;
    private float jumpCooldown = 0f;
    private bool isFrozen = false;          
    private bool isFreezeTrapActive = false; 
    private Coroutine freezeCoroutine; 
    private bool isFadingOut = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        currentFriction = groundDecel;

        if (mainCamera == null) mainCamera = Camera.main; 
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
        
        if (bgmAudioSource != null) bgmAudioSource.volume = bgmVolume;

        PhysicsMaterial2D slipperyMaterial = new PhysicsMaterial2D("CodeSlippery");
        slipperyMaterial.friction = 0f;     
        slipperyMaterial.bounciness = 0f;   
        rb.sharedMaterial = slipperyMaterial;
        if(col != null) col.sharedMaterial = slipperyMaterial;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log("A키 인식 성공!");
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Debug.Log("왼쪽 화살표 인식 성공!");

        if (bgmAudioSource != null && !isFadingOut) bgmAudioSource.volume = bgmVolume;

        if (jumpCooldown > 0) { jumpCooldown -= Time.deltaTime; isGrounded = false; }
        else
        {
            Bounds bounds = col.bounds;
            Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.1f);
            RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, bounds.extents.y + 0.1f, groundLayer);
            isGrounded = hit.collider != null;

            if (isGrounded)
            {
                string tag = hit.collider.tag;
                if (tag == "NoControlIce") { isOnNoControlIce = true; isOnIce = true; currentFriction = noControlDecel; }
                else if (tag == "Ice" || tag == "FreezeTrigger") { isOnNoControlIce = false; isOnIce = true; currentFriction = iceDecel; }
                else 
                { 
                    isOnNoControlIce = false;
                    float groundAngle = Vector2.Angle(hit.normal, Vector2.up);
                    if (groundAngle > slopeThreshold) { isOnIce = true; currentFriction = iceDecel; }
                    else { isOnIce = false; currentFriction = groundDecel; }
                }
            }
            else { isOnNoControlIce = false; isOnIce = false; currentFriction = 2f; }
        }

        if (anim != null)
        {
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            if (!isGrounded || isMoving) anim.SetBool("isJumping", true);
            else anim.SetBool("isJumping", false);
        }
        HandleJumpInput();
    }

 void FixedUpdate()
{
    if (jumpCooldown > 0) return;
    if (isFrozen) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }

    // 1. WASD 입력 받기
    float xInput = Input.GetAxisRaw("Horizontal");

    // 2. 이동 제한 조건 수정 (핵심!)
    if (isOnNoControlIce) 
    {
        xInput = 0f; 
    }
    else if (isCharging && !isOnIce) 
    {
        // 땅 위에서 기 모을 때만 이동을 막음. 
        // 만약 기 모으는 중에도 움직이고 싶다면 이 else if 블록을 아예 지우세요.
        xInput = 0f; 
    }

    // 3. 이동 속도 계산 (Math -> Mathf로 수정)
    float targetSpeed = xInput * moveSpeed;
    float changeRate;

    if (isGrounded)
    {
        if (isOnNoControlIce) changeRate = noControlDecel;
        // Math.Abs를 Mathf.Abs로 수정하여 에러 해결
        else if (Mathf.Abs(targetSpeed) > 0.01f) changeRate = isOnIce ? iceAcceleration : groundDecel;
        else changeRate = currentFriction;
    }
    else 
    {
        changeRate = 5f; 
    }

    // 4. 물리 적용
    float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, changeRate * Time.fixedDeltaTime);
    rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

    // 5. 방향 전환
    if (xInput != 0 && !isCharging && !isOnNoControlIce)
    {
        float direction = xInput > 0 ? 1 : -1;
        transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * direction, originalScale.y, originalScale.z);
    }
}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.y > landSoundThreshold) 
        {
            if (((1 << collision.gameObject.layer) & groundLayer) != 0) PlaySfx(landSfx);
        }

        bool isSteppingOn = collision.GetContact(0).normal.y > 0.5f;
        string tag = collision.gameObject.tag;

        if (isSteppingOn)
        {
            if (tag == "FreezeTrigger")
            {
                if (snowParticle != null && !snowParticle.isPlaying) snowParticle.Play();
                if (bgmAudioSource != null && area1Bgm != null)
                {
                    if (!bgmAudioSource.isPlaying || bgmAudioSource.clip != area1Bgm)
                    {
                        isFadingOut = false;
                        bgmAudioSource.volume = bgmVolume;
                        bgmAudioSource.clip = area1Bgm; 
                        bgmAudioSource.loop = true; 
                        bgmAudioSource.Play();
                    }
                }
                if (!isFreezeTrapActive) { isFreezeTrapActive = true; freezeCoroutine = StartCoroutine(PeriodicFreezeRoutine()); }
            }
            else if (tag == "Zone2") StopZone1Effects();
            else if (tag == "StoryBlock") if (typewriterUI != null) typewriterUI.ShowMessage("이곳은 미끄럽지 않아");
        }
    }

    void StopZone1Effects()
    {
        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        isFreezeTrapActive = false; isFrozen = false;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
        if (snowParticle != null) snowParticle.Stop();
        if (bgmAudioSource != null && bgmAudioSource.isPlaying) StartCoroutine(FadeOutMusic());
        if (typewriterUI != null) typewriterUI.ShowMessage("");
    }

    IEnumerator FadeOutMusic()
    {
        isFadingOut = true; 
        float startVolume = bgmAudioSource.volume;
        float timer = 0f;
        while (timer < bgmFadeDuration)
        {
            timer += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeDuration);
            yield return null;
        }
        bgmAudioSource.Stop();
        bgmAudioSource.volume = bgmVolume; 
        isFadingOut = false;
    }

    IEnumerator PeriodicFreezeRoutine()
    {
        while (true) 
        {
            if (typewriterUI != null) typewriterUI.ShowMessage(""); 
            yield return new WaitForSeconds(freeTime);
            if (typewriterUI != null) typewriterUI.ShowMessage("3");
            yield return new WaitForSeconds(1.0f);
            if (typewriterUI != null) typewriterUI.ShowMessage("2");
            yield return new WaitForSeconds(1.0f);
            if (typewriterUI != null) typewriterUI.ShowMessage("1");
            yield return new WaitForSeconds(1.0f);

            if (Random.value < realFreezeChance)
            {
                if (typewriterUI != null) typewriterUI.ShowMessage("!얼음"); 
                PlaySfx(freezeSfx); 
                isFrozen = true; isCharging = false;
                if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
                if (spriteRenderer != null) spriteRenderer.color = freezeColor; 
                yield return new WaitForSeconds(freezeDuration); 
                isFrozen = false;
                if (spriteRenderer != null) spriteRenderer.color = Color.white; 
            }
            else yield return new WaitForSeconds(freezeDuration);
        }
    }

    public void ApplyKnockback(Vector2 direction, float force) { rb.AddForce(direction.normalized * force, ForceMode2D.Impulse); }

    void PlaySfx(AudioClip clip) { if (sfxAudioSource != null && clip != null) sfxAudioSource.PlayOneShot(clip); }

    void HandleJumpInput() 
    { 
        if (mainCamera == null || jumpCooldown > 0 || isOnNoControlIce || isFrozen) return;
        if (Input.GetMouseButtonDown(0) && isGrounded) { isCharging = true; chargeTime = 0f; if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(true); } 
        if (isCharging && Input.GetMouseButton(0)) { chargeTime += Time.deltaTime; RotateArrow(); UpdateArrowVisual(); } 
        if (isCharging && Input.GetMouseButtonUp(0)) { Jump(); isCharging = false; if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false); } 
    }
    
    void RotateArrow() { Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0; Vector2 direction = (mousePos - transform.position); float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + (Mathf.Abs(transform.eulerAngles.y) > 90f ? 180 : 0) + rotationOffset); }
    void UpdateArrowVisual() { float ratio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; float targetScaleX = 1f + (ratio * 2f); arrowIndicator.localScale = new Vector3(transform.localScale.x < 0 ? -targetScaleX : targetScaleX, 1f, 1f); if (arrowSprite != null) arrowSprite.color = Color.Lerp(Color.yellow, Color.red, ratio); }
    void Jump() { PlaySfx(jumpSfx); Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0; Vector2 direction = (mousePos - transform.position).normalized; float powerRatio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; float finalPower = Mathf.Max(powerRatio * maxJumpPower, 5f); jumpCooldown = 0.2f; isGrounded = false; rb.linearVelocity = direction * finalPower; }
}