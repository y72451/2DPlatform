using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour
{
    public bool isOnAir = true;
    public int dropSpeed = -10;
    public int dropAcceler = -2;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isOnAir)
        {
            //this.gameObject.transform.Translate(new Vector2(0, -1) * Time.deltaTime * dropSpeed);
        }
    }
}
