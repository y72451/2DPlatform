using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Start is called before the first frame update
    bool isFaceRight;
    public int bulletSpeed = 30;
    float liveTime = 0f;
    void Start()
    {
        if (this.transform.rotation.y == 180)
        {
            isFaceRight = true;
        }
        else if (this.transform.rotation.y == 0)
        {
            isFaceRight = false;
        }
        liveTime = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        this.gameObject.transform.Translate(new Vector2(isFaceRight ? 1 : -1, 0) * Time.deltaTime * bulletSpeed);
        liveTime -= Time.deltaTime;
        if (liveTime <= 0f)
        {            
            Destroy(this.gameObject);
            Debug.Log("Destory by time");
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log("OnTriggerEnter2D Bullet");
        if (col.gameObject.tag != "Player")
        {
            Debug.Log("Destory by hit object: "+col.gameObject.name);
            Destroy(this.gameObject);
        }
    }
}
