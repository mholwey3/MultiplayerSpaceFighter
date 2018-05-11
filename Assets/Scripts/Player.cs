using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public float maxVelocity;
	public float thrust;
	public float deceleration;
	public float rotationSpeed;

    private new Rigidbody rigidbody;
    private ParticleSystem leftExhaust;
	private ParticleSystem rightExhaust;

	void Start () {
		rigidbody = GetComponent<Rigidbody>();
		leftExhaust = transform.GetChild(0).GetComponent<ParticleSystem>();
		rightExhaust = transform.GetChild(1).GetComponent<ParticleSystem>();
	}
	
	void FixedUpdate () {
		// MOVEMENT
		if (Input.GetButton("Vertical")){
			if (!leftExhaust.isPlaying && !rightExhaust.isPlaying && Input.GetAxis("Vertical") > 0f) {
				leftExhaust.Play();
				rightExhaust.Play();
			}
			rigidbody.AddForce(Input.GetAxis("Vertical") * transform.up * thrust);
			if (rigidbody.velocity.magnitude > maxVelocity) {
				rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxVelocity);
			}
		} else {
			if (leftExhaust.isPlaying && rightExhaust.isPlaying) {
				leftExhaust.Stop();
				rightExhaust.Stop();
			}
		}

		//ROTATION
		if (Input.GetButton("Horizontal")) {
			transform.Rotate(Input.GetAxis("Horizontal") * Vector3.back * rotationSpeed);
		}
	}
}
