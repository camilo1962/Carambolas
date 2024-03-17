using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton Managing all the game logic
/// Needs an InputManager
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Dependencies
    //Needs an InputManager
    [SerializeField]
    private InputManager    m_inputManager;

    //Needs a Camera Manager
    [SerializeField]
    private CameraManager   m_cameraManager;

    //Optionnal PlayerPreferences
    [SerializeField]
    private PlayerPreferences m_playerPref;
    public PlayerPreferences PlayerPreferences { get { return m_playerPref; } } //Used by Ball.cs and CameraManager.cs
    #endregion
  

    public string           m_menuSceneName; //to switch scene

    #region Tweak
    public Ball             m_whiteBall;
    public Ball             m_redBall;
    public Ball             m_yellowBall;
    private Vector3         m_whiteBallStartPos;
    private Vector3         m_yellowBallStartPos;
    private Vector3         m_redBallStartPos;

    
    [SerializeField]
    private float           m_shotMaxHoldDuration;
    [SerializeField]
    private float           m_ballsMaxSpeed; //in m/s
    [SerializeField]
    private float           m_endOfShotWaitDuration;
    [SerializeField]
    private int             m_gameOverScoreToReach;
    #endregion

    private float           m_endOfShotWaitTimer;

    #region Singleton
    private static GameManager m_instance;
    public static GameManager Instance
    {
        get
        {
            return m_instance;
        }
    }

    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        else
        {
            if (m_instance != this) 
            {
                Debug.LogError("You cannot have multiple GameManager ! This one will be destroyed.");
                Destroy(this.gameObject);
            }
        }
    }
    #endregion

    #region Events
    public delegate void PlayerIsShootingEvent(float _power);
    public event PlayerIsShootingEvent PlayerShotEvent;
    public event PlayerIsShootingEvent InputShotHoldEvent;

    public delegate void        ScoredPoint();
    public event ScoredPoint    ScoredPointEvent;

    public delegate void        SwitchState(GameState _newState);
    public event SwitchState    LeaveStateEvent;
    public event SwitchState    EnterStateEvent;

    public delegate void        GameOver();
    public event GameOver       GameOverEvent;
    #endregion
    
    #region GameState
    public GameState            CurrentGameState { get { return m_currentGameSate; } }
    private GameState           m_currentGameSate;   

    public enum GameState
    {
        Shooting,
        ProcessingShot,
        EndOfShotScoredPoint,
        PreReplay,
        ProcessingReplay,
        GameOverScreen
    }
    #endregion

    #region ReplayData
    struct ReplayShotData
    {
        public Vector3 whiteBallPos;
        public Vector3 yellowBallPos;
        public Vector3 redBallPos;
        public Vector3 shotDirection;
        public float   shotPower; //0 to 1
        public float   cameraAngle; //the m_angleOffsetFromBaseDir, used to calculate camera position 
    }

    private ReplayShotData m_lastShotData;

    public float  m_preReplayDuration; //we wait a bit before shooting
    private float m_preReplayTimer;
    #endregion

    private Vector3 m_shotAimDirection; //the horizontal/flat ball shot direction    
   
   
   
    private void Start()
    {
      
        m_inputManager.InputShotHoldEvent += OnInputShotHold;
        m_inputManager.InputChangeCameraHeightEvent += OnInputChangeCameraHeight;
        m_inputManager.m_shotMaxHoldDuration = m_shotMaxHoldDuration;

        m_cameraManager.CameraChangedAimDirectionEvent += OnAimDirectionChanged;
       
        //if we didn't manually set a PlayerPref  
        //OR if we come from MainMenu and another instance of
        //PlayerPref overrided our reference, let's try to find one
        if (m_playerPref == null) 
        {
            m_playerPref = FindObjectOfType<PlayerPreferences>();
        }

        m_cameraManager.SetGameDifficulty(m_playerPref ? m_playerPref.Difficulty : PlayerPreferences.GameDifficulty.Fácil);

        m_whiteBall.MaxSpeed = m_yellowBall.MaxSpeed = m_redBall.MaxSpeed = m_ballsMaxSpeed;
        m_whiteBallStartPos = m_whiteBall.transform.position;
        m_yellowBallStartPos = m_yellowBall.transform.position;
        m_redBallStartPos = m_redBall.transform.position;

        m_preReplayTimer = 0f;

        SwitchGameState(GameState.Shooting);        
    }

    private void Update()
    {
        switch (m_currentGameSate)
        {
            case GameState.Shooting:
                break;
            case GameState.ProcessingShot:
                if (m_whiteBall.IsStopped() && m_yellowBall.IsStopped() && m_redBall.IsStopped())
                {
                    //classic case
                    bool scored = m_whiteBall.HasCollidedWithTwoOtherBallsLastShot();
                    //special case when balls where already in collision
                    if(!scored)
                    {
                        //white ball was touching the two others + at least one of the other ball moved 
                        bool touch = m_whiteBall.IsTouchingTwoOtherBalls();
                        if(touch)
                        {
                            if (m_yellowBall.HasMovedSinceLastCheck() || m_redBall.HasMovedSinceLastCheck())
                            {
                                scored = true;
                            }
                        }
                    }

                    if(scored)
                        SwitchGameState(GameState.EndOfShotScoredPoint);
                    else
                        SwitchGameState(GameState.Shooting);
                }
                break;
            case GameState.EndOfShotScoredPoint:
                m_endOfShotWaitTimer += Time.deltaTime;
                if(m_endOfShotWaitTimer >= m_endOfShotWaitDuration)
                {
                    m_endOfShotWaitTimer = 0f;
                    SwitchGameState(GameState.Shooting);
                }
                break;
            case GameState.PreReplay:
                if (m_preReplayTimer <= m_preReplayDuration)
                {
                    m_preReplayTimer += Time.deltaTime;
                }
                else
                {
                    m_preReplayTimer = 0f;
                    SwitchGameState(GameState.ProcessingReplay);
                }
                break;
            case GameState.ProcessingReplay: 
                if (m_whiteBall.IsStopped() && m_yellowBall.IsStopped() && m_redBall.IsStopped())
                {
                    SwitchGameState(GameState.Shooting);
                }
                break;
            default:
                break;
        }     
    }

    private void SwitchGameState(GameState _state)
    {
        LeaveGameState(m_currentGameSate);
        m_currentGameSate = _state;
        EnterGameState(m_currentGameSate);      
    }

    private void EnterGameState(GameState _state)
    {
        switch (_state)
        {
            case GameState.Shooting:
                ResetBallsShotData();
                m_inputManager.InputShotEvent += OnPlayerShotInput;
                m_inputManager.InputMoveCameraEvent += OnMoveCameraInput;

                m_cameraManager.SmoothlyMoveToNewPositionWithAngle(0f);
                break;
            case GameState.ProcessingShot:
                break;
            case GameState.EndOfShotScoredPoint:
                ScoredPointEvent?.Invoke();

                //checking if Game is over
                if (ScoreManager.Instance)
                {
                    if (ScoreManager.Instance.CurrentGameScore >= m_gameOverScoreToReach)
                    {
                        SwitchGameState(GameState.GameOverScreen);
                    }
                }
                else
                {
                    Debug.LogError("Sin ScoreManager no habrá GameOver.");
                }
                break;            
            case GameState.PreReplay:
                break;
            case GameState.ProcessingReplay:
                m_whiteBall.OnPlayerShot(m_lastShotData.shotPower, m_lastShotData.shotDirection);
                break;
            case GameState.GameOverScreen:
                GameOverEvent?.Invoke();
                break;
            default:
                break;
        }

        EnterStateEvent?.Invoke(_state);
    }

    private void LeaveGameState(GameState _state)
    {
        switch (_state)
        {
            case GameState.Shooting:
                m_inputManager.InputShotEvent -= OnPlayerShotInput;
                m_inputManager.InputMoveCameraEvent -= OnMoveCameraInput;
                break;
            case GameState.ProcessingShot:
                break;
            case GameState.EndOfShotScoredPoint:
                break;            
            case GameState.PreReplay:
                break;
            case GameState.ProcessingReplay:
                break;            
            case GameState.GameOverScreen:
                break;
            default:
                break;
        }

        LeaveStateEvent?.Invoke(_state);
    }       

    private void ResetBallsShotData()
    {
        m_whiteBall.ResetLastShotCollisions();
        m_yellowBall.ResetLastShotCollisions();
        m_redBall.ResetLastShotCollisions();

        m_whiteBall.ResetTouchingObjects();
        m_yellowBall.ResetTouchingObjects();
        m_redBall.ResetTouchingObjects();

        m_whiteBall.ResetMovedSinceLastCheck();
        m_yellowBall.ResetMovedSinceLastCheck();
        m_redBall.ResetMovedSinceLastCheck();
    }

    public void RestartGame()
    {
        if (ScoreManager.Instance)
            ScoreManager.Instance.ResetScores();

        ResetBallsPos();

        SwitchGameState(GameState.Shooting);
    }

    private void OnPlayerShotInput(float _power)
    {
        PlayerShotEvent?.Invoke(_power);

        m_whiteBall.OnPlayerShot(_power, m_shotAimDirection.normalized); //it's already normalized but it's a double check 

        SwitchGameState(GameState.ProcessingShot);                

        SaveLastShotData(_power);
    }

    private void OnInputShotHold(float _power)
    {
        InputShotHoldEvent?.Invoke(_power);
    }

    private void OnInputChangeCameraHeight(float _delta)
    {
        m_cameraManager.OnInputChangeCameraHeight(_delta);
    }

    private void SaveLastShotData(float _shotPower)
    {
        m_lastShotData = new ReplayShotData();
        m_lastShotData.whiteBallPos = m_whiteBall.gameObject.transform.position;
        m_lastShotData.yellowBallPos = m_yellowBall.gameObject.transform.position;
        m_lastShotData.redBallPos = m_redBall.gameObject.transform.position;
        m_lastShotData.shotDirection = m_shotAimDirection.normalized;
        m_lastShotData.shotPower = _shotPower;
        m_lastShotData.cameraAngle = m_cameraManager.AimAngleFromBase;
    }

    private void PlaceElementsLikeBeforeShot()
    {
        m_whiteBall.gameObject.transform.position = m_lastShotData.whiteBallPos;
        m_yellowBall.gameObject.transform.position = m_lastShotData.yellowBallPos;
        m_redBall.gameObject.transform.position = m_lastShotData.redBallPos;

        m_cameraManager.SmoothlyMoveToNewPositionWithAngle(m_lastShotData.cameraAngle);
    }

    private void OnAimDirectionChanged(Vector3 _direction)
    {
        m_shotAimDirection = _direction;
    }

    private void OnMoveCameraInput(float _val)
    {
        m_cameraManager.MoveCameraInput(_val);
    }

    public void StartReplay()
    {
        SwitchGameState(GameState.PreReplay);
        PlaceElementsLikeBeforeShot();
    }

    private void ResetBallsPos()
    {
        m_whiteBall.transform.position = m_whiteBallStartPos;
        m_yellowBall.transform.position = m_yellowBallStartPos;
        m_redBall.transform.position = m_redBallStartPos;
    }      

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(m_menuSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }    
}