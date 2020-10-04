using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public float Speed;
    public float BaseSpeed;
    Transform mTransform;
    // Start is called before the first frame update
    void Start()
    {
        mTransform = transform;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        mTransform.Rotate(0,Speed * Time.deltaTime, 0, Space.Self);
    }
}
