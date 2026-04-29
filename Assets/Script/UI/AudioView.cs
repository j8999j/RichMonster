using GameSystem;
using UnityEngine;
using UnityEngine.UI;

public class AudioView : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private bool initializeSliderRange = true;

    private AudioManager _audioManager;

    private void Awake()
    {
        _audioManager = AudioManager.Instance;
        ConfigureSlider(musicVolumeSlider);
        ConfigureSlider(sfxVolumeSlider);
    }

    private void OnEnable()
    {
        if (_audioManager == null)
            _audioManager = AudioManager.Instance;

        Refresh();

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void OnDisable()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    public void Refresh()
    {
        if (_audioManager == null)
            return;

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(_audioManager.MusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(_audioManager.SfxVolume);
    }

    public void SetMusicVolume(float volume)
    {
        OnMusicVolumeChanged(volume);
    }

    public void SetSfxVolume(float volume)
    {
        OnSfxVolumeChanged(volume);
    }

    private void OnMusicVolumeChanged(float volume)
    {
        if (_audioManager == null)
            return;

        _audioManager.SetMusicVolume(volume);
    }

    private void OnSfxVolumeChanged(float volume)
    {
        if (_audioManager == null)
            return;

        _audioManager.SetSfxVolume(volume);
    }

    private void ConfigureSlider(Slider slider)
    {
        if (!initializeSliderRange || slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }
}
