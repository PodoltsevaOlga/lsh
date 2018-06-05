using UnityEngine;
using System.Collections;

public class Wall : MonoBehaviour
{
    //Alternate sprite to display after Wall has been destroyed
    public Sprite dmgSprite;                    

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D collider;


    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collider = GetComponent<BoxCollider2D>();
    }

    public void DestroyWall()
    {
        spriteRenderer.sprite = dmgSprite;
        collider.enabled = false;
    }
}
