using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paralaxController : MonoBehaviour
{

    Transform cam;
    Vector3 camStartPos;
    float distance;

    GameObject[] backgrounds;
    Material[] mat;
    float[] backSpeed;

    float farthestBack;

    [Range(0.01f,0.05f)]
    public float paralaxSpeed;

    // Start is called before the first frame update
    void Start()
    {
        cam =  Camera.main.transform;
        camStartPos = cam.position;

        int backcount = transform.childCount;
        mat= new Material[backcount];
        backSpeed = new float[backcount];
        backgrounds = new GameObject[backcount];

        for (int i = 0; i < backcount; i++)
        {
            backgrounds[i] = transform.GetChild(i).gameObject;
            mat[i] = backgrounds[i].GetComponent<Renderer>().material;
        }
        BackSpeedCalculate(backcount);
    }

    // Update is called once per frame
    void BackSpeedCalculate(int backCount)
    {
        for (int i = 0; i < backCount; i++)
        {
            if((backgrounds[i].transform.position.z - cam.position.z) > farthestBack)
            {
                farthestBack = backgrounds[i].transform.position.z - cam.position.z;
            }
        }

        for (int i = 0; i < backCount; i++)
        {
            backSpeed[i] = (backgrounds[i].transform.position.z - cam.position.z) / farthestBack;
        }
        

    }

    private void LateUpdate()
    {
        distance = cam.position.x - camStartPos.x;
        transform.position= new Vector3(cam.position.x, transform.position.y, 0);

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * paralaxSpeed;
            mat[i].SetTextureOffset("_MainTex", new Vector2(distance, 0) * speed);
        }
    }
}
