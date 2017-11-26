using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Reflection;
using System.Collections.Generic;
using System;

public class NetworkManager : UnityEngine.Networking.NetworkManager
{
    NATHelper natHelper;
    FieldInfo clientIDField;
    NetworkMatch networkMatch;
    string hostExternalIP, hostInternalIP;
    List<NetworkServerSimple> natServers = new List<NetworkServerSimple>();
    public static NetworkManager main;


    [Serializable]
    internal class JsonData
    {
        public string externalIP;
        public string internalIP;
        public string guid;
        public string roomName;
        public string username;
        public int heartbeat;
    }
    internal class JsonHelper
    {
        public static T[] getJsonArray<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }

    void Awake()
    {
        main = this;
        // We abuse unity's matchmaking system to pass around connection info but no match is ever actually joined
        networkMatch = gameObject.AddComponent<NetworkMatch>();

        // Use reflection to get a reference to the private m_ClientId field of NetworkClient
        // We're going to need this later to set the port that client connects from.
        // Even though it's called clientId it is actually the transport level host id that the NetworkClient
        // uses to connect
        clientIDField = typeof(NetworkClient).GetField("m_ClientId", BindingFlags.NonPublic | BindingFlags.Instance);

        natHelper = GetComponent<NATHelper>();
    }
    
    public void OnGUI()
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

                JsonData[] result = JsonHelper.getJsonArray<JsonData>(www.text.ToString());
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
        form.AddField("password","moon");

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

           StartHost();
        }
    }

    #region Network events ------------------------------------------------------------------------

    /**
     * We start a host as normal but we also tell the NATHelper to start listening for incoming punchthroughs. 
     */
    public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        // It's important here that we host via NetworkServer.ListenRelay otherwise the match will
        // be delisted from the UNet matchmaking service after 30 seconds and clients will not be
        // able to find the game.
        // Luckily this is exactly what unity does by default in OnMatchCreate so it's all good
        base.OnMatchCreate(success, extendedInfo, matchInfo);

        natHelper.startListeningForPunchthrough(OnHolePunchedServer);
    }


    internal void OnMatchList(JsonData[] rooms)
    {
        if (rooms.Length > 0)
        {
            JsonData room = rooms[0];
            hostExternalIP = room.externalIP;
            hostInternalIP = room.internalIP;
            string hostGUID = room.guid;
            natHelper.punchThroughToServer(hostGUID, OnHolePunchedClient);
        } else
        {
            Debug.Log("Error: no rooms found!");
        }
    }
    /**
     * Punches a hole through to the host of the first match in the list
     */
    public override void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (!success || matchList.Count == 0) return;

        string[] data = matchList[0].name.Split(':');
        
        hostExternalIP = data[0];
        hostInternalIP = data[1];
        string hostGUID = data[2];

        natHelper.punchThroughToServer(hostGUID, OnHolePunchedClient);

        // This would be a good time to try and make a direct connection without NAT Punchthrough
        // If the connection succeeds then the code in OnHolePunchedClient isn't necessary
    }

    /**
     * Server received a hole punch from a client
     * Start up a new NATServer listening on the newly punched hole
     */
    void OnHolePunchedServer(int natListenPort)
    {
        NATServer newServer = new NATServer();

        bool isListening = newServer.Listen(natListenPort, NetworkServer.hostTopology);
        if (isListening)
        {
            natServers.Add(newServer);
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

        // Make sure to connect to the correct IP or things won't work
        if (hostExternalIP == natHelper.externalIP)
        {
            if (hostInternalIP == Network.player.ipAddress)
            {
                // Host is running on the same computer as client, two separate builds
                networkAddress = "127.0.0.1";
            }
            else
            {
                // Host is on the same local network as client
                networkAddress = hostInternalIP;
            }
        }
        else
        {
            // Host is somewhere out on the internet
            networkAddress = hostExternalIP;
        }
        
        Debug.Log("Attempting to connect to server " + networkAddress + ":" + networkPort);

        // Standard client setup stuff than the NetworkMager would normally take care of for us
        NetworkTransport.Init(globalConfig);
        if (customConfig)
        {
            foreach (QosType type in base.channels)
            {
                connectionConfig.AddChannel(type);
            }
        }
        else
        {
            connectionConfig.AddChannel(QosType.ReliableSequenced);
            connectionConfig.AddChannel(QosType.Unreliable);
        }

        // If we try to use maxConnection when the Advanced Configuration checkbox is not checked
        // we will get a crc mismatch because the host will have been started with the default
        // max players of 8 rather than the value in maxConnections
        int maxPlayers = 8;
        if (customConfig)
        {
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

    #endregion Network events ---------------------------------------------------------------------

    void Update()
    {
        natServers.ForEach(server => server.Update());
    }

}
