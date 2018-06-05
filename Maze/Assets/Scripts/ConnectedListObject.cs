using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectedListObject : MonoBehaviour {
    
    private GameObject next = null;

    public void SetNext(GameObject _next)
    {
        next = _next;
    }

    public GameObject GetNext()
    {
        return next;
    }
   
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
