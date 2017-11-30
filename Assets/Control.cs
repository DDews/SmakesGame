using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Control : NetworkBehaviour {

    public static Control main;

    public List<Player> players;

    public Transform applePrefab;

	internal bool boundaryShown = false;
	public Transform boundary;
	
    public List<Transform> appleDisplays;

	public Transform boundaryPrefab;


	[SyncVar] public float roundResultsTime = 5f;
	[SyncVar] public int rounds = 1;
    [SyncVar] public int numApples = 1;
    public SyncListVector3 applePositions;

    [SyncVar] public int size = 300;

	[SyncVar] public bool addTeleports = false;
	[SyncVar] public bool wrapping = true;
	[SyncVar] public bool roundActive = true;
	[SyncVar] public bool disappearOnDeath = false;
	[SyncVar] public bool respawn = true;
	[SyncVar] public bool allowFast = true;
	[SyncVar] public bool allowSlow = true;
	[SyncVar] public int living = 0;

    internal float tick = 0f;
    internal float tickRate = 0.05f;

    void OnEnable()
    {
		main = this;
    }
    void OnDisable()
    {
    }
    
	void Start () {
	}
	
	void Update () {
		if (wrapping && boundaryShown) {
			Debug.Log("hiding boundary");
			Destroy(boundary.gameObject);
			boundaryShown = false;
		} else if (!wrapping && !boundaryShown) {
			Debug.Log("showing boundary");
			boundaryShown = true;
			ShowBoundary();
		}
        Camera.main.orthographicSize = size;
		if (isServer) {
			if (roundActive) {
				tick += Time.deltaTime;
				while (tick > tickRate)
				{
					tick -= tickRate;
					foreach (Player p in players)
					{
						if (p != null) p.Tick(tickRate,false);
					}
		}
	}			else {
				tick += Time.deltaTime;
				if (tick > roundResultsTime) {
					foreach (Player p in players) 
					{
						if (p != null) {
							p.apples = 0;
							p.Respawn();
						}
					}
					rounds++;
					roundActive = true;
					tick = 0f;
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
			foreach (Player p in players) 
			{
				if (p != null) {
					if (addTeleports)
					{
						p.teleports++;
					}
					else
					{
						p.teleports = 1;
					}
				}
			}
			
        }
    }

	public void TeleportApple() {
		for (int i = 0; i < applePositions.Count; i++) {
			applePositions[i] = new  Vector3(Random.Range(-size,size),Random.Range(-size,size),0);
		}
	}
	internal void ShowBoundary() {
		boundary = Instantiate(boundaryPrefab);
		LineRenderer lineRenderer = boundary.GetComponent<LineRenderer>();
		lineRenderer.widthMultiplier = 1f;
		lineRenderer.positionCount = 5;
		Vector2 bounds = Camera.main.BoundsMax();
		float w = bounds.x;
		float h = bounds.y;
		lineRenderer.SetPosition(0, new Vector3(-w, h, 0));
		lineRenderer.SetPosition(1, new Vector3(w, h, 0));
		lineRenderer.SetPosition(2, new Vector3(w, -h, 0));
		lineRenderer.SetPosition(3, new Vector3(-w, -h, 0));
		lineRenderer.SetPosition(4, new Vector3(-w, h, 0));
		// A simple 2 color gradient with a fixed alpha of 1.0f.
		float alpha = 1.0f;
		Gradient gradient = new Gradient();
		gradient.SetKeys(
			new GradientColorKey[] { new GradientColorKey(Color.red, 1.0f), new GradientColorKey(Color.red, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey(alpha, 1.0f), new GradientAlphaKey(alpha, 1.0f) }
			);
		lineRenderer.colorGradient = gradient;
	}
	public void Reset() {
		foreach(Transform p in appleDisplays) {
			Destroy(p.gameObject);
		}
		appleDisplays.Clear();
		foreach(Player p in players) {
			Destroy(p.gameObject);
		}
		players.Clear();
		if (boundary != null) Destroy(boundary.gameObject);
		boundary = null;
	}
}


public static class Helpers
{
    public static Vector3 round(this Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));
    }
	public static Vector2 ToVector2(this Vector3 v) {
		return new Vector2(v.x,v.y);
	}
	public static Vector2 BoundsMin(this Camera camera) {
		return camera.transform.position.ToVector2() - camera.Extents();
	}

	public static Vector2 BoundsMax(this Camera camera) {
		return camera.transform.position.ToVector2() + camera.Extents();
	}

	public static Vector2 Extents(this Camera camera) {
		if (camera.orthographic)
			return new Vector2(camera.orthographicSize * Screen.width / Screen.height, camera.orthographicSize);
		else {
			Debug.LogError("Camera is not orthographic!", camera);
			return new Vector2();
		}
	}
}
