//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
///// <summary>
///// Singleton. Only designed to be in a game scene
///// Has no reference to any other Object
///// Other objects are listening to its events
///// </summary>
//public class StandardInputManager : InputManager
//{
//    #region Tweak
//    [Range(1, 6)]
//    [SerializeField]
//    private float m_mouseXSpeedFactor;
//    //[Range(1, 6)]
//    [SerializeField]
//    private float m_mouseWheelFactor;
//    #endregion
//
//
//    //Mouse
//    private bool        m_lastFrameMouseLeftClicked;
//
//    //Space key
//    private bool        m_lastFrameSpaceKeyPressed;
//    private float       m_shotCurrentPressDuration = 0.0f;    
//
//    private void Start()
//    {
//        m_lastFrameSpaceKeyPressed = false;
//    }
//
//    private void Update()
//    {
//        ManageMouseInputs();
//        ManageSpacePressure();         
//    }
//
//    private void ManageSpacePressure()
//    {
//        if (Input.GetButton("Jump"))
//        {
//            if(m_lastFrameSpaceKeyPressed)
//            {
//                if(m_shotCurrentPressDuration < m_shotMaxHoldDuration)
//                {
//                    m_shotCurrentPressDuration += Time.deltaTime;
//                    InvokeInputShotHoldEvent(GetShotPower());
//                }
//                else        // Max power !!!!
//                {
//                    m_shotCurrentPressDuration = m_shotMaxHoldDuration;
//
//                    InvokeInputShotHoldEvent(1);
//                    InvokeInputShotEvent(1);
//
//                    m_shotCurrentPressDuration = 0.0f;
//                    m_lastFrameSpaceKeyPressed = false;
//                }
//            }      
//            else
//                m_lastFrameSpaceKeyPressed = true;
//        }
//        else
//        {
//            if(m_lastFrameSpaceKeyPressed)   // when we release the key
//            {
//                InvokeInputShotEvent(GetShotPower());
//            }
//
//            m_lastFrameSpaceKeyPressed = false;
//            m_shotCurrentPressDuration = 0.0f;
//        }
//    }
//
//    private float GetShotPower()
//    {
//        return m_shotCurrentPressDuration / m_shotMaxHoldDuration;
//    }
//
//    private void ManageMouseInputs()
//    {
//        if (Input.GetButton("Fire1"))
//        {
//            float deltaMouse = Input.GetAxis("Mouse X"); 
//
//            InvokeInputCameraRotationEvent(- deltaMouse * m_mouseXSpeedFactor * Time.deltaTime);
//
//            m_lastFrameMouseLeftClicked = true;
//        }
//        else
//        {
//            m_lastFrameMouseLeftClicked = false;
//        }
//
//        if(Input.mouseScrollDelta.y != 0)
//        {
//            float delta = Input.mouseScrollDelta.y * m_mouseWheelFactor;
//            InvokeInputChangeCameraHeightEvent(-delta);
//        }
//    }       
//}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandardInputManager : InputManager
{
    #region Tweak
    [Range(1, 6)]
    [SerializeField]
    private float m_touchSpeedFactor;
    [SerializeField]
    private float m_touchPressureFactor;
    #endregion
    private float m_currentShotPower = 0f; // Variable para almacenar la fuerza del impulso
    // Touch
    private bool m_lastFrameTouched;

    private void Update()
    {
        ManageTouchInputs();
    }

    private void ManageTouchInputs()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                float deltaTouch = touch.deltaPosition.x;

                InvokeInputCameraRotationEvent(-deltaTouch * m_touchSpeedFactor * Time.deltaTime);

                m_lastFrameTouched = true;
            }
            else if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
            {
                // Calculate shot power based on touch pressure
                float touchPressure = Mathf.Clamp01(touch.pressure / m_touchPressureFactor);
                InvokeInputShotHoldEvent(touchPressure);
            }
        }
        else
        {
            if (m_lastFrameTouched)
            {
                // Release the shot if finger is lifted
                InvokeInputShotEvent(1);

            }

            m_lastFrameTouched = false;
        }
    }
    public void SetShotPower(float power)
    {
        m_currentShotPower = power;
    }
}

