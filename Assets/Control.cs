using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Control : NetworkBehaviour {

    public static Control main;

    public List<Player> players;

    public Transform applePrefab;
    public List<Transform> appleDisplays;

    [SyncVar] public int numApples = 1;
    [SyncVar] public SyncListVector3 applePositions;

    [SyncVar] public int size = 50;

    internal float tick = 0f;
    internal float tickRate = 0.05f;

    void OnEnable()
    {
        main = this;
    }
    void OnDisable()
    {
        main = null;
    }
    
	void Start () {
	}
	
	void Update () {

        Camera.main.orthographicSize = size;

        if (isServer)
        {

        tick += Time.deltaTime;
        while (tick > tickRate)
        {
            tick -= tickRate;
            foreach (Player p in players)
            {
                if (p != null) p.tick(tickRate);
            }
        }

        }
    }

    public override void OnStartServer()
    {
        for (int i = 0; i < numApples; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), 0);
            applePositions.Add(pos);
        }
    }

    void LateUpdate()
    {
        while (appleDisplays.Count < applePositions.Count)
        {
            appleDisplays.Add(Instantiate(applePrefab));
        }
        for (int i = 0; i < appleDisplays.Count; i++)
        {
            appleDisplays[i].position = applePositions[i];
        }
    }

    public void MoveApple(Transform appleTransform)
    {
        int index = appleDisplays.IndexOf(appleTransform);
        if (index > -1)
        {
            applePositions[index] = new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), 0);
        }
    }

}


public static class Helpers
{
    public static Vector3 floor(this Vector3 v)
    {
        return new Vector3(Mathf.Floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z));
    }
}
