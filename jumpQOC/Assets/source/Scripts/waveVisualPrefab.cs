using UnityEngine;
using System.Collections;

public class WaveProjectile : MonoBehaviour
{
    private float speed;
    private float lifeTime;
    private float pushForce;
    private Vector2 moveDirection;
    private bool isLaunched = false;

    [Header("회전 설정")]
    public float rotationSpeed = 720f;

    private TrailRenderer trail;
    private ParticleSystem particles;
    private Collider2D waveCollider;

    void Awake()
    {
        trail = GetComponentInChildren<TrailRenderer>();
        particles = GetComponentInChildren<ParticleSystem>();
        waveCollider = GetComponent<Collider2D>();
        
        // 초기에는 비활성화
        if (trail != null) trail.enabled = false;
        if (particles != null) particles.Stop();
        if (waveCollider != null) waveCollider.enabled = false;
    }

    public void Launch(Vector2 fireDir, float moveSpeed, float duration, float force)
    {
        moveDirection = fireDir.normalized;
        speed = moveSpeed;
        lifeTime = duration;
        pushForce = force;
        isLaunched = true;

        // 시각 및 물리 효과 활성화
        if (trail != null) trail.enabled = true;
        if (waveCollider != null) waveCollider.enabled = true;
        if (particles != null) 
        {
            particles.Clear();
            particles.Play();
        }

        // 초기 방향 설정
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        StartCoroutine(DestroyAfterEffect());
    }

    // 히트박스에 닿아있는 동안 플레이어를 밀어냄
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isLaunched) return;

        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                // 음파가 날아가는 방향으로 힘 전달
                player.ApplyKnockback(moveDirection, pushForce * Time.deltaTime * 60f);
            }
        }
    }

    IEnumerator DestroyAfterEffect()
    {
        // 날아가는 시간 동안 대기
        yield return new WaitForSeconds(lifeTime);
        
        isLaunched = false;

        // 이동은 멈추고 비주얼과 히트박스만 먼저 제거
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        if (waveCollider != null) waveCollider.enabled = false;
        if (trail != null) trail.emitting = false;

        // 남아있는 파티클이 사라질 때까지 대기 후 완전히 삭제
        if (particles != null)
        {
            particles.Stop();
            yield return new WaitForSeconds(2.0f);
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (!isLaunched) return;

        // 직선 이동
        transform.Translate((Vector3)moveDirection * speed * Time.deltaTime, Space.World);

        // Z축 자전
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}