using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    private const float MOVING_THRESHOLD = 0.01f; //we consider a movement when velocity.magnitude is over this

    private float       m_maxSpeed; //in m/s
    public float MaxSpeed { get { return m_maxSpeed; }
        set { m_maxSpeed = value; } }

    private Rigidbody   m_rigidBody;
    
    #region Sound
    public AudioClip    m_cushionCollisionSound;
    public AudioClip    m_ballCollisionSound;
    public AudioClip    m_tacoBola;
    public AudioSource  m_audioSource;
    #endregion        

    private enum BilliardObjects
    {
        Cushion,
        WhiteBall,
        YellowBall,
        RedBall
    }

    private BilliardObjects m_selfTag;

    //to store every collisions from last shot
    //for basic rules it's a bit too much but may be 
    //usefull if we add new rules with Cushions etc
    private List<BilliardObjects> m_lastShotCollisions;

    //used to know if balls are stuck before a shot
    private HashSet<BilliardObjects> m_touchingObjectsWhileStopped;

    //in the situation balls are already in collision before
    //a shot, we need to detect if they move to trigger point
    private bool    m_hasMovedSinceLastCheck;


    private void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
        ResetLastShotCollisions();
        ResetTouchingObjects();
        m_hasMovedSinceLastCheck = false;

        switch (gameObject.tag)
        {
            case "WhiteBall":
                m_selfTag = BilliardObjects.WhiteBall;
                break;
            case "YellowBall":
                m_selfTag = BilliardObjects.YellowBall;
                break;
            case "RedBall":
                m_selfTag = BilliardObjects.RedBall;
                break;
            default:
                break;
        }
    }   

    public void ResetLastShotCollisions()
    {
        m_lastShotCollisions = new List<BilliardObjects>(20);
    }

    public void ResetTouchingObjects()
    {
        m_touchingObjectsWhileStopped = new HashSet<BilliardObjects>();
    }

    // colisión con otras bolas
    private void OnCollisionEnter(Collision collision)
    {
        switch(collision.collider.gameObject.tag)
        {
            case "WhiteBall":
                m_lastShotCollisions.Add(BilliardObjects.WhiteBall);
                break;
            case "YellowBall":
                m_lastShotCollisions.Add(BilliardObjects.YellowBall);
                break;
            case "RedBall":
                m_lastShotCollisions.Add(BilliardObjects.RedBall);
                break;
            default:
                break;
        }
        
        PlayBallCollisionSound(m_rigidBody.velocity.magnitude / m_maxSpeed);
    }

    private void OnCollisionStay(Collision collision)
    {
        switch (collision.collider.gameObject.tag)
        {
            case "WhiteBall":
                m_touchingObjectsWhileStopped.Add(BilliardObjects.WhiteBall);
                break;
            case "YellowBall":
                m_touchingObjectsWhileStopped.Add(BilliardObjects.YellowBall);
                break;
            case "RedBall":
                m_touchingObjectsWhileStopped.Add(BilliardObjects.RedBall);
                break;
            default:
                break;
        }
    }

    //collision with cushions
    private void OnTriggerEnter(Collider other)
    {
        Vector3 newVelocity = m_rigidBody.velocity - 2 * Vector3.Dot(m_rigidBody.velocity, other.transform.forward) * other.transform.forward;
        m_rigidBody.velocity = newVelocity;

        m_lastShotCollisions.Add(BilliardObjects.Cushion);

        PlayCushionCollisionSound(m_rigidBody.velocity.magnitude / m_maxSpeed);
    }

    private void Update()
    {
        if (m_rigidBody.velocity.magnitude <= MOVING_THRESHOLD)
        {
            //Ayudamos a que la pelota se detenga, de lo contrario es demasiado largo para nuestro juego.
            m_rigidBody.velocity = Vector3.zero;
        }
        else
        {
            m_hasMovedSinceLastCheck = true;
        }
    }

    public void OnPlayerShot(float _power, Vector3 _dir)
    {
        PlayTacoCollisionSound(_power);
        PlayBallCollisionSound(_power);
        PlayCushionCollisionSound(_power);
        m_rigidBody.velocity = m_maxSpeed * _power * _dir;
        

    }

    public bool IsStopped()
    {
        return m_rigidBody.velocity.magnitude == 0f;
    }

    public bool HasCollidedWithTwoOtherBallsLastShot()
    {
        HashSet<BilliardObjects> collisions = new HashSet<BilliardObjects>();
        foreach(BilliardObjects obj in m_lastShotCollisions)
        {
            if(obj != m_selfTag && obj != BilliardObjects.Cushion)
            {
                collisions.Add(obj);
                if (collisions.Count == 2)
                    return true;
            }
        }

        return false;
    }

    public bool IsTouchingTwoOtherBalls()
    {
        int touchingBallsNb = 0;
        foreach (BilliardObjects obj in m_touchingObjectsWhileStopped)
        {
            if (obj != m_selfTag && obj != BilliardObjects.Cushion)
            {
                touchingBallsNb++;
                if (touchingBallsNb == 2)
                    return true;
            }
        }

        return false;
    }

    public bool HasMovedSinceLastCheck()
    {
        return m_hasMovedSinceLastCheck;
    }
    public void ResetMovedSinceLastCheck()
    {
        m_hasMovedSinceLastCheck = false;
    }

    /// <summary>
    /// While using physic, after a shot, balls take a lot
    /// of time to decrease their velocity down to pure Zero
    /// We will just fake a bit and force their velocity to 0 when it's near.
    /// </summary>
    private void HelpBallStop()
    {
        
    }

    private void PlayCushionCollisionSound(float _volume)
    {
        if(GameManager.Instance.PlayerPreferences)
            m_audioSource.PlayOneShot(m_cushionCollisionSound, _volume * GameManager.Instance.PlayerPreferences.MasterVolume);
        else
            m_audioSource.PlayOneShot(m_cushionCollisionSound, _volume);
    }

    private void PlayBallCollisionSound(float _volume)
    {
        if (GameManager.Instance.PlayerPreferences)
            m_audioSource.PlayOneShot(m_ballCollisionSound, _volume * GameManager.Instance.PlayerPreferences.MasterVolume);
        else
            m_audioSource.PlayOneShot(m_ballCollisionSound, _volume);
    }
    private void PlayTacoCollisionSound(float _volume)
    {
        if (GameManager.Instance.PlayerPreferences)
            m_audioSource.PlayOneShot(m_tacoBola, _volume * GameManager.Instance.PlayerPreferences.MasterVolume);
        else
            m_audioSource.PlayOneShot(m_tacoBola, _volume);
    }
}
