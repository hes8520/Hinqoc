using UnityEngine;

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

    [Header("4. 카메라 (자동으로 잡힘)")]
    public Camera mainCamera; 

    [Header("5. 애니메이션 (추가됨)")]
    public Animator anim; // 인스펙터에서 확인 가능

    // 내부 변수
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private float chargeTime;
    private bool isCharging;
    private Vector3 originalScale;
    private float jumpCooldown = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // [추가됨] 애니메이터 컴포넌트 가져오기
        if (anim == null) anim = GetComponent<Animator>();

        originalScale = transform.localScale;

        // [핵심 수정] 카메라 찾기 3단 콤보
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>(); 
            if (mainCamera == null) mainCamera = FindObjectOfType<Camera>(); 
        }

        if (mainCamera == null)
        {
            Debug.LogError("비상!! 씬에 카메라 오브젝트가 아예 없습니다! Hierarchy 창을 확인하세요!");
        }

        if (arrowIndicator != null) 
            arrowIndicator.gameObject.SetActive(false);
    }

    void Update()
    {
        // 1. 땅 체크 로직 (기존과 동일)
        if (jumpCooldown > 0)
        {
            jumpCooldown -= Time.deltaTime;
        }
        else
        {
            Bounds bounds = col.bounds;
            Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.1f);
            RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, bounds.extents.y + 0.1f, groundLayer);
            
            isGrounded = hit.collider != null;
            Debug.DrawRay(bounds.center, Vector2.down * (bounds.extents.y + 0.1f), isGrounded ? Color.green : Color.red);
        }

        // [여기가 핵심 변경!] 2. 애니메이션 상태 업데이트
        if (anim != null)
        {
            // 옆으로 이동 중인지 확인 (속도의 절대값이 0.1보다 크면 움직이는 것)
            bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;

            // 조건: 땅에 없거나(점프중) OR 이동 중이면 -> 점프 애니메이션(isJumping) 켬
            if (!isGrounded || isMoving)
            {
                anim.SetBool("isJumping", true);
            }
            else
            {
                // 땅에 있고 가만히 멈춰있을 때만 -> 대기 애니메이션
                anim.SetBool("isJumping", false);
            }
        }

        HandleJumpInput();
    }

    void FixedUpdate()
    {
        if (jumpCooldown > 0 || isCharging) return;

        if (isGrounded) 
        {
            float xInput = Input.GetAxisRaw("Horizontal");
            rb.linearVelocity = new Vector2(xInput * moveSpeed, rb.linearVelocity.y);
            
            if (xInput != 0)
            {
                float direction = xInput > 0 ? 1 : -1;
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * direction, originalScale.y, originalScale.z);
            }
        }
    }

    void HandleJumpInput()
    {
        if (mainCamera == null) return; 

        if (jumpCooldown > 0) return;

        if (Input.GetMouseButtonDown(0) && isGrounded)
        {
            isCharging = true;
            chargeTime = 0f;
            rb.linearVelocity = Vector2.zero;
            
            if (arrowIndicator != null) 
            {
                arrowIndicator.gameObject.SetActive(true);
                arrowIndicator.localScale = new Vector3(1, 1, 1); 
            }
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
        if (arrowIndicator == null || mainCamera == null) return;
        
        // 1. 마우스 위치와 각도 계산 (월드 기준)
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector2 direction = (mousePos - transform.position);
        
        // 아크탄젠트로 각도 구하기
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // [핵심 해결] 플레이어가 Y축으로 180도 회전했는지 확인
        // (y 회전값이 90도보다 크면 뒤집힌 걸로 간주)
        bool isFlipped = Mathf.Abs(transform.eulerAngles.y) > 90f;

        if (isFlipped)
        {
            // 몸통이 뒤집혀 있다면, 화살표도 같이 뒤집혀서 반대로 보임.
            // 그래서 강제로 180도를 더 돌려서 정방향을 보게 만듦.
            // (Mathf.Atan2의 결과값은 그대로 두고, 180도 보정만 함)
            arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + 180 + rotationOffset);
        }
        else
        {
            // 정방향이면 그냥 계산된 각도 그대로
            arrowIndicator.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
        }
    }
    void UpdateArrowVisual()
    {
        if (arrowIndicator == null) return;

        float ratio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime;
        float targetScaleX = 1f + (ratio * 2f); 

        if (transform.localScale.x < 0)
             arrowIndicator.localScale = new Vector3(-targetScaleX, 1f, 1f);
        else
             arrowIndicator.localScale = new Vector3(targetScaleX, 1f, 1f);

        if (arrowSprite != null)
            arrowSprite.color = Color.Lerp(Color.yellow, Color.red, ratio);
    }

    void Jump()
    {
        if (mainCamera == null) return;

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector2 direction = (mousePos - transform.position).normalized;

        float powerRatio = Mathf.Clamp(chargeTime, 0, maxChargeTime) / maxChargeTime;
        float finalPower = powerRatio * maxJumpPower;

        if (finalPower < 5f) finalPower = 5f; 

        jumpCooldown = 0.2f;
        isGrounded = false; // 점프 순간 땅에서 떨어짐 처리
        
        rb.linearVelocity = direction * finalPower;
    }
}