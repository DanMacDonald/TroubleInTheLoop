using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController Player;
    public float LookAtYOffset = 1.5f;
    public float PositionYOffset;
    public float FollowSpeed = 1f;
    
    // Start is called before the first frame update
    void Start() 
    {
        var playerLookAt = Player.GroundPos + new Vector3(0, LookAtYOffset,0);
        var zDistance = playerLookAt.z - transform.position.z;
        var yDistance = playerLookAt.y - transform.position.y;
        Debug.Log($"zDistance:{zDistance}");

        var defaultLook = Vector3.forward * zDistance;
        var playerLook = new Vector3(0, yDistance, zDistance);
        var angle = Vector3.SignedAngle(defaultLook, playerLook, Vector3.right);
        var eulers = transform.eulerAngles;
        eulers.x = angle;
        transform.eulerAngles = eulers;

    }
   

    // Update is called once per frame
    void Update()
    {
       
        var pos = transform.position;

        // Position
        var targetYPos = Player.GroundPos.y + PositionYOffset;
        var delta = targetYPos - transform.position.y;
        var stepDistance = FollowSpeed * Time.deltaTime;

        if (Mathf.Abs(delta) > stepDistance)
        {
            if (delta > 0)
            {
                pos.y += FollowSpeed * Time.deltaTime;
            } 
            else if ( delta < 0) 
            {
                pos.y -= FollowSpeed * Time.deltaTime;
            }
        }

        transform.position = pos;        
    }
}
