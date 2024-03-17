using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Persistent Singleton to share score across every scenes and
/// also serialize / deserialize last game score
/// </summary>
public class ScoreManager : MonoBehaviour
{
    private static ScoreManager m_instance;
    public static ScoreManager Instance
    {
        get
        {
            return m_instance;
        }
    }        

    private int     m_currentGameScore;
    public int      CurrentGameScore { get { return m_currentGameScore; } }
    private float   m_elapsedTime;
    public float    ElapsedTime { get { return m_elapsedTime; } }
    private int     m_shotNumber;
    public int      ShotNumber { get { return m_shotNumber; } }
    
    public class SerializableScores
    {
        public int      m_score;
        public float    m_elapsedTime;
        public int      m_shotNumber;
    }

    private string              m_saveFilePath;
    private SerializableScores  m_lastGameScore;
    public SerializableScores   LastGameScore { get { return m_lastGameScore; } }

    private void Awake()
    {
        if (m_instance == null)     //first opening of menu
        {
            m_instance = this;
            DontDestroyOnLoad(this);
        }
        else                       //another scene is loaded
        {
            if (m_instance != this) //we only want to keep the original object
            {
                DestroyImmediate(this.gameObject);
            }
        }
#if UNITY_ANDROID || UNITY_IOS
        m_saveFilePath = Path.Combine(Application.persistentDataPath, "lastGameSave.json");
#else
    m_saveFilePath = Path.Combine(Application.dataPath, "lastGameSave.json");
#endif
        m_lastGameScore = null;

       //// Establece la ruta del archivo en el directorio persistente de la aplicación en dispositivos móviles
       //m_saveFilePath = Path.Combine(Application.persistentDataPath, "lastGameSave.json");
       //    m_lastGameScore = null;
        
       ////inicio perezoso aquí
       //m_saveFilePath = Application.dataPath + "/lastGameSave.json";
       //m_lastGameScore = null;
    }

    private void Start() //when starting directly in Game scene
    {
        Init();
    }

    private void OnLevelWasLoaded() //while switching from Menu to Game or Game to Menu scene
    {
        Init();
    }

    private void Init()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.ScoredPointEvent += OnPointScored;
            GameManager.Instance.GameOverEvent += SerializeScore;
            GameManager.Instance.PlayerShotEvent += OnPlayerShot;
        }

        ResetScores();
        DeserializeScore();
    }

    private void Update()
    {
        if (GameManager.Instance && GameManager.Instance.CurrentGameState == GameManager.GameState.Shooting)            
            m_elapsedTime += Time.deltaTime;    
    }

    public void ResetScores()
    {
        m_currentGameScore = 0;
        m_elapsedTime = 0f;
        m_shotNumber = 0;
    }
    
    private void OnPointScored()
    {
        m_currentGameScore++;     
    }

    private void OnPlayerShot(float _power)
    {
        m_shotNumber++;
    }    

    private void SerializeScore()
    {
        SerializableScores s = new SerializableScores();
        s.m_score = m_currentGameScore;
        s.m_elapsedTime = m_elapsedTime;
        s.m_shotNumber = m_shotNumber;

        string jsonScore = JsonUtility.ToJson(s);
        
        if(!File.Exists(m_saveFilePath))
        {
            File.Create(m_saveFilePath).Dispose();
        }

        File.WriteAllText(m_saveFilePath, jsonScore);
    }

    private void DeserializeScore()
    {
        if (File.Exists(m_saveFilePath))
        {
            m_lastGameScore = JsonUtility.FromJson<SerializableScores>(File.ReadAllText(m_saveFilePath));
        }
    }
}
