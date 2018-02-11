using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour {

	public Vector3 offset;
	public float lerpSpeed;

	private GameObject player;

	void Start() {
		player = GameObject.FindGameObjectWithTag("Player");
		transform.position = new Vector3(player.transform.position.x + offset.x, player.transform.position.y + offset.y, player.transform.position.z + offset.z);
	}
	
	void FixedUpdate () {
		Vector3 start = transform.position;
		Vector3 end = new Vector3(player.transform.position.x + offset.x, player.transform.position.y + offset.y, player.transform.position.z + offset.z);
		transform.position = Vector3.Lerp(start, end, lerpSpeed);
		transform.LookAt(player.transform);
	}
}
