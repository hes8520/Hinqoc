using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    public float destroyTime = 0.5f; // 애니메이션 길이에 맞춰 설정 (예: 0.5초)

    void Start()
    {
        // 생성되자마자 지정된 시간 후에 스스로 삭제됨
        Destroy(gameObject, destroyTime);
    }
}