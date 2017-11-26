using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SyncListVector3 : SyncListStruct<Vector3> { }
/**
 * Most basic player possible. Use arrow keys to move around.
 */
public class Player : NetworkBehaviour {
	[SyncVar]
	internal Color color = Color.black;
	internal Color _color = Color.black;
	[SyncVar]
	internal Vector3 dir;
	[SyncVar]
	internal bool living = true;

	[SyncVar] internal int speed = 2;
	[SyncVar] public int fastSpeed = 10;
	[SyncVar] public int midSpeed = 5;
	[SyncVar] public int slowSpeed = 2;


	[SyncVar]
	internal SyncListVector3 segments;

	List<Transform> displays;

	public Transform segmentPrefab;

	internal float heartbeat = 0;
	internal int size = 0;
	internal int length = 10;
	internal int myIndex;
	[SyncVar] internal float time = 0;

	void Start() {
		displays = new List<Transform>();
	}
	public override void OnStartServer() {
		base.OnStartServer();
		color = new Color(Random.value, Random.value, Random.value);
	}

	void OnEnable() {
		Control.main.players.Add(this);
		myIndex = Control.main.players.IndexOf(this);
		transform.position = transform.position.floor();
		segments.Add(transform.position);
	}
	void OnDisable() {
		if (Control.main != null) {
			Control.main.players[myIndex] = null;
		}
	}

	void Update() {
		if (isServer) {
			heartbeat += Time.deltaTime;
			if (heartbeat > 15) {
				heartbeat = 0;
				StartCoroutine(SendHeartbeat());
			}
		}

		if (color != _color) {
			_color = color;
			UpdateDisplayColor();
		}
		if (!isLocalPlayer) return;

		Vector3 dir = Vector3.zero;

		if (Input.GetKey(KeyCode.UpArrow)) CmdSetDirection(Vector3.up);
		else if (Input.GetKey(KeyCode.DownArrow)) CmdSetDirection(Vector3.down);
		else if (Input.GetKey(KeyCode.LeftArrow)) CmdSetDirection(Vector3.left);
		else if (Input.GetKey(KeyCode.RightArrow)) CmdSetDirection(Vector3.right);

		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
			CmdSetSpeed(fastSpeed);
		} else if (Input.GetKey(KeyCode.Space)) {
			CmdSetSpeed(slowSpeed);
		} else {
			CmdSetSpeed(midSpeed);
		}

		if (Input.GetKeyDown(KeyCode.R)) {
			CmdDoSomething();
		}

	}

	private void UpdateDisplayColor() {
		foreach (var renderer in GetComponentsInChildren<Renderer>()) {
			renderer.material.color = color;
		}
	}

	public void LateUpdate() {
		while (displays.Count < segments.Count) {
			Transform seg = Instantiate(segmentPrefab);
			seg.SetParent(transform);
			seg.GetComponent<Renderer>().material.color = color;
			displays.Add(seg);
		}
		for (int i = 0; i < displays.Count; i++) {
			displays[i].position = segments[i];
		}
	}
	[Command]
	void CmdDoSomething() {
		color = new Color(Random.value, Random.value, Random.value);
		UpdateDisplayColor();
	}
	[Command]
	void CmdSetDirection(Vector3 dir) {
		this.dir = dir;
	}

	[Command]
	void CmdSetSpeed(int speed) {
		this.speed = speed;
	}
	IEnumerator SendHeartbeat() {
		// The easiest way I've found to get all the connection data to the clients is to just
		// mash it all together in the match name
		//string name = string.Join(":", new string[] { natHelper.externalIP, Network.player.ipAddress, guid });

		//networkMatch.CreateMatch(name, 2, true, "", natHelper.externalIP, Network.player.ipAddress, 0, 0, OnMatchCreate);
		// List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
		// formData.Add(new MultipartFormDataSection("externalIP=" + natHelper.externalIP + "&internalIP=" + Network.player.ipAddress + "&guid=" + guid + "&roomName=Test&username=Desynched&password=moon"));
		//formData.Add(new MultipartFormFileSection("my file data", "myfile.txt"));

		// UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080/heartbeat", formData);
		// yield return www.SendWebRequest();

		WWWForm form = new WWWForm();
		form.AddField("roomName", "Test");

		// Upload to a cgi script
		UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080/heartbeat", form);
		yield return www.Send();
		if (www.error != null) {
			Debug.Log(www.error);
		} else {
			Debug.Log("Sent heartbeat!");
		}
	}

	public void tick(float delta) {
		if (isServer && living) {

			time += delta * speed;

			while (time > 1) {
				time -= 1;
				MoveForward();
			}

			// TODO: Move all segments forward 
		}
	}

	private void MoveForward() {
		Vector3 head = transform.position;

		RaycastHit hit;
		if (Physics.Raycast(head, dir, out hit, 1f, Physics.DefaultRaycastLayers)) {
			if (hit.transform.GetComponent<Apple>() != null) {
				length = (int)(length * 1.5f);
				Control.main.MoveApple(hit.transform);
			} else {
				living = false;
				color = Color.black;
				UpdateDisplayColor();
			}

		}

		Vector3 lastTail = segments[segments.Count - 1];

		head += dir;
		transform.position = head = head.floor();

		segments.Insert(0, head);

		if (segments.Count > length) {
			segments.RemoveAt(segments.Count - 1);
		}
	}

}
