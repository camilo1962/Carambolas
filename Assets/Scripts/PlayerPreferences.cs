using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// - Private Singleton that other objects optionnaly find via FindObjectOfType
/// - Not necessary to play the game
/// - Save preferences into PlayerPrefs
/// - If you want to do tests you can manually set values and uncheck 'm_saveAndLoadInPlayerPrefs'
/// </summary>
public class PlayerPreferences : MonoBehaviour
{
    /// <summary>
    /// The classic flow is to open MainMenu which will set player prefs, save them 
    /// thanks to this script, then open a Game Scene and use them. If we just want to test
    /// with other values (eg opening GameScene directly) we can disable this m_saveAndLoadInPlayerPrefs
    /// to force values and set them in Unity Inspector.
    /// </summary>
    [SerializeField]
    private bool m_saveAndLoadInPlayerPrefs; 

    private static PlayerPreferences m_instance;

    private const string KEY_VOLUME = "volume";
    private const string KEY_DIFFICULTY = "difficulty";

    [SerializeField]
    private float m_masterVolumeValue;
    public float MasterVolume
    {
        get
        {
            return m_masterVolumeValue;
        }
        set
        {
            m_masterVolumeValue = value;
            if (m_saveAndLoadInPlayerPrefs)
                PlayerPrefs.SetFloat(KEY_VOLUME, m_masterVolumeValue);
        }
    }    

    public enum GameDifficulty
    {
        Fácil,
        Medio,
        Difícil
    }

    [SerializeField]
    private GameDifficulty m_difficulty;
    public GameDifficulty Difficulty
    {
        get
        {
            return m_difficulty;
        }
        set
        {
            m_difficulty = value;

            if(m_saveAndLoadInPlayerPrefs)
                PlayerPrefs.SetInt(KEY_DIFFICULTY, (int) m_difficulty);
        }
    }

    private void Awake()
    {
        if(m_instance == null)     //first opening of menu
        {
            m_instance = this;
            DontDestroyOnLoad(this);
        }
        else                       //another scene is loaded
        {
            if(m_instance != this) //we only want to keep the original object
            {
                DestroyImmediate(this.gameObject);     
            }
        }


        if (PlayerPrefs.HasKey(KEY_VOLUME) && m_saveAndLoadInPlayerPrefs)
        {
            m_masterVolumeValue = PlayerPrefs.GetFloat(KEY_VOLUME);
        }

        if (PlayerPrefs.HasKey(KEY_DIFFICULTY) && m_saveAndLoadInPlayerPrefs)
        {
            m_difficulty = (GameDifficulty)PlayerPrefs.GetInt(KEY_DIFFICULTY);
        }
    }        
    
    public string[] GetDifficultyValuesStrings()
    {
        return System.Enum.GetNames(typeof(GameDifficulty));
    }
}
