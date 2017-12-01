using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SyncListVector3 : SyncListStruct<Vector3> { }
/**
 * Most basic player possible. Use arrow keys to move around.
 */
public class Player : NetworkBehaviour {
	internal Vector3 willChange = Vector3.zero;
	[SyncVar]
	internal Color color = Color.grey;
	internal Color _color = Color.grey;
	internal Vector3 oldDir = Vector3.zero;

	[SyncVar]
	internal bool respawning = false;

	[SyncVar]
	internal Color oldColor = Color.grey;
	[SyncVar]
	public int kills = 0;


	internal SyncListVector3 joints;
	[SyncVar]
	internal Vector3 dir;
	internal Vector3 ghostDir = Vector3.zero;
	[SyncVar]
	public int apples;

	[SyncVar]
	public Color killer;
	[SyncVar]
	internal short teleports = 1;

	public static Player localPlayer;

	internal Vector3 lastDir;

	[SyncVar]
	public bool living = true;

	[SyncVar] public int fastSpeed = 10;
	[SyncVar] public int midSpeed = 5;
	[SyncVar] public int slowSpeed = 2;
	[SyncVar] public int speed = 5;

	internal float tickRate = 0.05f;
	public float tick = 0;
	internal Vector3 wantedDir = Vector3.zero;
	

	public List<Vector3> segments = new List<Vector3>();

	public List<Vector3> mySegs;

	public float pingDelta = 0;

	List<Transform> displays;

	public Transform segmentPrefab;

	internal float heartbeat = 0;
	[SyncVar] public int size = 0;
	[SyncVar] public int length = 10;
	internal int myIndex;
	internal float time = 0;

	public void Respawn() {
		respawning = true;
		killer = Color.black;
		kills = 0;
		length = 10;
		size = 0;
		transform.position = new Vector3(Random.Range(-Control.main.size, Control.main.size), Random.Range(-Control.main.size, Control.main.size),0).round();
		segments.Clear();
		joints.Clear();
		joints.Add(transform.position);
		segments.Add(transform.position);
		color = oldColor;
		teleports = 1;
	}
	void ClearSegments() {
		mySegs.Clear();
		foreach (Vector3 d in segments) {
			mySegs.Add(d);
		}
	}
	void Start() {
		displays = new List<Transform>();
		if (!isServer) CmdSpawn(Network.player.ipAddress, NetworkManager.main.networkPort);
	}
	void StartServer() {
		CmdSpawn(Network.player.ipAddress, NetworkManager.main.networkPort);
	}
	public override void OnStartServer() {
		base.OnStartServer();
		color = new Color(Random.value, Random.value, Random.value);
	}
	public override void OnStartClient() {
		base.OnStartClient();
		Control.main.players.Add(this);
	}
	public override void OnNetworkDestroy() {
		base.OnNetworkDestroy();
	}
	void OnEnable() {
	}
	void OnApplicationQuit() {
		if (isServer) {
			CloseRoom();
		}
		Despawn();
	}
	void OnDisable() {
		Control.main.living--;
	}
	IEnumerator CloseRoom() {
		WWWForm form = new WWWForm();
		form.AddField("roomName", NetworkManager.roomName);
		form.AddField("password", NetworkManager.roomPassword);
		form.AddField("kill", "true");

		// Upload to a cgi script
		UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080", form);
		yield return www.Send();
		if (www.error != null) {
			Debug.Log(www.error);
		} else {
			Debug.Log("Form upload complete!");
		}
	}
	void Despawn() {
		if (Control.main) {
			Control.main.living--;
			Control.main.players[myIndex] = null;
		}
	}
	void SendGhost() {
		if (segments.Count < mySegs.Count && !living) {
			foreach (Transform p in displays) {
				Destroy(p.gameObject);
			}
			displays.Clear();
			mySegs.Clear();
			joints.Clear();
		}
		int indexToSend = -1;
		for (int i = 3; i < segments.Count; i++) {
			if (!mySegs.Contains(segments[i])) {
				indexToSend = i;
				break;
			}
		}
		if (indexToSend != -1) {
			for (int i = 0; i < mySegs.Count; i++) {
				CmdSendGhost(i, mySegs[i]);
			}
			//CmdSendHead(this.transform.position);
		}
	}
	void Update() {
		if (!isLocalPlayer && !isServer) NetworkManager.debugMsg = "" + segments.Count + ", " + size;
		if (!living && respawning && isLocalPlayer) {
			ClearSegments();
			CmdRespawn();
			respawning = false;
		}
		if (isServer) {
			heartbeat += Time.deltaTime;
			if (heartbeat > 15) {
				heartbeat = 0;
				StartCoroutine(SendHeartbeat());
			}
		} else {
			heartbeat += Time.deltaTime;
			if (heartbeat > 0.5) {
				heartbeat = 0;
				//CmdSetDirection(ghostDir, transform.position, mySegs.Count > 0 ? mySegs[mySegs.Count - 1] : transform.position);
			}
		}

		if (color != _color) {
			_color = color;
			UpdateDisplayColor();
		}
		if (!isLocalPlayer) return;
		if (localPlayer == null) localPlayer = this;


		if (ghostDir != Vector3.zero && ghostDir + lastDir != Vector3.zero && ghostDir != this.dir && !isServer) {
	
			CmdSetDirection(tick, ghostDir, transform.position, mySegs.Count > 0 ? mySegs[mySegs.Count - 1] : transform.position);
		}
		

		Vector3 dir = Vector3.zero;
		if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) 
		{
			dir = Vector3.up;
			if (wantedDir != Vector3.up) wantedDir = Vector3.up;
		}
		else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
		{
			dir = Vector3.down;
			if (wantedDir != Vector3.down) wantedDir = Vector3.down;
		}
		else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) 
		{
			dir = Vector3.left;
			if (wantedDir != Vector3.left) wantedDir = Vector3.left;
		}
		else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) 
		{
			dir = Vector3.right;
			if (wantedDir != Vector3.right) wantedDir = Vector3.right;
		}
		if (Input.GetKeyDown(KeyCode.E)) {
			CmdTeleportApple();
		}
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
			CmdSetSpeed(fastSpeed,transform.position);
		} else if (Input.GetKey(KeyCode.Space)) {
			CmdSetSpeed(slowSpeed,transform.position);
		} else if (speed != midSpeed) {
			CmdSetSpeed(midSpeed,transform.position);
		}
		if (Input.GetKey(KeyCode.Tab)) {
			NetworkManager.main.showScore = true;
		} else {
			NetworkManager.main.showScore = false;
		}
		if (Input.GetKeyDown(KeyCode.R)) {
			CmdDoSomething();
		}
		if (Input.GetKeyDown(KeyCode.F1)) {
			if (isServer) NetworkManager.main.optionsEnabled = true;
			else NetworkManager.main.optionsEnabled = false;

			NetworkManager.main.options = !NetworkManager.main.options;
		}
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (NetworkManager.main.quitClient || NetworkManager.main.quitScreen) {
				NetworkManager.main.quitScreen = NetworkManager.main.quitClient = false;
			}
			else {

				if (isServer) NetworkManager.main.quitScreen = true;
				else NetworkManager.main.quitClient = true;
			}
		}
		if (dir != Vector3.zero && dir + lastDir != Vector3.zero) 
		{
			this.ghostDir = dir;
			this.dir = dir;
			if (!isServer) CmdSetDirection(tick, dir,transform.position.round(), mySegs.Count > 0 ? mySegs[mySegs.Count - 1] : transform.position.round());
		}

		if (!isServer && living) {
			if (Control.main.roundActive) {
				tick += Time.deltaTime;
				while (tick > tickRate) {
					tick -= tickRate;
					Tick(tickRate, true);
				}
			}
		}
	}

	IEnumerator Ping() {
		string ip = "127.0.0.1";
		
		Ping ping = new Ping(ip);
		 while (!ping.isDone) {
		}
		 NetworkManager.debugMsg = ping.time + "ms";
		return null;
	}
	private void UpdateDisplayColor() {
		foreach (var renderer in GetComponentsInChildren<Renderer>()) {
			renderer.material.color = color;
		}
	}
	public void LateUpdate() {
		pingDelta += Time.deltaTime;
		/*if (pingDelta > 1) {
			pingDelta = 0;
			if (externalIPs.Count > 0) {
				string str = "";
				//NetworkManager.debugMsg = clients[clients.Count - 1] + " ip";
				//if (isServer) NetworkManager.debugMsg = NetworkManager.main.natHelper.rakPeer.GetAveragePing(new RakNet.RakNetGUID(System.Convert.ToUInt64(clients[clients.Count - 1],10))) + "ms";
				string str = "";
				foreach(NATServer s in NetworkManager.main.natServers) {
					if (s == null) continue;
					foreach(NetworkConnection c in s.connections) {
						if (c == null) continue;
						str += c.address.Split(null)[1];
					}
				}
		for (int i = 0; i < externalIPs.Count && i < internalPorts.Count; i++) {
					if (i < guids.Count) str += guids[i] + "\n";
					str += externalIPs[i] + ":" + externalPorts[i];
					if (i < externalIPs.Count && i < externalPorts.Count) str += "@" + internalPorts[i];
					str += "\n";
				}
				str += externalIPs.Count + ", " + externalPorts.Count + ", " + internalPorts.Count;
				NetworkManager.debugMsg = str;
				
			}
		}*/
		bool ghosted = false;
		if (!(isServer && isLocalPlayer)) {
			Vector2 bounds = Camera.main.BoundsMax();
			float w = bounds.x;
			
			float h = bounds.y;
		}
		if (!isServer && isLocalPlayer) ghosted = true;
		if (ghosted) {
			if (mySegs.Count < displays.Count || segments.Count < displays.Count) {
				foreach (Transform d in displays) {
					Destroy(d.gameObject);
				}
				displays.Clear();
				joints.Clear();
			}
		}
		if (!living) {
			if (Control.main.disappearOnDeath && displays.Count > 0)
			{
				foreach (Transform d in displays) 
				{
					Destroy(d.gameObject);
				}
				joints.Clear();
				displays.Clear();
			}
		}
		else 
		{
			if (ghosted) 
			{
				while (displays.Count < mySegs.Count) {
					Transform seg = Instantiate(segmentPrefab);
					seg.SetParent(transform);
					seg.GetComponent<Renderer>().material.color = color;
					displays.Add(seg);
				}
				if (displays.Count == mySegs.Count) {
					for (int i = 0; i < mySegs.Count; i++) {
						displays[i].position = mySegs[i];
					}
				}

			}
			else 
			{
				if (segments.Count < displays.Count) {
					foreach (Transform d in displays) {
						Destroy(d.gameObject);
					}
					displays.Clear();
				}
				if (isServer) {
					while (displays.Count < segments.Count) {
						Transform seg = Instantiate(segmentPrefab);
						seg.SetParent(transform);
						seg.GetComponent<Renderer>().material.color = color;
						displays.Add(seg);
					}
					if (isLocalPlayer) {
						if (displays.Count == segments.Count) {
							for (int i = 0; i < displays.Count; i++) {
								displays[i].position = segments[i].round();
							}
						}
					}
					else updateSegs();
				}
				else {
					if (size < displays.Count) {
						foreach (Transform p in displays) {
							Destroy(p.gameObject);
						}
						displays.Clear();
					}
					while (size > displays.Count) {
						Transform seg = Instantiate(segmentPrefab);
						seg.SetParent(transform);
						seg.GetComponent<Renderer>().material.color = color;
						displays.Add(seg);
					}
					updateSegs();
					/*while (displays.Count < segments.Count) {
						Transform seg = Instantiate(segmentPrefab);
						seg.SetParent(transform);
						seg.GetComponent<Renderer>().material.color = color;
						displays.Add(seg);
					}*/
					/*if (segments.Count == displays.Count) {
						for (int i = 0; i < segments.Count; i++) {
							displays[i].position = segments[i];
						}
					}*/
				}
			}
		}
	}
	[Command]
	void CmdSpawn(string internalIP, int port) {
		/*
		if (port != NetworkManager.main.networkPort) internalPorts.Add(port);
		if (NetworkManager.externalIPs.Count > externalIPs.Count) {
				for (int i = externalIPs.Count; i < NetworkManager.externalIPs.Count; i++) {
					externalIPs.Add(NetworkManager.externalIPs[i]);
				}
			}
			if (NetworkManager.externalPorts.Count > externalPorts.Count) {
				for (int i = externalPorts.Count; i < NetworkManager.externalPorts.Count; i++) {
					externalPorts.Add(NetworkManager.externalPorts[i]);
				}
			}
			if (NetworkManager.guids.Count > guids.Count) {
				for (int i = guids.Count; i < NetworkManager.guids.Count; i++) {
					guids.Add(NetworkManager.guids[i]);
				}
			}
		*/

		Control.main.living++;
		myIndex = Control.main.players.IndexOf(this);
		Vector2 bounds = Camera.main.BoundsMax();
		float x = bounds.x;
		float y = bounds.y;
		transform.position = new Vector3(Random.Range(-x, x), Random.Range(-y, y), 0);
		transform.position = transform.position.round();
	}
	[Command]
	void CmdRespawn() {
		Control.main.living++;
		respawning = false;
		living = true;
	}
	[Command]
	void CmdEat(int appleIndex) {
		length = (int)(length * 1.5f);
		Control.main.MoveApple(Control.main.appleDisplays[appleIndex]);
		apples++;
	}
	[Command]
	void CmdDie() {
		Control.main.living--;
		living = false;
		oldColor = color;
		color = Color.grey;
		UpdateDisplayColor();
		size = 0;
		length = 10;
		if (Control.main.living <= 0) {
			Debug.Log(Control.main.living);
			Control.main.roundActive = false;
		}
		if (Control.main.disappearOnDeath) {
			segments.Clear();
			joints.Clear();
		}
	}
	[Command]
	void CmdSendGhost(int index, Vector3 seg) {
		if (index < segments.Count) {
			segments[index] = seg;
		}
	}
	[Command]
	void CmdSendHead(Vector3 head) {
		segments[0] = head;
		this.transform.position = head;
	}
	[Command]
	void CmdTeleportApple() {
		if (teleports > 0) 
		{
			teleports--;
			Control.main.TeleportApple();
		}
	}
	[Command]
	void CmdDoSomething() {
		if (living) {
			color = new Color(Random.value, Random.value, Random.value);
			UpdateDisplayColor();
		}
	}
	[Command]
	void CmdSetDirection(float newTick, Vector3 dir, Vector3 pos, Vector3 tail) {
		if (dir == Vector3.zero || dir + lastDir == Vector3.zero) return;
		if (!isLocalPlayer) {
			if (joints.Count > 0) {
				while (size > length && joints.Count > size * 2) {
					joints.RemoveAt(joints.Count - 1);
				}
				if (dir != oldDir && Vector3.Distance(pos,joints[0]) >= 1) {
					oldDir = dir;
					//joints.RemoveAt(joints.Count - 1);
					joints.Insert(0,pos);
					//FixSegments();
				} else if (dir != oldDir) {
					willChange = dir;
				}
			} else {
				if (joints.Count == 0) {
					joints.Add(pos);
				}
				else if (dir != oldDir && Vector3.Distance(pos, joints[0]) >= 1) {
					oldDir = dir;
					//joints.RemoveAt(joints.Count - 1);
					joints.Insert(0, pos);
					//FixSegments();
				} else if (dir != oldDir) {
					willChange = dir;
				}
			}
			string str = "[";
				foreach (Vector3 v in joints) {
					str += v.ToString() + ", ";
				}
				str += "]";
				Debug.Log(str);
			//FixSegments();
			tick = Vector3.Distance(transform.position,pos) * tickRate * speed;
			Tick(tick,false);
			transform.position = pos.round();
		}
		this.dir = dir;
	}
	void updateSegs() {
		while (displays.Count < size) {
			Transform seg = Instantiate(segmentPrefab);
			seg.SetParent(transform);
			seg.GetComponent<Renderer>().material.color = color;
			displays.Add(seg);
		}
		while (displays.Count > size) {
			Destroy(displays[displays.Count - 1].gameObject);
			displays.RemoveAt(displays.Count - 1);
		}
		if (size > 0 && displays.Count == size && joints.Count > 0) {
			Vector3 offset = joints[0].round();
			displays[0].position = transform.position.round();
			if (size > 2) {
				int length = -1;
				int startJ = 0;
				if (Vector3.Distance(transform.position.round(),joints[0].round()) >= 1) {
					for (int i = 1, z = 0, j = startJ; i < displays.Count && j < joints.Count; i++) {
						Vector3 jointB = joints[j].round();
						Vector3 jointA = j == 0 ? Vector3.zero : joints[j - 1].round();
						int dx = 0;
						int dy = 0;
						if (j == startJ) {
							dx = -Mathf.RoundToInt(dir.x);
							dy = -Mathf.RoundToInt(dir.y);
						} else if (jointB.x == jointA.x) {
							if (jointB.y > jointA.y) dy = 1;
							else dy = -1;
						} else if (jointB.y == jointA.y) {
							if (jointB.x > jointA.x) dx = 1;
							else dx = -1;
						}
						if (dx == 0 && dy == 0) {
							Debug.LogError("Shouldn't be happening at index " + i + ", " + j);
							break;
						}
						Vector3 newPos = (displays[i - 1].position.round() + new Vector3(dx, dy, 0)).round();
						if (Vector3.Distance(displays[i].position, newPos) > 1) {
							displays[i].position = newPos;
						}
						z++;
						float newDis = Vector3.Distance(displays[i].position.round(), joints[j].round());
						if (length == -1 && newDis <= 2) {
							j++;
							while (newDis > 0) {
								i++;
								if (i < displays.Count) {
									newPos = (displays[i - 1].position.round() + new Vector3(dx, dy, 0)).round();
									if (Vector3.Distance(displays[i].position, newPos) > 1) {
										displays[i].position = newPos;
									}
								}
								newDis--;
							}
							if (j < joints.Count) {
								length = Mathf.RoundToInt(Vector2.Distance(joints[j], joints[j - 1]));
								z = 0;
							}
						} else if (length != -1 && z >= length) {
							j++;
							if (j < joints.Count) {
								length = Mathf.FloorToInt(Vector2.Distance(joints[j], joints[j - 1]));
								z = 0;
							}
						}
					}
				}
			}
		}
	}

	[Command]
	void CmdSetSpeed(int speed, Vector3 pos) {
		if (speed > midSpeed && !Control.main.allowFast) return;
		if (speed < midSpeed && !Control.main.allowSlow) return;
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
		form.AddField("roomName", NetworkManager.roomName);
		form.AddField("players",Control.main.players.Count);

		// Upload to a cgi script
		UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080/heartbeat", form);
		yield return www.Send();
		if (www.error != null) {
			Debug.Log(www.error);
		} else {
			Debug.Log("Sent heartbeat!");
		}
	}

	public void Tick(float delta, bool ghosted) {
		if (living) {
			time += delta * speed;
			while (time > 1) {
				time -= 1;
				MoveForward(ghosted);
			}

			// TODO: Move all segments forward 
		}
	}
	public void MoveForward(bool ghosted) {
		if (respawning || !living) return;
		Vector3 head = transform.position;
		if (ghosted || (isServer && isLocalPlayer)) {
			RaycastHit hit;
			if (Physics.Raycast(head, ghostDir, out hit, 1f, Physics.DefaultRaycastLayers)) {
				if (hit.transform.GetComponent<Apple>() != null) {
					CmdEat(Control.main.appleDisplays.IndexOf(hit.transform));
				} else {
					Debug.LogError("WTF: " + hit.transform.GetComponentInParent<Player>().color + ", " + hit.transform.position + ", " + transform.position);
					CmdDie();
					killer = hit.transform.GetComponentInParent<Player>().oldColor;
					Player k = hit.transform.GetComponentInParent<Player>();
					if (k != null && k != this) k.kills++;
					if (Control.main.respawn) Respawn();
					return;
				}

			}
			head += ghostDir;
			lastDir = ghostDir;
		}
		else {
			head += dir;
			lastDir = dir;
		}

		if (Control.main.wrapping) {
			if (Mathf.Abs(head.x) > Camera.main.BoundsMax().x) {
				head.x *= -1;
			}
			if (Mathf.Abs(head.y) > Camera.main.BoundsMax().y) {
				head.y *= -1;
			}
		} else {
			if (Mathf.Abs(head.x) > Camera.main.BoundsMax().x || Mathf.Abs(head.y) > Camera.main.BoundsMax().y)
			{
				if (ghosted || (isServer && isLocalPlayer)) {
					CmdDie();
					if (Control.main.respawn) Respawn();
					return;
				}
			}
		}
		transform.position = head = head.round();
		if (ghosted) {
			mySegs.Insert(0, head);
			size++;
			if (mySegs.Count > length) {
				size--;
				mySegs.RemoveAt(mySegs.Count - 1);
			}
		}
		else
		{
			size++;
			segments.Insert(0, head);
			if (segments.Count > length) {
				size--;
				segments.RemoveAt(segments.Count - 1);
			}
		}
	}

}
