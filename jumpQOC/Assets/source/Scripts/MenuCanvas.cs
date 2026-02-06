using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("메뉴 패널들")]
    public GameObject mainMenuPanel;
    public GameObject settingsMenuPanel;

    [Header("설정 요소 (TextMeshPro)")]
    public Slider masterVolumeSlider;
    public TMP_Text masterVolumeText;
    
    public Slider bgmSlider;
    public TMP_Text bgmVolumeText;
    
    public Slider sfxSlider;
    public TMP_Text sfxVolumeText;

    [Header("효과음 설정")]
    public AudioSource menuAudioSource; 
    public AudioClip buttonClickSound;
    
    [Range(0f, 1f)] public float buttonVolumeScale = 0.5f; 

    private PlayerController player;
    private bool isMenuOpen = false;
    private const float DEFAULT_CAP = 0.4f; 

    void Start()
    {
        player = Object.FindFirstObjectByType<PlayerController>();

        float master = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1.0f);

        if (masterVolumeSlider)
        {
            masterVolumeSlider.value = master;
            masterVolumeSlider.onValueChanged.AddListener(UpdateMasterVolume);
            AddClickEventToSlider(masterVolumeSlider);
        }
        if (bgmSlider)
        {
            bgmSlider.value = bgm;
            bgmSlider.onValueChanged.AddListener(UpdateBGMVolume);
            AddClickEventToSlider(bgmSlider);
        }
        if (sfxSlider)
        {
            sfxSlider.value = sfx;
            sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
            AddClickEventToSlider(sfxSlider);
        }

        UpdateAllTexts(master, bgm, sfx);
        ApplyInitialVolumes(master, bgm, sfx);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void AddClickEventToSlider(Slider slider)
    {
        if (slider == null) return;
        EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((data) => { PlayButtonClick(); });
        trigger.triggers.Add(entry);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen) CloseAllMenus();
            else OpenMainMenu();
        }
    }

    public void UpdateMasterVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        if (masterVolumeText != null) masterVolumeText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void UpdateBGMVolume(float value)
    {
        // [안전장치] 플레이어 연결 끊기면 다시 찾기
        if (player == null) player = Object.FindFirstObjectByType<PlayerController>();

        if (player != null) player.bgmVolume = value;
        PlayerPrefs.SetFloat("BGMVolume", value);
        if (bgmVolumeText != null) bgmVolumeText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void UpdateSFXVolume(float value)
    {
        if (player == null) player = Object.FindFirstObjectByType<PlayerController>();

        if (player != null) player.sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        if (sfxVolumeText != null) sfxVolumeText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void UpdateAllTexts(float m, float b, float s)
    {
        if (masterVolumeText != null) masterVolumeText.text = Mathf.RoundToInt(m * 100) + "%";
        if (bgmVolumeText != null) bgmVolumeText.text = Mathf.RoundToInt(b * 100) + "%";
        if (sfxVolumeText != null) sfxVolumeText.text = Mathf.RoundToInt(s * 100) + "%";
    }

    private void ApplyInitialVolumes(float m, float b, float s)
    {
        AudioListener.volume = m;
        if (player == null) player = Object.FindFirstObjectByType<PlayerController>(); // 시작할 때도 한번 더 확인
        if (player != null) { player.bgmVolume = b; player.sfxVolume = s; }
    }

    public void PlayButtonClick()
    {
        if (menuAudioSource != null && buttonClickSound != null)
        {
            float baseVolume = 1.0f;
            if (player != null)
            {
                baseVolume = player.sfxVolume * player.masterVolumeCap;
            }
            else
            {
                baseVolume = 1.0f * DEFAULT_CAP; 
            }
            float finalVolume = baseVolume * buttonVolumeScale;
            menuAudioSource.PlayOneShot(buttonClickSound, finalVolume); 
        }
    }

    public void OpenMainMenu() 
    { 
        isMenuOpen = true; 
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true); 
        Time.timeScale = 0f; 
        Cursor.visible = true; 
        Cursor.lockState = CursorLockMode.None; 
    }

    public void CloseAllMenus() 
    { 
        PlayButtonClick(); 
        isMenuOpen = false; 
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false); 
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(false); 
        Canvas.ForceUpdateCanvases(); 
        Time.timeScale = 1f; 
    }

    public void GoToSettings() 
    { 
        PlayButtonClick(); 
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false); 
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(true); 
    }

    public void GoToMain() 
    { 
        PlayButtonClick(); 
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true); 
        if (settingsMenuPanel != null) settingsMenuPanel.SetActive(false); 
    }
}