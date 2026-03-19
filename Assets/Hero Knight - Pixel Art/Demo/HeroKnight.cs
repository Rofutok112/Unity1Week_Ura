using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class HeroKnight : MonoBehaviour {

    [FormerlySerializedAs("m_speed")] [SerializeField] float      mSpeed = 4.0f;
    [FormerlySerializedAs("m_jumpForce")] [SerializeField] float      mJumpForce = 7.5f;
    [FormerlySerializedAs("m_rollForce")] [SerializeField] float      mRollForce = 6.0f;
    [FormerlySerializedAs("m_noBlood")] [SerializeField] bool       mNoBlood = false;
    [FormerlySerializedAs("m_slideDust")] [SerializeField] GameObject mSlideDust;

    private Animator            mAnimator;
    private Rigidbody2D         mBody2d;
    private SensorHeroKnight   mGroundSensor;
    private SensorHeroKnight   mWallSensorR1;
    private SensorHeroKnight   mWallSensorR2;
    private SensorHeroKnight   mWallSensorL1;
    private SensorHeroKnight   mWallSensorL2;
    private bool                mIsWallSliding = false;
    private bool                mGrounded = false;
    private bool                mRolling = false;
    private int                 mFacingDirection = 1;
    private int                 mCurrentAttack = 0;
    private float               mTimeSinceAttack = 0.0f;
    private float               mDelayToIdle = 0.0f;
    private float               mRollDuration = 8.0f / 14.0f;
    private float               mRollCurrentTime;


    // Use this for initialization
    void Start ()
    {
        mAnimator = GetComponent<Animator>();
        mBody2d = GetComponent<Rigidbody2D>();
        mGroundSensor = transform.Find("GroundSensor").GetComponent<SensorHeroKnight>();
        mWallSensorR1 = transform.Find("WallSensor_R1").GetComponent<SensorHeroKnight>();
        mWallSensorR2 = transform.Find("WallSensor_R2").GetComponent<SensorHeroKnight>();
        mWallSensorL1 = transform.Find("WallSensor_L1").GetComponent<SensorHeroKnight>();
        mWallSensorL2 = transform.Find("WallSensor_L2").GetComponent<SensorHeroKnight>();
    }

    // Update is called once per frame
    void Update ()
    {
        // Increase timer that controls attack combo
        mTimeSinceAttack += Time.deltaTime;

        // Increase timer that checks roll duration
        if(mRolling)
            mRollCurrentTime += Time.deltaTime;

        // Disable rolling if timer extends duration
        if(mRollCurrentTime > mRollDuration)
            mRolling = false;

        //Check if character just landed on the ground
        if (!mGrounded && mGroundSensor.State())
        {
            mGrounded = true;
            mAnimator.SetBool("Grounded", mGrounded);
        }

        //Check if character just started falling
        if (mGrounded && !mGroundSensor.State())
        {
            mGrounded = false;
            mAnimator.SetBool("Grounded", mGrounded);
        }

        // -- Handle input and movement --
        var inputX = Input.GetAxis("Horizontal");

        // Swap direction of sprite depending on walk direction
        if (inputX > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            mFacingDirection = 1;
        }
            
        else if (inputX < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            mFacingDirection = -1;
        }

        // Move
        if (!mRolling )
            mBody2d.linearVelocity = new Vector2(inputX * mSpeed, mBody2d.linearVelocity.y);

        //Set AirSpeed in animator
        mAnimator.SetFloat("AirSpeedY", mBody2d.linearVelocity.y);

        // -- Handle Animations --
        //Wall Slide
        mIsWallSliding = (mWallSensorR1.State() && mWallSensorR2.State()) || (mWallSensorL1.State() && mWallSensorL2.State());
        mAnimator.SetBool("WallSlide", mIsWallSliding);

        //Death
        if (Input.GetKeyDown("e") && !mRolling)
        {
            mAnimator.SetBool("noBlood", mNoBlood);
            mAnimator.SetTrigger("Death");
        }
            
        //Hurt
        else if (Input.GetKeyDown("q") && !mRolling)
            mAnimator.SetTrigger("Hurt");

        //Attack
        else if(Input.GetMouseButtonDown(0) && mTimeSinceAttack > 0.25f && !mRolling)
        {
            mCurrentAttack++;

            // Loop back to one after third attack
            if (mCurrentAttack > 3)
                mCurrentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (mTimeSinceAttack > 1.0f)
                mCurrentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            mAnimator.SetTrigger("Attack" + mCurrentAttack);

            // Reset timer
            mTimeSinceAttack = 0.0f;
        }

        // Block
        else if (Input.GetMouseButtonDown(1) && !mRolling)
        {
            mAnimator.SetTrigger("Block");
            mAnimator.SetBool("IdleBlock", true);
        }

        else if (Input.GetMouseButtonUp(1))
            mAnimator.SetBool("IdleBlock", false);

        // Roll
        else if (Input.GetKeyDown("left shift") && !mRolling && !mIsWallSliding)
        {
            mRolling = true;
            mAnimator.SetTrigger("Roll");
            mBody2d.linearVelocity = new Vector2(mFacingDirection * mRollForce, mBody2d.linearVelocity.y);
        }
            

        //Jump
        else if (Input.GetKeyDown("space") && mGrounded && !mRolling)
        {
            mAnimator.SetTrigger("Jump");
            mGrounded = false;
            mAnimator.SetBool("Grounded", mGrounded);
            mBody2d.linearVelocity = new Vector2(mBody2d.linearVelocity.x, mJumpForce);
            mGroundSensor.Disable(0.2f);
        }

        //Run
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Reset timer
            mDelayToIdle = 0.05f;
            mAnimator.SetInteger("AnimState", 1);
        }

        //Idle
        else
        {
            // Prevents flickering transitions to idle
            mDelayToIdle -= Time.deltaTime;
                if(mDelayToIdle < 0)
                    mAnimator.SetInteger("AnimState", 0);
        }
    }

    // Animation Events
    // Called in slide animation.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (mFacingDirection == 1)
            spawnPosition = mWallSensorR2.transform.position;
        else
            spawnPosition = mWallSensorL2.transform.position;

        if (mSlideDust != null)
        {
            // Set correct arrow spawn position
            var dust = Instantiate(mSlideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3(mFacingDirection, 1, 1);
        }
    }
}
