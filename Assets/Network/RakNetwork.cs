using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RakNetwork : MonoBehaviour {
    NATHelper natHelper;
    public int networkPort;
    public Connection host;
    public List<Connection> clients;
    public string msg;
	// Use this for initialization
	void Start () {
        natHelper = GetComponent<NATHelper>();
        clients = new List<Connection>();
        msg = "nothing";
	}
	void OnGUI()
    {
        GUI.enabled = natHelper.isReady;
        if (GUI.Button(new Rect(0, 10, 150, 100), "Host"))
        {
            // This is how RakNet identifies each peer in the network
            // Clients will use the guid of the server to punch a hole
            StartCoroutine(CreateRoom());


        }
        if (GUI.Button(new Rect(0, 120, 150, 100), "Join server"))
        {
            StartCoroutine(GetRooms());
        }
        GUI.Label(new Rect(100, 10, 150, 100), msg);

        if (host != null)
        {
            GUI.Label(new Rect(200, 200, 150, 100), "" + host);
        }

        if (clients.Count > 0)
        {
            Rect brush = new Rect(400, 200, 150, 100);
            foreach (var client in clients)
            {
                GUI.Label(brush, "" + client);
            }
        }
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

            JsonArray result = Json.Parse(www.text.ToString()) as JsonArray;
            OnMatchList(result);
            // Or retrieve results as binary data
            //byte[] results = www.downloadHandler.data;
        }
    }

    IEnumerator CreateRoom()
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

        WWWForm form = new WWWForm();
        form.AddField("externalIP", natHelper.externalIP);
        form.AddField("internalIP", Network.player.ipAddress);
        form.AddField("guid", guid);
        form.AddField("roomName", "Test");
        form.AddField("username", "Desynched");
        form.AddField("password", "moon");

        // Upload to a cgi script
        UnityWebRequest www = UnityWebRequest.Post("http://35.193.1.47:8080", form);
        yield return www.Send();
        if (www.error != null)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
            natHelper.startListeningForPunchthrough(OnHolePunchedServer);
            // StartHost();
        }
    }
    internal void OnMatchList(JsonArray rooms)
    {
        if (rooms.Count > 0)
        {
            JsonObject room = rooms[0] as JsonObject;
            string hostExternalIP = room.Get<string>("externalIP");
            string hostInternalIP = room.Get<string>("internalIP");
            string hostGUID = room.Get<string>("guid");
            host = new Connection(hostExternalIP, hostInternalIP);
            natHelper.punchThroughToServer(hostGUID, OnHolePunchedClient);
        }
        else
        {
            Debug.Log("Error: no rooms found!");
        }
    }

    /**
     * Client punched through to a server
     * Perform sacred ritual to create a custom client and attempt to connect
     * through the hole.
     */
    void OnHolePunchedClient(int natListenPort, int natConnectPort)
    {
        // The port on the server that we are connecting to
        networkPort = natConnectPort;
        host.listenPort = natListenPort;
        host.connectPort = natConnectPort;
        // Make sure to connect to the correct IP or things won't work
        if (host.externalIP == natHelper.externalIP)
        {
            if (host.internalIP == Network.player.ipAddress)
            {
                // Host is running on the same computer as client, two separate builds
                host.externalIP = "127.0.0.1";
            }
            else
            {
                // Host is on the same local network as client
                host.externalIP = host.internalIP;
            }
        }
        else
        {
            // Host is somewhere out on the internet
            host.externalIP = natHelper.externalIP;
        }

        Debug.Log("Attempting to connect to server " + host.externalIP + ":" + networkPort);

        byte[] arr = host.Receive();
        arr = host.Receive();
        msg = Encoding.ASCII.GetString(arr);

        host.Send(Encoding.ASCII.GetBytes("You're a dummy"));


    }
    /**
   * Server received a hole punch from a client
   * Start up a new NATServer listening on the newly punched hole
   */
    void OnHolePunchedServer(int natListenPort)
    {
        // clients.Add(new Connection(ipAddress, ipAddress, natListenPort, natListenPort));
         
        // clients[0].Send(Encoding.ASCII.GetBytes("You're a complete loser."));

        byte[] arr = clients[0].Receive();
        msg = Encoding.ASCII.GetString(arr);
    }

    // Update is called once per frame
    void Update () {
		
	}

}
