using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {
    
    private SpriteRenderer renderer;

	// Use this for initialization
	void Start () {
        renderer = GetComponent<SpriteRenderer>();
        renderer.enabled = false;
	}

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player" && gameObject.activeSelf)
        {
            //make it invisible
            renderer.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            //make it visible
            renderer.enabled = true;
        }
    }

        // Update is called once per frame
        void Update () {
		
	}
}
