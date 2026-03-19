using UnityEngine;
using System.Collections;

public class SensorHeroKnight : MonoBehaviour {

    private int mColCount = 0;

    private float mDisableTimer;

    private void OnEnable()
    {
        mColCount = 0;
    }

    public bool State()
    {
        if (mDisableTimer > 0)
            return false;
        return mColCount > 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        mColCount++;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        mColCount--;
    }

    void Update()
    {
        mDisableTimer -= Time.deltaTime;
    }

    public void Disable(float duration)
    {
        mDisableTimer = duration;
    }
}
