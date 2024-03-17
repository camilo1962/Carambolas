using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CaromBilliards;

public class MainMenuManager : MonoBehaviour
{
    private PlayerPreferences m_playerPref;

    public string       m_singlePlayerSceneName;
    public Slider       m_masterVolumeSlider;
    public Dropdown     m_difficultyDropdown;
    public GameObject   m_lastGameScoreParent;
    public Text         m_lastGameScoreText;
    public Text         m_lastGameTimeText;
    public Text         m_lastGameShotsText;
    
    private void Start()
    {
        m_playerPref = FindObjectOfType<PlayerPreferences>();

        if (m_playerPref)
        {
            m_masterVolumeSlider.gameObject.SetActive(true);
            m_masterVolumeSlider.value = m_playerPref.MasterVolume;

            m_difficultyDropdown.gameObject.SetActive(true);
            m_difficultyDropdown.AddOptions(new List<string>(m_playerPref.GetDifficultyValuesStrings()));
            m_difficultyDropdown.value = (int) m_playerPref.Difficulty;
        }
        else
        {
            m_masterVolumeSlider.gameObject.SetActive(false);
            m_difficultyDropdown.gameObject.SetActive(false);
        }

        if (m_lastGameScoreParent)
        {
            if (ScoreManager.Instance)
            {
                if (ScoreManager.Instance.LastGameScore != null)
                {
                    m_lastGameScoreParent.SetActive(true);

                    if (m_lastGameScoreText)
                        m_lastGameScoreText.text = Utils.GetScoreString(ScoreManager.Instance.LastGameScore.m_score);

                    if (m_lastGameTimeText)
                        m_lastGameTimeText.text = Utils.GetElapsedTimeString(ScoreManager.Instance.LastGameScore.m_elapsedTime);

                    if (m_lastGameShotsText)
                        m_lastGameShotsText.text = Utils.GetShotsNumberString(ScoreManager.Instance.LastGameScore.m_shotNumber);
                }
                else
                {
                    m_lastGameScoreParent.SetActive(false);
                }
            }
            else
                m_lastGameScoreParent.SetActive(false);
        }
    }

    public void OnNewGameButtonClick()
    {
        SceneManager.LoadScene(m_singlePlayerSceneName);
    }

    public void OnQuitButtonClick()
    {
        Application.Quit();
    }

    public void OnMasterVolumeSliderChanged(float _newValue)
    {
        if(m_playerPref)
            m_playerPref.MasterVolume = _newValue;        
    }

    public void OnDifficultyChanged(int _difficulty)
    {
        if(m_playerPref)
            m_playerPref.Difficulty = (PlayerPreferences.GameDifficulty)_difficulty;
    }
}
