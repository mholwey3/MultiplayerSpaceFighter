using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public float maxVelocity;
	public float thrust;
	public float deceleration;
	public float rotationSpeed;

	private Rigidbody rigidbody;

	void Start () {
		rigidbody = GetComponent<Rigidbody>();
	}
	
	void FixedUpdate () {
		// MOVEMENT
		if (Input.GetButton("Vertical")){
			rigidbody.AddForce(Input.GetAxis("Vertical") * transform.up * thrust);
			if (rigidbody.velocity.magnitude > maxVelocity) {
				rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxVelocity);
			}
		}

		//ROTATION
		if (Input.GetButton("Horizontal")) {
			transform.Rotate(Input.GetAxis("Horizontal") * Vector3.back * rotationSpeed);
		}
	}
}
