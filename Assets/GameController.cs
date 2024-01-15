using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    Vector2 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            transform.position = startPos;
        }if(collision.CompareTag("Respawn")){
            Die();
        }        
    }

    void Die(){
        Respawn();
    }

    void Respawn(){
        transform.position = startPos;
    }
}
