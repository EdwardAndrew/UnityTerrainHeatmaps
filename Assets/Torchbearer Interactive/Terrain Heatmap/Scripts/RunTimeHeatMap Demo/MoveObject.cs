using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObject : MonoBehaviour {

    public float moveSpeed = 20.0f;
    bool direction = true;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (direction) this.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime); 
        else this.transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        if (this.transform.position.z > 512 || this.transform.position.z < 0) direction = !direction;

    }
}
