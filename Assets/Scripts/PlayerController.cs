using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public WorldController WorldController;
    public Rigidbody Body;
	public AnimationCurve ForceCurve;
	public OpponentController Opponent;
	public HUDController HUD;
	public GameObject HUDPrefab;
	public float recoveryTime = 0.5f;
	public float WorldRotation;
	public float radius = 0.25f;
	public float DragCoefficent = 10f;
	public Transform SphereMesh;
	public Transform ChatMount;
	public GameObject ConeSprite;
	public bool IsHoldingCone { get; set; } = true;

	public Camera GameCamera;
	public float Force = 50f;
	public float JumpForce = 1000f;
	public float Gravity = 8f;
	public float MaxVelocity = 8.5f;
	public Vector3 GroundPos;
	public float fowardPanningScalar = 100f;
    Vector3 worldHoriz;
    Transform mTransform;
	float timeInDirection;
	float forceScalar;
	float timeInputStopped;
	float timeFallStated;
	RaycastHit[] hitResults = new RaycastHit[5];
	bool wasGrounded;
	bool isGrounded;
	bool isInputPressedLastFrame;
	bool isJumping;
	Vector3 lateralVelocity = Vector3.zero;
	float verticalVelocity = 0f;
	 Vector3 impactVelocity;
	bool isDecending => verticalVelocity < 0;
	bool isAirborn => isGrounded == false;
	public bool isInputAllowed;
	bool isPlayerDead;

    void Start()
    {
        mTransform = transform;
		if (HUDController.Instance == null) {
			var hudGO = GameObject.Instantiate(HUDPrefab, Vector3.zero, Quaternion.identity);
			HUD = hudGO.GetComponent<HUDController>();
			Debug.LogWarning("Creating HUD");
		}
		HUD = HUDController.Instance;
		HUD.Player = this;
		Reset();
		TriggerStartSequence();
    }

	public void Reset(bool reloadScene = false)
	{
		if (reloadScene)
			SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);

		WorldController.Speed = 0;
		isInputAllowed = false;
		isPlayerDead = false;
	}

	public void ApplyImpact(Vector3 impactVelocity)
    {
        this.impactVelocity = impactVelocity;
    }

	public void TriggerStartSequence()
	{
		StartCoroutine(StartSequence());
	}

	IEnumerator StartSequence()
	{
		HUD.ShowText(ChatMount, "");
		yield return new WaitForSeconds(0.1f);
		HUD.ShowText(ChatMount, "yum");
		yield return new WaitForSecondsRealtime(0.8f);
		HUD.ShowText(ChatMount, "I like Ice Cream");
		yield return new WaitForSeconds(1f);
		HUD.ShowText(ChatMount, "");
		
	}

	public void StartInput()
	{
		WorldController.Speed = WorldController.BaseSpeed;
		isInputAllowed = true;
		HUD.ShowText(ChatMount, "HEY!");
		StartCoroutine(HideText());
	}

	IEnumerator HideText()
	{
		yield return new WaitForSeconds(2);
		HUD.ShowText(ChatMount, "");
	}

	public void TriggerKilledSequence()
	{

	}

	IEnumerator KillSequence()
	{
		yield return 0;
	}

	public void TriggerOpponentKilled()
	{

	}

	bool IsPlayerDead()
	{
		if (isPlayerDead) return true;

		var camerOffsetVector = GameCamera.transform.position - transform.position;
		var proj = Vector3.Dot(camerOffsetVector, GameCamera.transform.forward);

		return proj > -1f;
	}



	void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.DrawRay(contact.point, contact.normal * 5f, Color.white);

			if (contact.otherCollider.gameObject == Opponent.gameObject) {
				// Calculate the impact velocity
				var impactVelocity = lateralVelocity - Opponent.LateralVelcoity;

				// See if the impact velocity is in the same direction as the player
				var isSameDirectoin = Vector3.Dot(impactVelocity, lateralVelocity) > 0;

				if (isSameDirectoin)
				{
					Opponent.ApplyImpact(impactVelocity);
					ApplyImpact(-impactVelocity * 0.5f);
					//Opponent.timeGrounded = Time.time;
					lateralVelocity = Vector3.zero;
					timeInDirection = 0;
					Debug.Log($"Impact magnitude{impactVelocity.magnitude}");

					if (impactVelocity.magnitude > 7.8f)
					{
						this.IsHoldingCone = !this.IsHoldingCone;
						Opponent.IsHoldingCone = !this.IsHoldingCone;
					}
				}
			}
			else if (contact.otherCollider.gameObject.layer == LayerMask.NameToLayer("KillPlayer"))
			{
				isPlayerDead = true;
			}
        }
    }

	void OnDrawGizmos()
	{
		Gizmos.color = isGrounded ? new Color(1,0,0,0.5f) : new Color(0,1,0,0.5f);
		Gizmos.DrawWireSphere(transform.position, 0.5f);
	}

    void FixedUpdate()
    {

		if (IsPlayerDead())
		{
			if (HUD.IsEnding == false)
			{
				HUD.TriggerTheEndSequence();
				isInputAllowed = false;
			}
			return;
		}


		ConeSprite.gameObject.SetActive(IsHoldingCone);

		var isAnyKeyPressed = false;
		worldHoriz = GameCamera.transform.right;
		worldHoriz.y = 0;
		worldHoriz.Normalize();

		isGrounded = CheckIsGrounded(out GroundPos);

		if (isGrounded)
			wasGrounded = true;

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
			mTransform.position = GroundPos + Vector3.up*radius;
			verticalVelocity = 0;
		}
		
		if (isInputAllowed)
		{
			// Left Input
			if (Input.GetKey(KeyCode.A))
			{
				// Key pressed this frame?
				if (Input.GetKeyDown(KeyCode.A))
					timeInDirection = 0;

				forceScalar = ForceCurve.Evaluate(timeInDirection);
				if (Vector3.Dot(Body.velocity, worldHoriz) > 0)
					forceScalar = 1f;

				lateralVelocity -= (forceScalar * Force) * worldHoriz * Time.deltaTime;
				isAnyKeyPressed = true;
			}
			// Right Input
			else if (Input.GetKey(KeyCode.D))
			{
				// Key Pressed this frame?
				if (Input.GetKeyDown(KeyCode.D))
					timeInDirection = 0;

				forceScalar = ForceCurve.Evaluate(timeInDirection);
				if (Vector3.Dot(Body.velocity, worldHoriz) < 0)
					forceScalar = 1f;

				lateralVelocity += (forceScalar * Force) * worldHoriz * Time.deltaTime;
				isAnyKeyPressed = true;
			}

			var speedScalar = 1f;

			if (Input.GetKey(KeyCode.LeftShift))
			{
				speedScalar = 3;
			}

			if (Input.GetKey(KeyCode.W))
			{
				if (mTransform.position.z < 0 )
				{
					mTransform.RotateAround(Vector3.zero, Vector3.up, -WorldRotation * Time.deltaTime * speedScalar);
				}
				else
				{
					WorldController.Speed = WorldRotation * speedScalar;
				}
			}
			else
			{
				if (mTransform.position.z < 0 )
				{
					mTransform.RotateAround(Vector3.zero, Vector3.up, -WorldController.BaseSpeed * Time.deltaTime * speedScalar);
				}
				else
				{
					WorldController.Speed = WorldController.Speed * speedScalar;
				}

				if (WorldController.Speed != 0)
					WorldController.Speed = WorldController.BaseSpeed;
			}

			// Was left or right pressed?
			if (isAnyKeyPressed)
			{
				isInputPressedLastFrame = true;

				// Clamp lateral velocity while movement keys are pressed
				if (lateralVelocity.magnitude > MaxVelocity)
				{
					lateralVelocity.y = 0; // insurance :)
					lateralVelocity =  lateralVelocity.normalized * MaxVelocity;
				}
				
				timeInDirection += Time.deltaTime;
			}
			else
			{
				// Did we have movement input last frame?
				if (isInputPressedLastFrame)
				{
					// Track the time input stopped
					timeInputStopped = Time.time;
					isInputPressedLastFrame = false;
				}

				// Use the time input stopped to lerp the lateral velocity to 0
				if (timeInputStopped + recoveryTime < Time.time && isGrounded) {
					var deltaTime = Time.time - timeInputStopped;
					var t = deltaTime / recoveryTime;
					lateralVelocity = Vector3.Lerp(lateralVelocity, Vector3.zero, t);
				}
			}
			
			// Jump input
			if (Input.GetKey(KeyCode.Space) && isGrounded)
			{
				wasGrounded = false;
				timeFallStated = 0;
				verticalVelocity = JumpForce;
				mTransform.position = GroundPos + new Vector3(0,radius + 0.01f,0);
			}
		}

		Body.velocity = new Vector3(lateralVelocity.x, verticalVelocity, lateralVelocity.z);

		// Get a vector perpendicular to velocity
		// Calculate a forward Vector
        var offsetVector = mTransform.position;
        offsetVector.y = 0;
		offsetVector.Normalize();
        var forwradVector = Vector3.Cross(offsetVector, Vector3.up);
        mTransform.forward = forwradVector.normalized;

		var fakeVelocity = forwradVector * WorldController.Speed;
		var axelVector = Vector3.Cross(fakeVelocity, Vector3.up);
		SphereMesh.RotateAround(SphereMesh.position, axelVector, -Time.deltaTime * fakeVelocity.magnitude * fowardPanningScalar);
		
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



		if (lateralVelocity.magnitude > 0.01f)
		{
			var fakeHorizVelocity = Mathf.Sign(Vector3.Dot(lateralVelocity, offsetVector)) * MaxVelocity;
			SphereMesh.RotateAround(SphereMesh.position, forwradVector, -Time.deltaTime * fakeHorizVelocity * 100f);
		}

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

		return closestDistance < radius + 0.01f;
	}
}
