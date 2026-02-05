using UnityEngine;
using System.Collections;

public class SpeakerWaveEmitter : MonoBehaviour
{
    [Header("1. 시간 및 크기 조절")]
    [Tooltip("예고 애니메이션 시간")]
    public float anticipationTime = 0.5f;
    
    [Tooltip("최대로 커질 배율 (1.3 = 1.3배)")]
    public float scaleExpansion = 1.3f; 

    [Tooltip("실제 음파 유지 시간")]
    public float waveDuration = 0.5f;       
    public float cooldown = 0.5f;

    [Header("2. 발사 설정")]
    public string triggerTag = "WooferStep";
    [Range(-180f, 180f)]
    public float fireAngle = 0f;
    public float waveRange = 8f;
    public float pushForce = 20f;
    public bool useDynamicPush = true;

    [Header("3. 연결")]
    public Animator animator;               
    public GameObject waveVisualPrefab;      
    public string animationTriggerName = "doBounce"; 

    private bool isFiring = false;
    private float cooldownTimer = 0f;
    private Transform player;
    private PlayerController playerScript;
    
    // 크기 및 위치 보정용 변수
    private Vector3 originalScale;
    private Vector3 originalPos;
    private float spriteHalfHeight; // 스프라이트 절반 높이 (바닥 보정 계산용)

    public Vector2 FireDirection => Quaternion.Euler(0, 0, fireAngle) * Vector2.right;

    void Start()
    {
        originalScale = transform.localScale;
        originalPos = transform.localPosition;

        // 스프라이트의 높이 계산 (없으면 1로 가정)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // 스프라이트의 로컬 좌표계 기준 높이
            spriteHalfHeight = sr.sprite.bounds.size.y / 2f; 
        }
        else
        {
            spriteHalfHeight = 0.5f; // 기본값
        }

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            player = pObj.transform;
            playerScript = pObj.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
    }

    public void TriggerByStep(string steppedTag)
    {
        if (cooldownTimer <= 0 && steppedTag == triggerTag && !isFiring)
        {
            StartCoroutine(EmitWaveRoutine());
            cooldownTimer = anticipationTime + waveDuration + cooldown;
        }
    }

    IEnumerator EmitWaveRoutine()
    {
        isFiring = true;

        if (animator != null) animator.SetTrigger(animationTriggerName);

        float timer = 0f;
        while (timer < anticipationTime)
        {
            timer += Time.deltaTime;
            float progress = timer / anticipationTime;

            // 1. 크기 키우기 (Lerp)
            // progress가 1일 때 scaleExpansion에 도달
            float currentRatio = Mathf.Lerp(1f, scaleExpansion, progress);
            transform.localScale = originalScale * currentRatio;

            // 2. [핵심] 바닥 고정을 위한 위치 보정
            // 커진 만큼(currentRatio - 1) * 절반 높이만큼 위로 올려줍니다.
            // Y축으로만 보정합니다 (스피커가 똑바로 서있다고 가정)
            float yOffset = spriteHalfHeight * (currentRatio - 1f) * originalScale.y;
            transform.localPosition = originalPos + new Vector3(0, yOffset, 0);

            yield return null;
        }

        // 발사 순간! (크기와 위치 원상복구)
        transform.localScale = originalScale;
        transform.localPosition = originalPos;

        if (waveVisualPrefab != null) {
            Instantiate(waveVisualPrefab, transform.position, Quaternion.Euler(0, 0, fireAngle));
        }

        float elapsed = 0f;
        while (elapsed < waveDuration)
        {
            elapsed += Time.deltaTime;
            CheckAndPushPlayer();
            yield return null;
        }
        
        isFiring = false;
    }

    void CheckAndPushPlayer()
    {
        if (player == null || playerScript == null) return;
        Vector2 dirToPlayer = (player.position - transform.position);
        float distance = dirToPlayer.magnitude;

        if (distance <= waveRange && Vector2.Angle(FireDirection, dirToPlayer.normalized) < 30f)
        {
            float force = pushForce;
            if (useDynamicPush) force *= (1f - (distance / waveRange));
            playerScript.ApplyKnockback(FireDirection, force * Time.deltaTime * 60f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 direction = FireDirection;
        Gizmos.DrawRay(transform.position, direction * waveRange);
    }
}