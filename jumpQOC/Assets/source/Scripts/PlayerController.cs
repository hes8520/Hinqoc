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
    public float bgmFadeDuration = 1.0f; // [추가됨] 브금 꺼지는 시간 (1초)

    [Space(10)]
    public AudioClip area1Bgm;  
    public AudioClip jumpSfx;   
    public AudioClip landSfx;   
    public AudioClip freezeSfx; 
    
    public float landSoundThreshold = 20f; 

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
    private bool isFadingOut = false; // [추가됨] 지금 소리가 줄어드는 중인가?

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (anim == null) anim = GetComponent<Animator>();
        originalScale = transform.localScale;
        currentFriction = groundDecel;

        if (mainCamera == null) mainCamera = Camera.main; 
        if (mainCamera == null) { mainCamera = FindFirstObjectByType<Camera>(); if (mainCamera == null) mainCamera = FindObjectOfType<Camera>(); }
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
        if (snowParticle != null && snowParticle.isPlaying) snowParticle.Stop();
        
        if (bgmAudioSource != null) bgmAudioSource.volume = bgmVolume;

        PhysicsMaterial2D slipperyMaterial = new PhysicsMaterial2D("CodeSlippery");
        slipperyMaterial.friction = 0f;     
        slipperyMaterial.bounciness = 0f;   
        rb.sharedMaterial = slipperyMaterial;
        if(col != null) col.sharedMaterial = slipperyMaterial;
    }

    void Update()
    {
        // [수정됨] 페이드 아웃 중이 아닐 때만 볼륨 슬라이더 값 적용
        if (bgmAudioSource != null && !isFadingOut) 
        {
            bgmAudioSource.volume = bgmVolume;
        }

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

        float xInput = 0f;
        if (isOnNoControlIce) xInput = 0f; 
        else if (isCharging) { if (isOnIce) xInput = 0f; else return; }
        else { xInput = Input.GetAxisRaw("Horizontal"); }

        if (isGrounded && xInput != 0 && !isCharging && !isOnNoControlIce)
        {
            if (Mathf.Abs(rb.linearVelocity.x) < 0.05f) 
            {
                rb.position += new Vector2(0, 0.05f);
            }
        }

        float targetSpeed = xInput * moveSpeed;
        float changeRate;
        if (isGrounded)
        {
            if (isOnNoControlIce) changeRate = noControlDecel;
            else if (Mathf.Abs(targetSpeed) > 0.01f) changeRate = isOnIce ? iceAcceleration : groundDecel;
            else changeRate = currentFriction;
        }
        else { changeRate = 5f; }

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, changeRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

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
                // [수정됨] 눈이 멈춰있었다면 다시 켬
                if (snowParticle != null && !snowParticle.isPlaying) snowParticle.Play();
                
                // 브금 켜기 (페이드 아웃 중이었다면 취소하고 다시 켜기)
                if (bgmAudioSource != null && area1Bgm != null)
                {
                    // 다른 브금이 나오고 있거나, 꺼져있다면
                    if (!bgmAudioSource.isPlaying || bgmAudioSource.clip != area1Bgm)
                    {
                        isFadingOut = false; // 혹시 페이드 아웃 중이었다면 중단
                        bgmAudioSource.volume = bgmVolume; // 볼륨 복구
                        bgmAudioSource.clip = area1Bgm; 
                        bgmAudioSource.loop = true; 
                        bgmAudioSource.Play();
                    }
                }

                if (!isFreezeTrapActive) 
                { 
                    isFreezeTrapActive = true; 
                    freezeCoroutine = StartCoroutine(PeriodicFreezeRoutine()); 
                }
            }
            // === 2구역 입구 ===
            else if (tag == "Zone2")
            {
                StopZone1Effects();
            }
            else if (tag == "StoryBlock")
            {
                if (typewriterUI != null) typewriterUI.ShowMessage("이곳은 미끄럽지 않아");
            }
        }
    }

    // [수정됨] 페이드 아웃 로직 적용
    void StopZone1Effects()
    {
        // 1. 함정 정지
        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        isFreezeTrapActive = false; 

        isFrozen = false;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
        
        // 2. 눈 파티클 끄기 (더 이상 생성되지 않음)
        if (snowParticle != null) snowParticle.Stop();

        // 3. 브금 페이드 아웃 시작
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }

        // 4. 텍스트 지우기
        if (typewriterUI != null) typewriterUI.ShowMessage("");
    }

    // [추가됨] 브금 서서히 끄기
    IEnumerator FadeOutMusic()
    {
        isFadingOut = true; // "지금 소리 줄이는 중이니까 건드리지 마!"
        
        float startVolume = bgmAudioSource.volume;
        float timer = 0f;

        // 지정된 시간(bgmFadeDuration) 동안 볼륨을 줄임
        while (timer < bgmFadeDuration)
        {
            timer += Time.deltaTime;
            // Lerp: 시작 볼륨에서 0까지 부드럽게 이동
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeDuration);
            yield return null; // 한 프레임 대기
        }

        bgmAudioSource.Stop();   // 완전히 끄기
        bgmAudioSource.volume = bgmVolume; // 다음 번을 위해 볼륨 원래대로 복구
        isFadingOut = false; // 페이드 아웃 종료
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
            else
            {
                if (typewriterUI != null) typewriterUI.ShowMessage("!얼리기"); 
                yield return new WaitForSeconds(freezeDuration);
            }
        }
    }

    public void ApplyKnockback(Vector2 direction, float force) { rb.AddForce(direction.normalized * force, ForceMode2D.Impulse); }

    void PlaySfx(AudioClip clip) { if (sfxAudioSource != null && clip != null) { sfxAudioSource.volume = sfxVolume; sfxAudioSource.PlayOneShot(clip); } }

    void HandleJumpInput() 
    { 
        if (mainCamera == null) return; if (jumpCooldown > 0) return; if (isOnNoControlIce) return; if (isFrozen) return;

        if (Input.GetMouseButtonDown(0) && isGrounded) 
        { 
            isCharging = true; chargeTime = 0f; 
            if (!isOnIce) { rb.linearVelocity = Vector2.zero; } 
            if (arrowIndicator != null) { arrowIndicator.gameObject.SetActive(true); arrowIndicator.localScale = new Vector3(1, 1, 1); } 
        } 

        if (isCharging && Input.GetMouseButton(0)) { chargeTime += Time.deltaTime; RotateArrow(); UpdateArrowVisual(); } 

        if (isCharging && Input.GetMouseButtonUp(0)) { Jump(); isCharging = false; if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false); } 
    }
    
    void RotateArrow() { if (arrowIndicator == null || mainCamera == null) return; Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0; Vector2 direction = (mousePos - transform.position); float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; bool isFlipped = Mathf.Abs(transform.eulerAngles.y) > 90f; if (isFlipped) arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + 180 + rotationOffset); else arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + rotationOffset); }
    void UpdateArrowVisual() { if (arrowIndicator == null) return; float ratio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; float targetScaleX = 1f + (ratio * 2f); if (transform.localScale.x < 0) arrowIndicator.localScale = new Vector3(-targetScaleX, 1f, 1f); else arrowIndicator.localScale = new Vector3(targetScaleX, 1f, 1f); if (arrowSprite != null) arrowSprite.color = Color.Lerp(Color.yellow, Color.red, ratio); }
    void Jump() { if (mainCamera == null) return; PlaySfx(jumpSfx); Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition); mousePos.z = 0; Vector2 direction = (mousePos - transform.position).normalized; float powerRatio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime; float finalPower = powerRatio * maxJumpPower; if (finalPower < 5f) finalPower = 5f; jumpCooldown = 0.2f; isGrounded = false; rb.linearVelocity = direction * finalPower; }
}