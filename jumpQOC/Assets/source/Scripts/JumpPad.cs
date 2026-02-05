using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("점프 설정")]
    public float jumpForce = 20f; // 튀어오를 힘 (높이)
    
    [Header("효과음 (선택사항)")]
    public AudioClip bounceSfx;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 플레이어인지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            // 2. 위에서 밟았는지 확인 (normal.y가 음수면 플레이어가 위에서 아래로 충돌한 것)
            if (collision.GetContact(0).normal.y < -0.5f)
            {
                Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    // [핵심] 현재 속도를 초기화하고 위로만 힘을 줌 (일정한 높이 보장)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

                    // 3. (선택) 플레이어의 오디오 소스를 빌려 소리 재생
                    AudioSource playerAudio = collision.gameObject.GetComponent<AudioSource>();
                    if (playerAudio != null && bounceSfx != null)
                    {
                        playerAudio.PlayOneShot(bounceSfx);
                    }
                    
                    Debug.Log("점프대 작동!");
                }
            }
        }
    }
}