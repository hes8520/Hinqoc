using UnityEngine;
using System.Collections;

public class FrozenTrap : MonoBehaviour
{
    [Header("1. 말풍선 그림 프리팹")]
    public GameObject freezingBubblePrefab; 
    public GameObject frozenBubblePrefab;   

    private GameObject freezingBubbleInstance; 
    private GameObject frozenBubbleInstance;   

    [Header("2. 파티클 및 효과음")]
    public ParticleSystem snowParticle; 
    public AudioSource sfxAudioSource; 
    public AudioClip countSound;      
    public AudioClip freezeSound;     

    [Header("3. 배경음악 (BGM)")]
    public AudioSource bgmAudioSource; 
    public AudioClip zoneBgm;          
    public float fadeDuration = 1.0f; 

    [Header("4. 빙결 밸런스")]
    public float freeTime = 3.0f;           
    public float countInterval = 1.0f;      
    public float freezeDuration = 2.0f;     
    [Range(0f, 1f)] public float freezeChance = 0.5f; 

    [Header("5. 연출 및 위치 설정")]
    public Vector3 freezingOffset = new Vector3(0, 2.0f, 0); 
    public Vector3 frozenOffset = new Vector3(0, 2.0f, 0); 
    public Color frozenColor = new Color(0.5f, 0.8f, 1f, 1f);

    private PlayerController player;
    private SpriteRenderer playerSR;
    private Color originalColor;
    private bool isTrapActive = false;
    private bool isFading = false;
    private Coroutine trapCoroutine;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (freezingBubblePrefab != null)
        {
            freezingBubbleInstance = Instantiate(freezingBubblePrefab);
            freezingBubbleInstance.SetActive(false);
        }

        if (frozenBubblePrefab != null)
        {
            frozenBubbleInstance = Instantiate(frozenBubblePrefab);
            frozenBubbleInstance.SetActive(false);
        }

        if (snowParticle) snowParticle.Stop();
        
        if (bgmAudioSource != null)
        {
            bgmAudioSource.loop = true; 
            bgmAudioSource.Stop();      
            if (zoneBgm != null) bgmAudioSource.clip = zoneBgm;
        }

        player = Object.FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        // [수정] BGM 볼륨을 '제곱'해서 적용 (사람 귀에 자연스럽게 들림)
        if (player != null && bgmAudioSource != null && !isFading)
        {
            // 예: 슬라이더 0.5일 때 -> 0.5 * 0.5 = 0.25 (확 줄어듦)
            float curvedVolume = player.bgmVolume * player.bgmVolume;
            
            // 최종 볼륨 = 곡선볼륨 * 마스터캡
            bgmAudioSource.volume = curvedVolume * player.masterVolumeCap;
        }
    }

    public void ActivateTrap(PlayerController targetPlayer)
    {
        if (isTrapActive) return;
        
        player = targetPlayer;
        playerSR = player.GetComponent<SpriteRenderer>();
        isTrapActive = true;
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        isFading = false;

        if (snowParticle) snowParticle.Play();
        
        if (bgmAudioSource != null && zoneBgm != null)
        {
            // 켜질 때도 제곱된 볼륨으로 시작
            float curvedVolume = player.bgmVolume * player.bgmVolume;
            bgmAudioSource.volume = curvedVolume * player.masterVolumeCap; 
            
            if (!bgmAudioSource.isPlaying) bgmAudioSource.Play();
        }

        if (trapCoroutine == null) trapCoroutine = StartCoroutine(PeriodicTrapRoutine());
    }

    public void DeactivateTrap()
    {
        isTrapActive = false;
        
        if (snowParticle) snowParticle.Stop();
        
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutBgm());
        }
        
        StopAllGimmicks();
    }

    IEnumerator FadeOutBgm()
    {
        isFading = true; 
        float startVolume = bgmAudioSource.volume;
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.volume = startVolume; 
        isFading = false; 
    }

    IEnumerator PeriodicTrapRoutine()
    {
        while (isTrapActive)
        {
            yield return new WaitForSeconds(freeTime);
            if (!isTrapActive) break;

            if (freezingBubbleInstance) freezingBubbleInstance.SetActive(true);
            
            for (int i = 0; i < 3; i++)
            {
                PlaySfx(countSound);
                float timer = 0f;
                while (timer < countInterval)
                {
                    if (!isTrapActive) break;
                    timer += Time.deltaTime;
                    UpdateBubblePos(freezingBubbleInstance, freezingOffset); 
                    yield return null;
                }
                if (!isTrapActive) break;
            }
            if (freezingBubbleInstance) freezingBubbleInstance.SetActive(false);

            if (isTrapActive && Random.value < freezeChance)
            {
                if (player) player.isFrozen = true; 
                if (playerSR) { originalColor = playerSR.color; playerSR.color = frozenColor; }
                
                if (frozenBubbleInstance) frozenBubbleInstance.SetActive(true);
                PlaySfx(freezeSound);

                float fTimer = 0f;
                while (fTimer < freezeDuration)
                {
                    if (!isTrapActive) break;
                    fTimer += Time.deltaTime;
                    UpdateBubblePos(frozenBubbleInstance, frozenOffset); 
                    yield return null;
                }

                if (player && isTrapActive) player.isFrozen = false;
                if (playerSR && isTrapActive) playerSR.color = originalColor;
                if (frozenBubbleInstance) frozenBubbleInstance.SetActive(false);
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        trapCoroutine = null;
    }

    void UpdateBubblePos(GameObject obj, Vector3 offset)
    {
        if (player != null && obj != null)
            obj.transform.position = player.transform.position + offset;
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource && clip)
        {
            float vol = (player != null) ? player.sfxVolume * player.masterVolumeCap : 1f;
            sfxAudioSource.PlayOneShot(clip, vol);
        }
    }

    void StopAllGimmicks()
    {
        if (trapCoroutine != null) { StopCoroutine(trapCoroutine); trapCoroutine = null; }
        
        if (freezingBubbleInstance) freezingBubbleInstance.SetActive(false);
        if (frozenBubbleInstance) frozenBubbleInstance.SetActive(false);
        
        if (player) player.isFrozen = false;
        if (playerSR) playerSR.color = Color.white;
    }
    
    void OnDestroy()
    {
        if (freezingBubbleInstance) Destroy(freezingBubbleInstance);
        if (frozenBubbleInstance) Destroy(frozenBubbleInstance);
    }
}