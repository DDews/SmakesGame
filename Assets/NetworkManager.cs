#if !DISABLE_NAT_TRAVERSAL
#if UNITY_5_4 || UNITY_5_5 || UNITY_5_6 || UNITY_5_7 || UNITY_5_8 || UNITY_5_9 || UNITY_6 || UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define NEW_STUFF
#endif
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Types;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
public class NetworkManager : UnityEngine.Networking.NetworkManager {

	public NATHelper natHelper;
	FieldInfo clientIDField;
    NetworkMatch networkMatch;
	public static List<String> guids = new List<String>();
	public static List<String> externalIPs = new List<String>();
	public static List<int> externalPorts = new List<int>();
	public static List<int> internalPorts = new List<int>();
	public List<NetworkServerSimple> natServers = new List<NetworkServerSimple>();
	public string hostExternalIP, hostInternalIP;
	public static string debugMsg = "";
    public static NetworkManager main = null;
	public bool GUIEnabled = true;
	public Texture2D bg;
	public bool options = false;
	public bool optionsEnabled = false;
	public static System.Random random;
	public string roomName;
	public string joinRoom = "Room Name";
	public bool showScore = false;

    void Awake() {
		Screen.SetResolution(800, 600, false);
		random = new System.Random();
		main = this;
		//networkMatch = gameObject.AddComponent<NetworkMatch>();
		clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);

		natHelper = GetComponent<NATHelper>();
	}
    public void OnGUI()
    {
		GUI.enabled = natHelper.isReady;
		GUIStyle bgStyle = new GUIStyle();
		bgStyle.normal.background = bg;
		GUILayout.BeginArea(new Rect(0,Screen.width / 6  * 4,Screen.width,Screen.height / 6));
		GUILayout.BeginHorizontal();
		GUIStyle debugStyle = GUI.skin.GetStyle("Label");
		debugStyle.alignment = TextAnchor.LowerRight;
		GUILayout.Label(debugMsg, debugStyle);
		GUILayout.EndVertical();
		GUILayout.EndArea();
		if (Control.main != null && !Control.main.roundActive) {
			List<Player> winners = new List<Player>();
			int highest = 0;
			foreach(Player p in Control.main.players) {
				if (p != null) {
					if (p.apples > highest) {
						winners.Clear();
						winners.Add(p);
						highest = p.apples;
					} else if (p.apples == highest) {
						winners.Add(p);
					}
				}
			}
			GUILayout.BeginArea(new Rect(Screen.width / 6f,Screen.height / 6f,Screen.width / 1.5f, Screen.height / 1.5f),bgStyle);
			GUIStyle style = GUI.skin.GetStyle("Label");
			style.normal.textColor = Color.white;
			style.alignment = TextAnchor.UpperCenter;
			style.richText = true;
			GUILayout.BeginVertical();
			GUILayout.Label("<size=50>ROUND " + Control.main.rounds + "</size>", style);
			if (winners.Count == 1) {
				style.normal.textColor = winners[0].oldColor;
				GUILayout.Label("<size=50>WINNER</size>",style);
			}
			else GUILayout.Label("<size=50>WINNERS</size>",style);
			GUILayout.Label("",style);
			foreach (Player p in winners) {
				if (p != null) {
					style.normal.textColor = p.oldColor;
					GUILayout.Label("<size=50>" + p.apples + " apples, " + p.kills + " kills</size>",style);
				}
			}
			GUILayout.EndVertical();
			GUILayout.EndArea();
			style.normal.textColor = Color.white;
		}
		else 
		{

			if (GUIEnabled) {
				joinRoom = GUI.TextField(new Rect(0,120,150,50), joinRoom, 25);
				if (GUI.Button(new Rect(0, 200, 150, 100), "Join room") || Event.current.keyCode == KeyCode.Return)
				{
					GUIEnabled = false;
					StartCoroutine(GetRooms());
					//matchMaker.ListMatches(0, 10, "", true, 0, 0, OnMatchList);
				}
			} else if (options) {
				GUILayout.BeginArea(new Rect(0,0,200,170),bgStyle);
				GUILayout.BeginVertical();
				bool newWrap = GUILayout.Toggle(Control.main.wrapping,"Wrap");
				if (newWrap != Control.main.wrapping) {
					if (optionsEnabled) {
						Control.main.wrapping = newWrap;
					}
				}
				bool newDisappear = GUILayout.Toggle(Control.main.disappearOnDeath, "Disappear on Death");
				if (newDisappear != Control.main.disappearOnDeath) {
					if (optionsEnabled) {
						Control.main.disappearOnDeath = newDisappear;
					}
				}
				bool newRespawn = GUILayout.Toggle(Control.main.respawn,"Respawn");
				if (newRespawn != Control.main.respawn) {
					if (optionsEnabled) 
					{
						Control.main.respawn = newRespawn;
					}
				}
				bool newAddTeleports = GUILayout.Toggle(Control.main.addTeleports, "Add To Teleports");
				if (newAddTeleports != Control.main.addTeleports) {
					if (optionsEnabled) {
						Control.main.addTeleports = newAddTeleports;
					}
				}
				bool newAllowFast = GUILayout.Toggle(Control.main.allowFast, "Allow Fast Speed");
				if (newAllowFast != Control.main.allowFast) {
					if (optionsEnabled) {
						Control.main.allowFast = newAllowFast;
					}
				}
				bool newAllowSlow = GUILayout.Toggle(Control.main.allowSlow, "Allow Slow Speed");
				if (newAllowSlow != Control.main.allowSlow) {
					if (optionsEnabled) {
						Control.main.allowSlow = newAllowSlow;
					}
				}
				GUILayout.EndVertical();
				GUILayout.EndArea();
			}
			if (Player.localPlayer != null) 
			{
				GUILayout.BeginArea(new Rect(0,0,Screen.width,Screen.height));
				GUIStyle rightStyle = GUI.skin.GetStyle("Label");
				rightStyle.alignment = TextAnchor.UpperRight;
				GUILayout.BeginVertical();
				GUILayout.Label("Room: " + roomName, rightStyle);
				GUILayout.EndVertical();
				GUILayout.EndArea();
				GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				GUIStyle style = GUI.skin.GetStyle("Label");
				style.alignment = TextAnchor.UpperCenter;
				GUILayout.BeginVertical();
				GUILayout.Label("ROUND " + Control.main.rounds, style);
				GUILayout.Label("Teleports: " + Player.localPlayer.teleports, style);
				GUILayout.EndVertical();
				GUILayout.EndArea();
			}
			if (showScore) {
				GUIStyle style = GUI.skin.GetStyle("Label");
				style.alignment = TextAnchor.MiddleCenter;
				style.richText = true;
				GUILayout.BeginArea(new Rect(Screen.width / 4,Screen.height / 4,Screen.width / 2, Screen.height / 2),bgStyle);
				GUILayout.BeginVertical();
				foreach(Player p in Control.main.players) {
					if (p != null) {
						style.normal.textColor = p.color;
						GUILayout.Label(p.apples + " apples, " + p.kills + " kills", style);
					}
				}
				style.normal.textColor = Color.white;
				GUILayout.EndVertical();
				GUILayout.EndArea();
			}
		}
    }
	private string ifTrue(bool value, string ifTrue, string ifFalse) {
		if (value) return ifTrue;
		else return ifFalse;
	}

    IEnumerator GetRooms()
    {
        //networkMatch.ListMatches(0, 1, "", true, 0, 0, OnMatchList);
        WWW www = new WWW("http://smakes.io:8080");
        yield return www;
        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.text);

                JsonArray result = Json.Parse<JsonArray>(www.text.ToString());
                OnMatchList(result, joinRoom);
                // Or retrieve results as binary data
                //byte[] results = www.downloadHandler.data;
        }
    }

   IEnumerator CreateRoom(string name)
    {
        string guid = natHelper.guid;

        // The easiest way I've found to get all the connection data to the clients is to just
        // mash it all together in the match name
        //string name = string.Join(":", new string[] { natHelper.externalIP, Network.player.ipAddress, guid });

        //networkMatch.CreateMatch(name, 2, true, "", natHelper.externalIP, Network.player.ipAddress, 0, 0, OnMatchCreate);
        // List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        // formData.Add(new MultipartFormDataSection("externalIP=" + natHelper.externalIP + "&internalIP=" + Network.player.ipAddress + "&guid=" + guid + "&roomName=Test&username=Desynched&password=moon"));
        //formData.Add(new MultipartFormFileSection("my file data", "myfile.txt"));

        // UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080/api/createMatch", formData);
        // yield return www.SendWebRequest();

		if (name == null || name == "")
		{
			roomName = RandomString(10); 
		}
		else
		{
			roomName = name;
		}
		WWWForm form = new WWWForm();
        form.AddField("externalIP", natHelper.externalIP);
        form.AddField("internalIP", Network.player.ipAddress);
        form.AddField("guid", guid);
        form.AddField("roomName", roomName);
        form.AddField("username", "Desynched");
        form.AddField("password","moon");

        // Upload to a cgi script
        UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080", form);
        yield return www.Send();
        if (www.error != null)
        {
			GUIEnabled = true;
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
            natHelper.startListeningForPunchthrough(OnHolePunchedServer);
			StartHost();
        }
    }
	/**
     * We start a host as normal but we also tell the NATHelper to start listening for incoming punchthroughs. 
     */
	/*public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo) {
		// It's important here that we host via NetworkServer.ListenRelay otherwise the match will
		// be delisted from the UNet matchmaking service after 30 seconds and clients will not be
		// able to find the game.
		// Luckily this is exactly what unity does by default in OnMatchCreate so it's all good
		base.OnMatchCreate(success, extendedInfo, matchInfo);

		natHelper.startListeningForPunchthrough(OnHolePunchedServer);
	}*/

	/**
     * We start a host as normal but we also tell the NATHelper to start listening for incoming punchthroughs. 
     */


	internal void OnMatchList(JsonArray rooms, string name)
    {
        foreach (JsonObject room in rooms)
        {
			if (room.GetString("roomName").Equals(name)) {
				roomName = room.GetString("roomName");
				hostExternalIP = room.GetString("externalIP");
				hostInternalIP = room.GetString("internalIP");
				string hostGUID = room.GetString("guid");
				natHelper.punchThroughToServer(hostGUID, OnHolePunchedClient); 
				return;
			}
        }
		StartCoroutine(CreateRoom(name));
    }
	/**
     * Punches a hole through to the host of the first match in the list
     */
//#if NEW_STUFF
    /*public override void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        int matchCount = matchList.Count;
        MatchInfoSnapshot match = null;
#else
	public override void OnMatchList(ListMatchResponse matchList) {
		bool success = matchList.success;
		int matchCount = matchList.matches.Count;
		MatchDesc match = null;
#endif
		if (!success) {
			Debug.Log("Failed to retrieve match list");
			return;
		}

		if (matchCount == 0) {
			Debug.Log("Match list is empty");
			roomName = joinRoom;
			StartHostAll(joinRoom, customConfig ? (uint)(maxConnections + 1) : matchSize);
			return;
		}

#if NEW_STUFF
		foreach (MatchInfoSnapshot m in matchList) {
			if (m.name == joinRoom) match = m;
		}
        
#else
		match = matchList.matches[0];
#endif

		if (match == null) {
			Debug.Log("Match list is empty");
			roomName = joinRoom;
			StartHostAll(joinRoom, customConfig ? (uint)(maxConnections + 1) : matchSize);
			return;
		}

		Debug.Log("Found a match, joining");
		roomName = joinRoom;
		matchID = match.networkId;
		StartClientAll(match);
	}*/

	/*public override void OnDoneConnectingToFacilitator(ulong guid) {
		base.OnDoneConnectingToFacilitator(guid);
		if (guid == 0) {
			Debug.Log("Failed to connect to Facilitator");
		} else {
			Debug.Log("Facilitator connected");
			StartCoroutine(getExternalIP());
		}
	}*/

	private void OnTestMessage(NetworkMessage netMsg) {
		Debug.Log("Received test message");
	}

	public override void OnServerReady(NetworkConnection conn) {
		base.OnServerReady(conn);
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
		Debug.Log("on server add player: " + playerControllerId);
		base.OnServerAddPlayer(conn, playerControllerId);
	}

	public override void OnServerConnect(NetworkConnection conn) {
		base.OnServerConnect(conn);
		NetworkServer.RegisterHandler(MsgType.OtherTestMessage, OnTestMessage);
	}
	/*public void StartMatch() {
		if (matchMaker == null) matchMaker = gameObject.AddComponent<NetworkMatch>();
		StartMatchMaker();
		roomName = joinRoom;
		StartHostAll(joinRoom, customConfig ? (uint)(maxConnections + 1) : matchSize);
	}*/
	/*public override void OnClientConnect(NetworkConnection conn) {
		base.OnClientConnect(conn);
		RegisterHandlerClient(MsgType.OtherTestMessage, OnTestMessage);
	}*/
	public void JoinRoom() {
		if (matchMaker == null) matchMaker = gameObject.AddComponent<NetworkMatch>();
		StartMatchMaker();
		matchMaker.ListMatches(0, 10, joinRoom, true, 0, 0, OnMatchList);
	}
	public void Disconnect() {
		if (NetworkServer.active) {
			NetworkServer.SetAllClientsNotReady();
			StopHost();
		} else {
			StopClient();
		}
	}

	/*void Update()
    {
        foreach(NATServer server in natServers) {
			if (server != null)
			{
				try {
					server.Update();

				} catch (Exception e) {
					Debug.Log(e.StackTrace);
				}
			}
		}
    }*/
	public static string RandomString(int length) {
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		return new string(Enumerable.Repeat(chars, length)
		  .Select(s => s[random.Next(s.Length)]).ToArray());
	}
	/*public override void OnHolePunchedClient(int natListenPort, int natConnectPort, bool success) {
		base.OnHolePunchedClient(natListenPort, natConnectPort, success);
		networkPort = natConnectPort;

		// Make sure to connect to the correct IP or things won't work
		if (hostExternalIP == externalIP) {
			if (hostInternalIP == Network.player.ipAddress) {
				// Host is running on the same computer as client, two separate builds
				networkAddress = "127.0.0.1";
			} else {
				// Host is on the same local network as client
				networkAddress = hostInternalIP;
			}
		} else {
			// Host is somewhere out on the internet
			networkAddress = hostExternalIP;
		}

		Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

		// Standard client setup stuff than the NetworkMager would normally take care of for us
		NetworkTransport.Init(globalConfig);
		if (customConfig) {
			foreach (QosType type in base.channels) {
				connectionConfig.AddChannel(type);
			}
		} else {
			connectionConfig.AddChannel(QosType.ReliableSequenced);
			connectionConfig.AddChannel(QosType.Unreliable);
		}

		// If we try to use maxConnection when the Advanced Configuration checkbox is not checked
		// we will get a crc mismatch because the host will have been started with the default
		// max players of 8 rather than the value in maxConnections
		int maxPlayers = 8;
		if (customConfig) {
			maxPlayers = maxConnections;
		}

		HostTopology topo = new HostTopology(connectionConfig, maxPlayers);

		// Start up a transport level host on the port that the hole was punched from
		int natListenSocketID = NetworkTransport.AddHost(topo, natListenPort);

		// Create and configure a new NetworkClient
		client = new NetworkClient();
		client.Configure(topo);

		// Connect to the port on the server that we punched through to
		client.Connect(networkAddress, networkPort);

		// Magic! Set the client's transport level host ID so that the client will use
		// the host we just started above instead of the one it creates internally when we call Connect.
		// This has to be done so that the connection will be made from the correct port, otherwise
		// Unity will use a random port to connect from and NAT Punchthrough will fail.
		// This is the shit that keeps me up at night.
		clientIDField.SetValue(client, natListenSocketID);

		// Tell Unity to use the client we just created as _the_ client so that OnClientConnect will be called
		// and all the other HLAPI stuff just works. Oh god, so nice.
		UseExternalClient(client);
		StartClient();
	}
	public override void OnHolePunchedServer(int natListenPort, ulong clientGUID) {
		base.OnHolePunchedServer(natListenPort, clientGUID);
		//StartHost();
	}*/
	/**
	* Server received a hole punch from a client
	* Start up a new NATServer listening on the newly punched hole
	*/
	void OnHolePunchedServer(int natListenPort) {
		NATServer newServer = new NATServer();
		bool isListening = newServer.Listen(natListenPort, NetworkServer.hostTopology);
		if (isListening) {
			natServers.Add(newServer);
		}
	}
	/**
	* Client punched through to a server
	* Perform sacred ritual to create a custom client and attempt to connect
	* through the hole.
	*/
	void OnHolePunchedClient(int natListenPort, int natConnectPort) {
		// The port on the server that we are connecting to
		networkPort = natConnectPort;

		// Make sure to connect to the correct IP or things won't work
		if (hostExternalIP == natHelper.externalIP) {
			if (hostInternalIP == Network.player.ipAddress) {
				// Host is running on the same computer as client, two separate builds
				networkAddress = "127.0.0.1";
			} else {
				// Host is on the same local network as client
				networkAddress = hostInternalIP;
			}
		} else {
			// Host is somewhere out on the internet
			networkAddress = hostExternalIP;
		}

		Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

		// Standard client setup stuff than the NetworkMager would normally take care of for us
		NetworkTransport.Init(globalConfig);
		if (customConfig) {
			foreach (QosType type in base.channels) {
				connectionConfig.AddChannel(type);
			}
		} else {
			connectionConfig.AddChannel(QosType.ReliableSequenced);
			connectionConfig.AddChannel(QosType.Unreliable);
		}

		// If we try to use maxConnection when the Advanced Configuration checkbox is not checked
		// we will get a crc mismatch because the host will have been started with the default
		// max players of 8 rather than the value in maxConnections
		int maxPlayers = 8;
		if (customConfig) {
			maxPlayers = maxConnections;
		}

		HostTopology topo = new HostTopology(connectionConfig, maxPlayers);

		// Start up a transport level host on the port that the hole was punched from
		int natListenSocketID = NetworkTransport.AddHost(topo, natListenPort);

		// Create and configure a new NetworkClient
		client = new NetworkClient();
		client.Configure(topo);

		// Connect to the port on the server that we punched through to
		client.Connect(networkAddress, networkPort);

		// Magic! Set the client's transport level host ID so that the client will use
		// the host we just started above instead of the one it creates internally when we call Connect.
		// This has to be done so that the connection will be made from the correct port, otherwise
		// Unity will use a random port to connect from and NAT Punchthrough will fail.
		// This is the shit that keeps me up at night.
		clientIDField.SetValue(client, natListenSocketID);

		// Tell Unity to use the client we just created as _the_ client so that OnClientConnect will be called
		// and all the other HLAPI stuff just works. Oh god, so nice.
		UseExternalClient(client);

		//StartClient();

	}
	void Update() {
		natServers.ForEach(server => server.Update());
	}
}
class MsgType : NATTraversal.MsgType {
	public static short OtherTestMessage = Highest + 1;
}
#endif
