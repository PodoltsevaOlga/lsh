using UnityEngine;
using UnityEngine.UI;

using System.Collections;

using System.Collections.Generic;       //Allows us to use Lists. 

public class GameManager : MonoBehaviour
{

    //Static instance of GameManager which allows it to be accessed by any other script.
    public static GameManager instance = null;              
    private MapManager mapScript;
    private Text levelText;
    private GameObject levelImage;

    public int bombsInArsenal = 3;
    private int level = 1;
    private float levelStartDelay = 2f;


    //Awake is always called before any Start functions
    void Awake()
    {
        Debug.Log("AWAKE");
        //there can only ever be one instance of a GameManager
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        mapScript = GetComponent<MapManager>();
        mapScript.SetGameManager(this);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
        
        InitGame();
    }

    void OnLevelWasLoaded(int index)
    {
        level++;
        InitGame();
    }

    //Initializes the game for each level.
    private void InitGame()
    {
        
        Debug.Log("INIT GAME");
        levelImage = GameObject.Find("LevelImage");
        levelText = GameObject.Find("LevelText").GetComponent<Text>();
        levelText.text = "Level " + level;
        levelImage.SetActive(true);
        
        mapScript.MapSetup();
        Debug.Log("AFTER MAPSETUP");
        Invoke("HideLevelImage", levelStartDelay);
        GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().SetGameManager(this);
    }

    public void HideLevelImage()
    {
        levelImage.SetActive(false);
    }
    

    public MapManager GetMapScript()
    {
        return mapScript;
    }



    //Update is called every frame.
    void Update()
    {

    }
}