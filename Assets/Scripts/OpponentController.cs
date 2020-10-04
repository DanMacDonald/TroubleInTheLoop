using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentController : MonoBehaviour
{
    public PlayerController Player;
    public WorldController World;
    public Rigidbody Body;
    public GameObject ConeSprite;
    Transform mTransform;
    public float Radius = 0.25f;
    float PathWidth = 5f;
    public float RadiusSpeedDelta = 1f;
    public float DragCoefficent = 1f;
    public bool IsGrounded;
    float verticalVelocity = 0f;
    Vector3 impactVelocity;
    Vector3 lateralVelocity = Vector3.zero;
    public Vector3 LateralVelcoity => lateralVelocity;
    bool wasGrounded;
    RaycastHit[] hitResults = new RaycastHit[5];
    
    public float JumpDetectDistance = 3;
    public float PlayerJumpDetectDistance;
    public float JumpForce = 12f;
    public float Gravity = 1.2f;
	bool isAirborn => IsGrounded == false;
    float timeFallStated;
    public float timeGrounded;
    public bool IsHoldingCone { get; set; }

    Vector3 forwadVector;

    // Start is called before the first frame update
    void Start()
    {
        mTransform = transform;
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + forwadVector);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Opponent Hit {collision.gameObject.name}");
        if (collision.gameObject == Player.gameObject && Player.isInputAllowed == false )
        {
            if (Player.IsHoldingCone)
            {
                Player.IsHoldingCone = !Player.IsHoldingCone;
				this.IsHoldingCone = !Player.IsHoldingCone;
                Player.StartInput();
            }
        }
    }


    public void ApplyImpact(Vector3 impactVelocity)
    {
        this.impactVelocity = impactVelocity;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        ConeSprite.gameObject.SetActive(IsHoldingCone);

        // Compare the player distance to yourself
        var myDistance = mTransform.position.magnitude;
        var playerDist = Player.transform.position.magnitude;
        //Debug.Log($"playerDistance {playerDist}");

        var deltaDist = myDistance - playerDist;
        var playerRot = Player.WorldRotation;
        var worldDelta = Player.WorldRotation - World.Speed;
        var relativeRot = playerRot * (RadiusSpeedDelta *(deltaDist / PathWidth)) - worldDelta;

        mTransform.RotateAround(Vector3.zero, Vector3.up, relativeRot * Time.deltaTime);
		
        // Calculate a forward Vector
        var offsetVector = mTransform.position;
        offsetVector.y = 0;
        forwadVector = Vector3.Cross(offsetVector, Vector3.up);
        mTransform.forward = forwadVector.normalized;

        var deltaFromLeftSide = offsetVector.magnitude - 42f;
        var pathWidthPercent = deltaFromLeftSide / PathWidth;

        if (pathWidthPercent < 0.4f)
        {
            lateralVelocity = offsetVector.normalized * 50f * Time.deltaTime;
        }
        else if (pathWidthPercent > 0.6f)
        {
            lateralVelocity = offsetVector.normalized * -50f * Time.deltaTime;
        }

        if (impactVelocity.magnitude != 0)
        {
            lateralVelocity += impactVelocity;
            var drag = impactVelocity.normalized * DragCoefficent * Time.deltaTime;
            var signBefore = Mathf.Sign(Vector3.Dot(impactVelocity, offsetVector));
            impactVelocity -= drag;
            var signAfter =  Mathf.Sign(Vector3.Dot(impactVelocity, offsetVector));
            if (signBefore != signAfter)
                impactVelocity = Vector3.zero;

        }

        IsGrounded = CheckIsGrounded(out var GroundPos);
        
		// Apply gravity if airborn
		if (isAirborn)
		{
			// Walked off a ledge
			if (wasGrounded) 
			{
				timeFallStated = Time.time;
				wasGrounded = false;
			}

			// Have gravity be slower at the beginning of a decent
			verticalVelocity -= Mathf.Lerp(Gravity * 0.25f,Gravity, Time.time - timeFallStated);
		}
		else 
		{
			mTransform.position = GroundPos + Vector3.up*Radius;
			verticalVelocity = 0;
            if (wasGrounded == false)
                timeGrounded = Time.time;

            wasGrounded = true;
		}

        // Jump detection
        if (CheckObsticalAhead(out var raycastHit) && IsGrounded)
        {
            var isJumping = false;
            var distance = (raycastHit.point - mTransform.position).magnitude;
            distance -= Radius;

            //Debug.Log($"Opponent raycast hit '{raycastHit.collider.gameObject.name}'");

            if (raycastHit.collider.gameObject == Player.gameObject)
            {
                if (World.Speed == World.BaseSpeed && distance < PlayerJumpDetectDistance)
                {
                    isJumping = true;

                    if (Player.IsHoldingCone) {
                        Player.IsHoldingCone = !Player.IsHoldingCone;
				        this.IsHoldingCone = !Player.IsHoldingCone;
                    }
                }
            }
            else if (distance < JumpDetectDistance) 
            {
               isJumping = true;
            }

            if (isJumping)
            {
                //Debug.Log($"Jump! jumpDistance:{distance}");
                wasGrounded = false;
                verticalVelocity = JumpForce;
            }
        }

        var vel = Body.velocity;
        vel.y = verticalVelocity;
        Body.velocity = vel;

        // If we've been on the ground a while, zero out any gained velocity
        // we might have from a weird collision
        if (Time.time - timeGrounded > 1f)
            Body.velocity = new Vector3(lateralVelocity.x, verticalVelocity, lateralVelocity.z);
    }

    bool CheckObsticalAhead(out RaycastHit hit)
    {
        var layerMask = LayerMask.GetMask("Default");
		//var hits = Physics.RaycastNonAlloc(transform.position - new Vector3(0, Radius * 0.5f,0), forwadVector, hitResults, Mathf.Infinity, layerMask);
        var hits = Physics.SphereCastNonAlloc(transform.position, Radius - 0.05f, forwadVector.normalized, hitResults, Mathf.Infinity, layerMask);
		var closestDistance = float.MaxValue;
		RaycastHit closestHit = default;
        for (int i = 0; i < hits; i++)
        {
			hit = hitResults[i];
			if (hit.distance < closestDistance && hit.collider.gameObject != gameObject)
			{
				closestDistance = hit.distance;
				closestHit = hit;
			}
        }

		if (closestHit.collider != null)
		{
            hit = closestHit;
            return true;
		}

		hit = default;
		return false;
    }

    bool CheckIsGrounded(out Vector3 hitPoint)
	{
		var layerMask = ~0;
		var hits = Physics.RaycastNonAlloc(transform.position, transform.up * -1, hitResults, Mathf.Infinity, layerMask);
		var closestDistance = float.MaxValue;
		RaycastHit closestHit = default;
		hitPoint = Vector3.zero;
        for (int i = 0; i < hits; i++)
        {
			var hit = hitResults[i];
			if (hit.distance < closestDistance)
			{
				closestDistance = hit.distance;
				closestHit = hit;
			}
        }

		if (closestHit.collider != null)
		{
			//Debug.Log($"Hit {closestHit.collider.gameObject.name} at {closestDistance}");
			hitPoint = closestHit.point;
		}

		return closestDistance < Radius + 0.01f;
	}
}
