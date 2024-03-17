using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{     
    //By default, Camera will be aligned to this point
    //I choose the middle of the billiard table
    public Transform m_focusPoint;

    //the distance between Camera and focused ball
    //if we stay in table's plan
    private float m_horizontalDistanceToBall;

    //Vertical Height of Camera from focused ball
    private float m_cameraHeight;

    //Pitch Angle of the camera in Degrees
    private float m_cameraPitchAngle;

    [Range(0, 5)]
    public float m_maxHorizontalDistanceToBall;
    [Range(0, 5)]
    public float m_minHorizontalDistanceToBall;
    [Range(0, 90)]
    public float m_minPitchAngle;
    [Range(0, 90)]
    public float m_maxPitchAngle;
    [Range(0, 8)]
    public float m_minHeight;
    [Range(0, 8)]
    public float m_maxHeight;

    [SerializeField]
    [Range(0, 1)]
    private float m_currentPitchHeightValue; //0 to 1

    //Base direction is alignment between White Ball and 'm_referencePointForOrientation' 
    //(I put the table's center). This angle simply move camera around the white ball
    private float m_angleOffsetFromBaseDir;
    public float AimAngleFromBase { get { return m_angleOffsetFromBaseDir; } }

    //Camera will always look in this direction
    [SerializeField]
    private Transform       m_ball;

    #region AimHelper
    private List<GameObject> m_aimHelpers;

    //In max difficulty there is 0 helper
    //1 Ray is 1 direction until a cushion bounce
    private int         m_numberOfAimHelperRays;

    [SerializeField]
    private float       m_aimHelperthickness = 0.03f;    
    [SerializeField]
    private Material    m_aimHelperMaterial;
    [SerializeField]
    private LayerMask   m_ballsLayer;
    #endregion

    [SerializeField]
    private float       m_easeMoveDuration;
    private bool        m_isInEaseMove;
    private Vector3     m_previousPosition;
    private Vector3     m_newPosition;
    private Vector3     m_newLookAtPoint;
    private float       m_easeMoveTimer;

    private Vector3     m_lookAtPoint;
    
    private PlayerPreferences.GameDifficulty m_gameDifficulty;
    public void SetGameDifficulty(PlayerPreferences.GameDifficulty _difficulty)
    {
        m_gameDifficulty = _difficulty;
        InitAimHelpers();
    }

    public delegate void CameraChangedAimDirection(Vector3 _direction);
    public event CameraChangedAimDirection CameraChangedAimDirectionEvent;

    private void Start()
    {      
        m_angleOffsetFromBaseDir = 0f;
        m_isInEaseMove = false;
        m_easeMoveTimer = 0f;

        OnPitchHeightValueChanged(m_currentPitchHeightValue);
    }

    public void SmoothlyMoveToNewPositionWithAngle(float _angle)
    {
        m_isInEaseMove = true;

        m_angleOffsetFromBaseDir = _angle;

        m_previousPosition = transform.position;

        m_newPosition = FindCameraPosition(m_ball.position, m_focusPoint.position, _angle);
        m_newLookAtPoint = FindLookAtPoint(m_ball.position, m_newPosition);
    }

    public void MoveCameraInput(float _angle)
    {
        if (m_isInEaseMove)
            return;

        m_angleOffsetFromBaseDir += _angle;

        RefreshPosition();
    }

    public void SetCameraPositionWithAngle(float _angle)
    {
        m_angleOffsetFromBaseDir = _angle;
        RefreshPosition();
    }

    public void OnInputChangeCameraHeight(float _deltaValue)
    {
        m_currentPitchHeightValue += _deltaValue;

        if (m_currentPitchHeightValue > 1)
            m_currentPitchHeightValue = 1;
        else if (m_currentPitchHeightValue < 0)
            m_currentPitchHeightValue = 0;

        OnPitchHeightValueChanged(m_currentPitchHeightValue);
    }

    //We update Aim Helpers objects depending of camera direction
    private void Update()
    {
        if (!m_ball)
        {
            Debug.LogError("CameraManager needs a 'ballToFocus'.");
            return;
        }

        if(m_isInEaseMove)
        {
            if (m_easeMoveTimer >= m_easeMoveDuration)
            {
                m_isInEaseMove = false;
                m_easeMoveTimer = 0f;
                                
                RefreshPosition();
            }
            else
            {
                m_easeMoveTimer += Time.deltaTime;

                float t = m_easeMoveTimer / m_easeMoveDuration;

                this.gameObject.transform.position = new Vector3(Mathf.Lerp(m_previousPosition.x, m_newPosition.x, t),
                    Mathf.Lerp(m_previousPosition.y, m_newPosition.y, t), Mathf.Lerp(m_previousPosition.z, m_newPosition.z, t));

                this.transform.LookAt(m_newLookAtPoint);
            }
        }
        
        if (m_aimHelpers == null)
        {
            //this case is triggered only if nobody called SetGameDifficulty()
            //should be done by GameManager
            InitAimHelpers();
        }
        else
        {
            if (m_numberOfAimHelperRays > 0)
            {
                if (GameManager.Instance && GameManager.Instance.CurrentGameState != GameManager.GameState.Shooting)
                {
                    if (m_aimHelpers[0].activeSelf)
                    {
                        SetActiveAimHelpers(false);
                    }
                }
                else if(!m_isInEaseMove) //If there is no GameManager (for debug / testing purpose ?) we activate AimHelpers anyway
                {
                    SetActiveAimHelpers(true);

                    //AIMING HELPER
                    Vector3 lastPos = m_ball.position;
                    Vector3 lastDirection = new Vector3(this.transform.forward.x, 0, this.transform.forward.z).normalized;
                    RaycastHit hit;

                    for (int i = 0; i < m_numberOfAimHelperRays; i++)
                    {
                        if (Physics.Raycast(lastPos, lastDirection, out hit, 10))
                        {
                            //Debug.DrawRay(lastPos, lastDirection.normalized * hit.distance, Color.yellow);                   

                            m_aimHelpers[i].transform.localScale = new Vector3(m_aimHelperthickness, m_aimHelperthickness, hit.distance);
                            m_aimHelpers[i].transform.localPosition = (hit.point + lastPos) * 0.5f;
                            m_aimHelpers[i].transform.localRotation = Quaternion.FromToRotation(Vector3.forward, lastDirection);

                            //we don't show any helper after ball collision
                            //with physics we cannot predict
                            if (Mathf.Pow(2, hit.collider.gameObject.layer) == m_ballsLayer.value)
                            {
                                for (int j = i + 1; j < m_numberOfAimHelperRays; j++)
                                {
                                    m_aimHelpers[j].SetActive(false);
                                }
                                break;
                            }

                            Vector3 newPos = hit.point;
                            Vector3 newDir = Vector3.Reflect(lastDirection, hit.normal);
                            lastDirection = newDir;
                            lastPos = newPos;
                        }
                    }
                }
            }
        }        
                
    }

    private Vector3 FindCameraPosition(Vector3 _ballPos, Vector3 _focus, float _angle)
    {
        //let's find Camera position in the 2D plan of the table (ball height)
        //I'll use Vector2's 'y' for real 'z'
        Vector2 ballPos = new Vector2(_ballPos.x, _ballPos.z);

        //baseOrientPoint is the point we use to align camera and ball with
        Vector2 focusPoint = new Vector2(_focus.x, _focus.z);

        Vector2 ballToFocusVec = focusPoint - ballPos;
        Vector2 direction = ballToFocusVec.normalized;

        //angle between X axis and 'ballToOrient' vector
        float baseAngle = 0;
        if (Vector2.Dot(new Vector2(0, 1), direction) >= 0)
        {
            baseAngle = Mathf.Acos(direction.x);
        }
        else
        {
            baseAngle = 2 * Mathf.PI - Mathf.Acos(direction.x);
        }

        //I calculate a new "focus point" from "base focus point" with an offset angle
        Vector2 newFocusPoint = new Vector2(Mathf.Cos(baseAngle + _angle) + ballPos.x, Mathf.Sin(baseAngle + _angle) + ballPos.y);

        Vector2 posCamera2D = ballPos + (ballPos - newFocusPoint) * m_horizontalDistanceToBall;

        //now we have the new camera position in this 2D Plan, we just have to add the Height
        Vector3 newCameraPos = new Vector3(posCamera2D.x, _ballPos.y + m_cameraHeight, posCamera2D.y);

        return newCameraPos;
    }
    
    private Vector3 FindLookAtPoint(Vector3 _ballPos, Vector3 _cameraPos)
    {
        //Find the direction of Camera with angle = 0 (horizontal plan)
        Vector3 baseDirPoint = new Vector3(_ballPos.x, _cameraPos.y, _ballPos.z);
        Vector3 baseDir = baseDirPoint - _cameraPos;

        //Calculate vertical distance offset between newDir and baseDir points
        float verticalDist = Mathf.Abs(Mathf.Tan(m_cameraPitchAngle * Mathf.Deg2Rad) * baseDir.magnitude);

        Vector3 lookAtPoint = new Vector3(baseDirPoint.x, baseDirPoint.y - verticalDist, baseDirPoint.z);

        return lookAtPoint;
    }

    private void FindOrientationAndRotate()
    {
        this.transform.LookAt(FindLookAtPoint(m_ball.position, this.transform.position), Vector3.up);

        CameraChangedAimDirectionEvent?.Invoke(new Vector3(transform.forward.x, 0, transform.forward.z));
    }

    private void OnPitchHeightValueChanged(float _value)
    {
        m_cameraPitchAngle = m_minPitchAngle + (m_maxPitchAngle - m_minPitchAngle) * _value;
        m_cameraHeight = m_minHeight + (m_maxHeight - m_minHeight) * _value;
        m_horizontalDistanceToBall = m_minHorizontalDistanceToBall + (m_maxHorizontalDistanceToBall - m_minHorizontalDistanceToBall) * _value;
        RefreshPosition();
    }    

    private void RefreshPosition()
    {
        this.gameObject.transform.position = FindCameraPosition(m_ball.position, m_focusPoint.position, m_angleOffsetFromBaseDir);
        FindOrientationAndRotate();
    }

    private void InitAimHelpers()
    {
        if (m_aimHelpers != null)
            DeleteAimHelpers();

        switch (m_gameDifficulty)
        {
            case PlayerPreferences.GameDifficulty.Fácil:
                m_numberOfAimHelperRays = 4;
                break;
            case PlayerPreferences.GameDifficulty.Medio:
                m_numberOfAimHelperRays = 3;
                break;
            case PlayerPreferences.GameDifficulty.Difícil:
                m_numberOfAimHelperRays = 1;
                break;
            default:
                break;
        }

        m_aimHelpers = new List<GameObject>(m_numberOfAimHelperRays);
        for (int i = 0; i < m_numberOfAimHelperRays; i++)
        {
            m_aimHelpers.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
            m_aimHelpers[i].SetActive(false);

            BoxCollider collider = m_aimHelpers[i].GetComponent<BoxCollider>();
            if (collider)
                Destroy(collider);

            Material material = m_aimHelperMaterial;
            m_aimHelpers[i].GetComponent<MeshRenderer>().material = material;
        }
    }

    private void SetActiveAimHelpers(bool _b)
    {
        for (int i = 0; i < m_numberOfAimHelperRays; i++)
        {
            m_aimHelpers[i].SetActive(_b);
        }
    }

    private void DisableAimHelpers()
    {
        for (int i = 0; i < m_numberOfAimHelperRays; i++)
        {
            m_aimHelpers[i].SetActive(false);
        }
    }

    private void DeleteAimHelpers()
    {
        for (int i = 0; i < m_aimHelpers.Count; i++)
        {
            if (m_aimHelpers[i])
                GameObject.DestroyImmediate(m_aimHelpers[i]);
        }

        m_aimHelpers = new List<GameObject>();
    }
}
