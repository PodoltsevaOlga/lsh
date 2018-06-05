using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    private GameManager gameManager;

    private float moveTime = 0.1f;
    private float restartLevelDelay = 2f;
    public LayerMask blockingLayer;
    public LayerMask environmentLayer;

    private BoxCollider2D boxCollider;      
    private Rigidbody2D rigidBody;
    private Animator animator;

    private Text bombText;
    private Text movementText;
    private Text actionText;
    
    private int movements = 0;
    private int bombs = 0;
    private bool hasTreasure = false;
    private bool isMoving = false;
    private bool isSmoothMoving = false;
    private bool isOnGround = true;
    private bool isInRiver = false;
    public float turnDelay = 0.05f;
    private GameObject currObjectOn = null;

    private KeyCode tunnelUseKey = KeyCode.Space;
    private KeyCode bombUseKey = KeyCode.Z;


    public void SetGameManager(GameManager _gameManager)
    {
        gameManager = _gameManager;
    }

    void Start () {
        boxCollider = GetComponent<BoxCollider2D>();
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        bombText = GameObject.Find("BombText").GetComponent<Text>();
        movementText = GameObject.Find("MovementText").GetComponent<Text>();
        actionText = GameObject.Find("ActionText").GetComponent<Text>();

        bombText.text = "Bombs " + bombs.ToString();
        movementText.text = "Movements: " + movements.ToString();
    }

    private void Update()
    {
        if (isMoving) return;

        //Tunnel using
        if (Input.GetKeyUp(tunnelUseKey) && currObjectOn != null && currObjectOn.tag == "Tunnel")
        {
            TunnelUse();
        }
        else 
        {
            int horizontal = 0;
            int vertical = 0;

            horizontal = (int)(Input.GetAxisRaw("Horizontal"));
            if (horizontal == 0)
                vertical = (int)(Input.GetAxisRaw("Vertical"));

            if (horizontal != 0 || vertical != 0)
            {
                if (Input.GetKey(bombUseKey)) 

                    BombUse(horizontal, vertical);
                else
                    AttemptMove(horizontal, vertical);
            }
        }        
    }

    //Co-routine for moving from one space to next
    private IEnumerator SmoothMovement(Vector3 end, bool wait)
    {
        isSmoothMoving = true;
        float remainingDistance = (transform.position - end).magnitude;
        
        if (wait)
        {
            yield return new WaitForSeconds(0.2f);
        }

        while (remainingDistance > float.Epsilon)
        {
            Vector3 newPosition = Vector3.MoveTowards(rigidBody.position, end, Time.deltaTime / moveTime);
            rigidBody.MovePosition(newPosition);
            remainingDistance = (transform.position - end).magnitude;
            yield return null;
        }
        
        isSmoothMoving = false;
        if (isOnGround)
            isMoving = false;
    }


    public void AttemptMove(int xDir, int yDir)
    {
        isMoving = true;
        RaycastHit2D hit;

        Vector2 start = transform.position;
        Vector2 end = start + new Vector2(xDir, yDir);

        //Disable the boxCollider so that linecast doesn't hit this object's own collider.
        boxCollider.enabled = false;
        hit = Physics2D.Linecast(start, end, blockingLayer);
        boxCollider.enabled = true;

        if (hit.transform != null && hit.transform.tag == "Wall") {
            WallHandle(hit.transform);
            
        }
        else if (hit.transform != null && hit.transform.tag == "Exit" && !hasTreasure)
        {
            CantExit();
        }
        else
        {
            boxCollider.enabled = false;
            if (currObjectOn != null)
                currObjectOn.GetComponent<Collider2D>().enabled = false;
            hit = Physics2D.Linecast(start, end, environmentLayer);
            if (currObjectOn != null)
                currObjectOn.GetComponent<Collider2D>().enabled = true;
            boxCollider.enabled = true;

            movements++;
            Debug.Log(movements.ToString());

            //show item. If Player find item, movement makes with small delay, so he can notice item
            if (hit.transform != null)
                hit.transform.gameObject.GetComponent<SpriteRenderer>().enabled = true;

            if (hit.transform != null && (hit.transform.tag == "River" || hit.transform.tag == "Tunnel"))
                isOnGround = false;
            else
                isOnGround = true;
            if (hit.transform == null || hit.transform.tag != "River")
                isInRiver = false;
            
            StartCoroutine(SmoothMovement(end, (hit.transform != null)));
            
            if (hit.transform != null && hit.transform.tag == "River")
                StartCoroutine(RiverHandle(hit.transform));
            else if (hit.transform != null && hit.transform.tag == "Tunnel")
                StartCoroutine(TunnelHandle(hit.transform));
            else
                currObjectOn = null;
            movementText.text = "Movements: " + movements.ToString();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    { 
        switch (other.tag)
        {
            case "Treasure":
                TreasureHandle(other);
                break;
            case "Arsenal":
                ArsenalHandle(other);
                break;
            case "Exit":
                StartCoroutine(ExitHandle(other));
                break;
        }
    }
    
    private void WallHandle(Transform otherTransform)
    {
        Debug.Log("I can't hit the wall");
        StartCoroutine(ShowActionText("I can't hit the wall"));
        isMoving = false;
    }

    private IEnumerator RiverHandle(Transform otherTransform)
    {
        GameObject nextRiverTile = otherTransform.gameObject.GetComponent<ConnectedListObject>().GetNext();
        yield return new WaitUntil(() => !isSmoothMoving);
        currObjectOn = otherTransform.gameObject;
        if (nextRiverTile != null && !isInRiver)
        {
            nextRiverTile.GetComponent<SpriteRenderer>().enabled = true;
            StartCoroutine(SmoothMovement(new Vector3(nextRiverTile.transform.position.x, nextRiverTile.transform.position.y, 0f), false));
            yield return new WaitUntil(() => !isSmoothMoving);
            currObjectOn = nextRiverTile;
        }
        isMoving = false;
        isInRiver = true;
    }

    private IEnumerator TunnelHandle(Transform otherTransform)
    {
        GameObject nextTunnel = otherTransform.gameObject.GetComponent<ConnectedListObject>().GetNext();
        yield return new WaitUntil(() => !isSmoothMoving);
        currObjectOn = otherTransform.gameObject;
        nextTunnel.GetComponent<SpriteRenderer>().enabled = true;
        rigidBody.MovePosition(new Vector3(nextTunnel.transform.position.x, nextTunnel.transform.position.y, 0f));
        currObjectOn = nextTunnel;
        isMoving = false;
    }

    private void TunnelUse()
    {
        isMoving = true;
        GameObject nextTunnel = currObjectOn.GetComponent<ConnectedListObject>().GetNext();
        nextTunnel.GetComponent<SpriteRenderer>().enabled = true;
        rigidBody.MovePosition(new Vector3(nextTunnel.transform.position.x, nextTunnel.transform.position.y, 0f));
        currObjectOn = nextTunnel;
        movements++;
        movementText.text = "Movements: " + movements.ToString();
        isMoving = false;
    }

    private void BombUse(int xDir, int yDir)
    {
        isMoving = true;
        if (bombs <= 0)
            StartCoroutine(ShowActionText("You have no bombs"));
        else
        {
            RaycastHit2D hit;

            Vector2 start = transform.position;
            Vector2 end = start + new Vector2(xDir, yDir);
            
            boxCollider.enabled = false;
            hit = Physics2D.Linecast(start, end, blockingLayer);
            boxCollider.enabled = true;

            if (hit.transform != null && hit.transform.tag == "Wall")
            {
                bombs--;
                bombText.text = "Bombs: " + bombs.ToString();
                //bomb use animation
                
                gameManager.GetMapScript().BreakWall(hit.transform, xDir, yDir);
                movements++;
                movementText.text = "Movements: " + movements.ToString();

            }
            else
            {
                Debug.Log("there is no wall in this direction");
                //StartCoroutine(ShowActionText("There is no wall in this direction"));
            }
        }
        isMoving = false;
    }
    private void TreasureHandle(Collider2D otherCollider)
    {
        otherCollider.gameObject.SetActive(false);
        hasTreasure = true;
        StartCoroutine(ShowActionText("You found the treasure!"));
    }

    private void ArsenalHandle(Collider2D otherCollider)
    {
        otherCollider.gameObject.SetActive(false);
        bombs = GameManager.instance.bombsInArsenal;
        bombText.text = "Bombs: " + bombs.ToString();
        StartCoroutine(ShowActionText("You found " + bombs.ToString() + " bombs"));
    }
    
    private void CantExit()
    {
        StartCoroutine(ShowActionText("You can't exit without treasure"));
        isMoving = false;
    }

    private IEnumerator ExitHandle(Collider2D transform)
    {
        yield return new WaitUntil(() => !isSmoothMoving);
        isMoving = true;
        StartCoroutine(ShowActionText("You win!"));
        Invoke("NextLevel", restartLevelDelay);
    }

    private void NextLevel()
    {
        SceneManager.LoadScene("Level");
    }

    private IEnumerator ShowActionText(string message)
    {
        actionText.text = message;
        yield return new WaitForSeconds(1f);
        actionText.text = "";
    }
}
