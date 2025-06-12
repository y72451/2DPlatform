using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    public GameObject Dust;
    public GameObject Bullet;
    // Start is called before the first frame update
    void Start()
    {

    }

    public void setEff(int objIndex, Transform pos,bool faceRight = false)
    {
        GameObject obj = null;
        if (objIndex == 1)
        {
            obj = Dust;
        }
        else if (objIndex == 2)
        {
            obj = Bullet;
        }
        if (faceRight)
        {
            Object.Instantiate(obj, pos.position, Quaternion.Euler(0, 180, 0));
        }
        else
        {
            Object.Instantiate(obj, pos.position, Quaternion.identity);
        }
        
    }
}
