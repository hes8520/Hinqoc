using UnityEngine;
using System.Collections;

public class SpeakerWaveEmitter : MonoBehaviour
{
    [Header("1. 시간 및 크기 조절")]
    public float anticipationTime = 0.5f;
    public float scaleExpansion = 1.3f; 
    public float waveDuration = 0.5f;       
    public float cooldown = 0.5f;

    [Header("2. 발사 설정")]
    public string triggerTag = "WooferStep";
    [Range(-180f, 180f)]
    public float fireAngle = 0f;
    public float projectileSpeed = 12f;
    public float pushForce = 20f;
    public float maxWaveScale = 1.5f;

    [Header("3. 연결")]
    public Animator animator;               
    public GameObject waveVisualPrefab;      
    public string animationTriggerName = "doBounce"; 

    [Header("4. 오디오 설정")]
    public AudioSource audioSource;    // 우퍼의 AudioSource 컴포넌트
    public AudioClip fireSfx;         // 발사 효과음 클립

    private bool isFiring = false;
    private float cooldownTimer = 0f;
    
    private Vector3 originalScale;
    private Vector3 originalPos;
    private float spriteHalfHeight;
    private PlayerController player; // 볼륨 참조용

    public Vector2 FireDirection => Quaternion.Euler(0, 0, fireAngle) * Vector2.right;

    void Start()
    {
        originalScale = transform.localScale;
        originalPos = transform.localPosition;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        spriteHalfHeight = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size.y / 2f : 0.5f;

        // 씬에서 플레이어를 찾아 SFX 볼륨 설정을 가져옵니다.
        player = Object.FindFirstObjectByType<PlayerController>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
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
            // 전체 쿨타임 계산
            cooldownTimer = anticipationTime + waveDuration + cooldown;
        }
    }

    IEnumerator EmitWaveRoutine()
    {
        isFiring = true;
        if (animator != null) animator.SetTrigger(animationTriggerName);

        // 발사체 생성 및 초기화
        GameObject currentWave = Instantiate(waveVisualPrefab, transform.position, Quaternion.identity);
        currentWave.transform.localScale = Vector3.zero;
        WaveProjectile projScript = currentWave.GetComponent<WaveProjectile>();
        if (projScript != null) projScript.enabled = false;

        // 예고 단계 (Anticipation)
        float timer = 0f;
        while (timer < anticipationTime)
        {
            timer += Time.deltaTime;
            float progress = timer / anticipationTime;

            float currentRatio = Mathf.Lerp(1f, scaleExpansion, progress);
            transform.localScale = originalScale * currentRatio;
            float yOffset = spriteHalfHeight * (currentRatio - 1f) * originalScale.y;
            transform.localPosition = originalPos + new Vector3(0, yOffset, 0);

            if (currentWave != null)
            {
                currentWave.transform.localScale = Vector3.one * (maxWaveScale * progress);
                currentWave.transform.position = transform.position + (Vector3)FireDirection * 0.2f;
            }
            yield return null;
        }

        // 발사 직후 스피커 위치 복구
        transform.localScale = originalScale;
        transform.localPosition = originalPos;

        // 실제 발사 및 소리 재생
        if (currentWave != null && projScript != null)
        {
            projScript.enabled = true;
            projScript.Launch(FireDirection, projectileSpeed, waveDuration, pushForce);

            // [수정됨] 효과음 재생 (메뉴 설정 볼륨 + 마스터 캡 반영)
            if (audioSource != null && fireSfx != null)
            {
                // 플레이어의 SFX 볼륨과 Master Volume Cap을 모두 곱해서 최종 볼륨 결정
                float currentSfxVol = (player != null) ? (player.sfxVolume * player.masterVolumeCap) : 1.0f;
                audioSource.PlayOneShot(fireSfx, currentSfxVol);
            }
        }
        
        isFiring = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, (Vector3)FireDirection * 5f);
    }
}