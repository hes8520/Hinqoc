using UnityEngine;

public class ProjectileMover : MonoBehaviour
{
    [Header("설정")]
    public float speed = 10f; // 날아가는 속도
    public float lifetime = 3f; // 날아가는 최대 시간
    
    [Header("넉백 설정")]
    public float knockbackForce = 15f; // 플레이어를 미는 힘

    private Rigidbody2D rb;
    private Collider2D col;
    private bool hasHit = false; // 이미 맞았는지 확인하는 변수

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // 오른쪽(앞)으로 날아감
        rb.linearVelocity = transform.right * speed;

        // 아무것도 안 맞으면 3초 뒤 삭제
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 어딘가에 맞았다면 무시 (중복 처리 방지)
        if (hasHit) return;

        hasHit = true;

        // 1. 플레이어라면 밀어내기
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerScript = collision.gameObject.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                Vector2 direction = (collision.transform.position - transform.position).normalized;
                // 약간 위쪽으로 밀쳐야 더 잘 밀림
                Vector2 pushDir = (direction + Vector2.up * 0.5f).normalized; 
                playerScript.ApplyKnockback(pushDir, knockbackForce);
            }
        }

        // 2. [핵심] "맞고 튕겨 나가는" 연출
        // 더 이상 충돌하지 않도록 콜라이더 끄기
        if (col != null) col.enabled = false;

        // 중력 켜기 (바닥으로 떨어지게)
        rb.gravityScale = 2f;

        // 튕겨 나가는 힘 주기 (반대 방향 + 위쪽)
        rb.linearVelocity = Vector2.zero; // 기존 속도 초기화

        // [오류 수정된 부분] transform.right 앞에 (Vector2)를 붙여서 명확하게 2D로 바꿨습니다.
        Vector2 bounceDir = (-(Vector2)transform.right + Vector2.up).normalized;
        
        rb.AddForce(bounceDir * 5f, ForceMode2D.Impulse);

        // 뱅글뱅글 돌기 (회전)
        rb.angularVelocity = 360f; 

        // 3. 0.5초 뒤에 진짜로 삭제 (그동안은 튕기는 모습 보여줌)
        Destroy(gameObject, 0.5f);
    }
}