using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapManager : MonoBehaviour {

    private GameManager gameManager;

    public int width;
    public int height;
    public int exitCount;

    private enum Tile { Blank = 0, Wall = 1, Water = 2, Tunnel = 3, Arsenal = 4, Treasure = 5 };
    
    private int riverLength;
    public int tunnelCount;
    public int arsenalCount;
    public int treasureCount;

    private Transform mapHolder = null;

    public GameObject floorTile;
    public GameObject wall;
    public GameObject wallCorner;
    public GameObject brokenWall;
    public GameObject exit;
    public GameObject riverTile;
    public GameObject tunnelTile;
    public GameObject arsenal;
    public GameObject treasure;
    public GameObject player;

    private Dictionary<Tile, GameObject> items;

    private Tile[,] maze;

    private const int MINMAZESIZE = 4;
    private const int MINEXITCOUNT = 1;
    private const int MINARSENALCOUNT = 1;
    private const int MINTUNNELCOUNT = 2;
    private const int MINTREASURECOUNT = 1;
    private int minRiverLength;
    private int maxRiverLength;

    public void SetGameManager(GameManager _gameManager)
    {
        gameManager = _gameManager;
    }

    private void dictItems()
    {
        items = new Dictionary<Tile, GameObject>();
        items.Add(Tile.Water, riverTile);
        items.Add(Tile.Tunnel, tunnelTile);
        items.Add(Tile.Arsenal, arsenal);
        items.Add(Tile.Treasure, treasure);
    }

    public void MapSetup()
    {
        Debug.Log("MAP SETUP");
        DestroyPreviousMap();
        mapHolder = new GameObject("map").transform;
        dictItems();
        width = System.Math.Max(width, MINMAZESIZE);
        height = System.Math.Max(height, MINMAZESIZE);
        exitCount = System.Math.Max(exitCount, MINEXITCOUNT);
        minRiverLength = (int)(System.Math.Max(width, height) * 0.8);
        maxRiverLength = (int)(System.Math.Max(width, height) * 1.3);

        instantiateFloor();

        EllerAlgorithm();
        generateExits();
        instantiateWalls();
        instantiateExit();

        generateRiver();
        instantiateList(riverSource, riverTile);
        
        arsenalCount = System.Math.Max(arsenalCount, MINARSENALCOUNT);
        treasureCount = System.Math.Max(treasureCount, MINTREASURECOUNT);
        tunnelCount = System.Math.Max(MINTUNNELCOUNT, System.Math.Min(width * height - riverLength - arsenalCount - treasureCount, tunnelCount));
 
        //now we need to remember all blank tiles for easier item generation
        List<Vector2Int> blankTiles = getBlankTiles();
        generateTunnels(blankTiles);
        instantiateList(firstTunnel, tunnelTile);


        int seed = System.DateTime.Now.ToString().GetHashCode();
        System.Random rand = new System.Random(seed);
        instantiateRandomItem(blankTiles, arsenalCount, arsenal, Tile.Arsenal, rand);
        instantiateRandomItem(blankTiles, treasureCount, treasure, Tile.Treasure, rand);
        instantiateRandomItem(blankTiles, 1, player, Tile.Treasure, rand);

        //for (int i = 0; i < maze.GetLength(0); i++)
        //{
          //  string s = "";
            //for (int j = 0; j < maze.GetLength(1); j++)
            //{
              //  s = s + " " + ((int)maze[i, j]).ToString();
           // }
            //Debug.Log(s);
        //}
    }


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void EllerAlgorithm()
    {
        //initialization
        int seed = System.DateTime.Now.ToString().GetHashCode();
        System.Random rand = new System.Random(seed);

        maze = new Tile[height * 2 + 1, width * 2 + 1];
        for (int i = 0; i < maze.GetLength(0); i++)
        {
            for (int j = 0; j < maze.GetLength(1); j++)
            {
                maze[i, j] = Tile.Wall;
            }
        }

        const int ELLERWALL = -1;
        const int UNDETERMINEDCELL = 0;

        int[] prevRow = new int[width * 2 + 1];
        int[] currentRow = new int[width * 2 + 1];
        for (int i = 0; i < currentRow.Length; i++)
        {
            currentRow[i] = -1;
        }


        //first row initialization. each cell is individual unique set
        int setCount = width;
        for (int i = 0; i < prevRow.Length; i++)
        {
            if (i % 2 == 0)
                prevRow[i] = ELLERWALL;
            else
                prevRow[i] = i / 2 + 1;
            currentRow[i] = UNDETERMINEDCELL;

        }

        //except last row 
        for (int row = 0; row < height - 1; row++)
        {
            if (row > 0)
            {
                for (int j = 0; j < prevRow.Length; j++)
                {
                    prevRow[j] = currentRow[j];
                    currentRow[j] = UNDETERMINEDCELL;
                }
            }

            currentRow[0] = ELLERWALL;
            currentRow[currentRow.Length - 1] = ELLERWALL;

            //randomly join adjacent cells
            //they should be in different sets and have a wall between
            //merge this sets 
            //DSU can be used for more effective merge but it will affect only on large mazes, so... not today
            //look only on possible wall locations
            for (int j = 2; j < prevRow.Length - 2; j += 2)
            {
                if (prevRow[j] == ELLERWALL
                        && prevRow[j - 1] != prevRow[j + 1]
                        && rand.NextDouble() > 0.7)
                {
                    int firstSet = System.Math.Min(prevRow[j - 1], prevRow[j + 1]);
                    int secondSet = System.Math.Max(prevRow[j - 1], prevRow[j + 1]);

                    prevRow[j] = firstSet;
                    for (int k = 0; k < prevRow.Length; k++)
                    {
                        if (prevRow[k] == secondSet)
                            prevRow[k] = firstSet;
                    }
                }
            }

            //randomly create vertical connection downward to the next row
            //each set must have at least one vertical connection
            int setBegin = 1;
            int setEnd = 1;
            while (setEnd != prevRow.Length - 2)
            {
                int j = setBegin;
                while (j < prevRow.Length - 2 && prevRow[j] == prevRow[j + 2])
                {
                    j += 2;
                }
                setEnd = j;

                bool isConnected = false;
                //don't like infinite loops
                const int LOOPLIMIT = 10;
                int loopCount = 0;
                while (loopCount < LOOPLIMIT && !isConnected)
                {
                    for (int k = setBegin; k <= setEnd; k += 2)
                    {
                        if (rand.NextDouble() > 0.5)
                        {
                            currentRow[k] = prevRow[k];
                            isConnected = true;
                        }
                    }
                    loopCount++;
                }
                //if LOOPLIMIT isn't enough, make one random connection
                if (!isConnected)
                {

                    int connection = (setEnd != setBegin) ? setBegin + (rand.Next((setEnd - setBegin) / 2 + 1) - 1) * 2 : 0;
                    currentRow[connection] = prevRow[connection];
                }
                setBegin = setEnd + 2;

            }

            //each remaining cell of next row is individual set
            for (int j = 1; j < currentRow.Length - 1; j++)
            {
                if (j % 2 == 1 && currentRow[j] == UNDETERMINEDCELL)
                {
                    setCount++;
                    currentRow[j] = setCount;
                }
                else if (j % 2 == 0 && currentRow[j - 1] != currentRow[j + 1])
                    currentRow[j] = ELLERWALL;
                else if (j % 2 == 0 && currentRow[j - 1] == currentRow[j + 1])
                    currentRow[j] = currentRow[j - 1];
            }

            //save row to the maze
            for (int j = 0; j < prevRow.Length; ++j)
            {
                if (prevRow[j] != ELLERWALL)
                {
                    maze[2 * row + 1, j] = Tile.Blank;
                    if (prevRow[j] == currentRow[j])
                        maze[2 * row + 2, j] = Tile.Blank;
                }
            }
        }

        //last row
        //join adjacent cells that don't share a set
        for (int i = 0; i < prevRow.Length; i++)
        {
            prevRow[i] = currentRow[i];
        }

        for (int i = 2; i < prevRow.Length - 1; i += 2)
        {
            if (prevRow[i] == ELLERWALL && prevRow[i - 1] != prevRow[i + 1])
            {
                int firstSet = System.Math.Min(prevRow[i - 1], prevRow[i + 1]);
                int secondSet = System.Math.Max(prevRow[i - 1], prevRow[i + 1]);
                prevRow[i] = UNDETERMINEDCELL;
                for (int k = 0; k < prevRow.Length; k++)
                {
                    if (prevRow[k] == secondSet)
                        prevRow[k] = firstSet;
                }
            }
        }
        for (int i = 0; i < prevRow.Length; i++)
        {
            if (prevRow[i] == UNDETERMINEDCELL)
                maze[maze.GetLength(0) - 2, i] = maze[maze.GetLength(0) - 2, i - 1];
            else if (prevRow[i] != ELLERWALL)
                maze[maze.GetLength(0) - 2, i] = Tile.Blank;
        }
    }

    private void generateExits()
    {
        int seed = System.DateTime.Now.ToString().GetHashCode();
        System.Random rand = new System.Random(seed);
        for (int i = 0; i < exitCount; i++)
        {
            int wall = rand.Next(3);
            switch (wall)
            {
                case 0:
                    maze[0, rand.Next(width) * 2 + 1] = Tile.Blank;
                    break;
                case 1:
                    maze[rand.Next(height) * 2 + 1, width * 2] = Tile.Blank;
                    break;
                case 2:
                    maze[height * 2, rand.Next(width) * 2 + 1] = Tile.Blank;
                    break;
                case 3:
                    maze[rand.Next(height) * 2 + 1, 0] = Tile.Blank;
                    break;
            }
        }
    }

    private class ListObject
    {
        private int x;
        private int y;
        private ListObject next;

        public ListObject(int _x, int _y)
        {
            this.x = _x;
            this.y = _y;
            this.next = null;
        }

        public void setNext(ListObject _next)
        {
            this.next = _next;
        }

        public int getX()
        {
            return this.x;
        }

        public int getY()
        {
            return this.y;
        }

        public ListObject getNext()
        {
            return this.next;
        }
    }

    private ListObject riverSource;

    private void generateRiver()
    {
        int seed = System.DateTime.Now.ToString().GetHashCode();
        System.Random rand = new System.Random(seed);

        const int LIMITLOOP = 5;
        riverLength = 0;
        int loopCount = 0;

        //try to generate river that will not be too short
        while (riverLength < minRiverLength && loopCount < LIMITLOOP)
        {
            loopCount++;
            riverLength = 0;
            int sourceX = rand.Next(width) * 2 + 1;
            int sourceY = rand.Next(height) * 2 + 1;
            riverSource = new ListObject(sourceX, sourceY);
            ListObject currTile = riverSource;
            ListObject nextTile = null;
            riverLength++;
            maze[sourceX, sourceY] = Tile.Water;
            bool isDeadEnd = false;
            Vector2Int[] directions = new Vector2Int[4] { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            do
            {
                bool isNextChosen = false;
                //random permutation of directions
                directions = directions.OrderBy(dir => rand.Next()).ToArray();
                for (int i = 0; i < directions.Length; ++i)
                {
                    int nextX = currTile.getX() + directions[i].x * 2;
                    int nextY = currTile.getY() + directions[i].y * 2;
                    if (nextX > 0 && nextX < maze.GetLength(0)
                        && nextY > 0 && nextY * 2 < maze.GetLength(1)
                        && maze[nextX, nextY] != Tile.Water
                        //avoid self-intersection
                        && maze[nextX - directions[i].x, nextY - directions[i].y] == Tile.Blank)
                    {
                        nextTile = new ListObject(nextX, nextY);
                        currTile.setNext(nextTile);
                        maze[nextX, nextY] = Tile.Water;
                        maze[nextX - directions[i].x, nextY - directions[i].y] = Tile.Water;
                        riverLength++;
                        currTile = nextTile;
                        isNextChosen = true;
                        break;
                    }
                }
                if (!isNextChosen)
                    isDeadEnd = true;
            } while (!isDeadEnd && riverLength < maxRiverLength);
            //if river is too short, delete it and try to generate again
            if (riverLength < minRiverLength && loopCount != LIMITLOOP - 1)
            {
                currTile = riverSource;
                nextTile = null;
                while (currTile.getNext() != null)
                {
                    nextTile = currTile.getNext();
                    maze[currTile.getX(), currTile.getY()] = Tile.Blank;
                    maze[currTile.getX() + (nextTile.getX() - currTile.getX()) / 2, currTile.getY() + (nextTile.getY() - currTile.getY()) / 2] = Tile.Blank;
                    currTile = nextTile;
                }
                maze[currTile.getX(), currTile.getY()] = Tile.Blank;
            }

        }
    }
    
    private List<Vector2Int> getBlankTiles()
    {
        List<Vector2Int> blankTiles = new List<Vector2Int>();
        for (int i = 1; i <= width; i++)
        {
            for (int j = 1; j <= height; j++)
            {
                if (maze[i * 2 - 1, j * 2 - 1] == Tile.Blank)
                {
                    blankTiles.Add(new Vector2Int(i * 2 - 1, j * 2 - 1));
                }
            }
        }
        return blankTiles;
    }

    private ListObject firstTunnel = null;

    private void generateTunnels(List<Vector2Int> blankTiles)
    {
        int seed = System.DateTime.Now.ToString().GetHashCode();
        System.Random rand = new System.Random(seed);

        int index = rand.Next(blankTiles.Count);
        Vector2Int position = blankTiles[index];
        firstTunnel = new ListObject(position.x, position.y);
        maze[position.x, position.y] = Tile.Tunnel;
        ListObject currTunnel = firstTunnel;
        ListObject nextTunnel = null;
        blankTiles.RemoveAt(index);
        int i = 0;
        while (i < tunnelCount)
        {
            i++;
            index = rand.Next(blankTiles.Count);
            position = blankTiles[index];
            nextTunnel = new ListObject(position.x, position.y);
            blankTiles.RemoveAt(index);
            currTunnel.setNext(nextTunnel);
            maze[position.x, position.y] = Tile.Tunnel;
            currTunnel = nextTunnel;  
        }
        currTunnel.setNext(firstTunnel);   
    }

    private void instantiateRandomItem(List<Vector2Int> blankTiles, int count, GameObject gameObject, Tile tile, System.Random rand)
    {
        for (int i = 0; i < count; i++)
        {
            int index = rand.Next(blankTiles.Count);
            maze[blankTiles[index].x, blankTiles[index].y] = tile;
            Vector3 position = new Vector3((blankTiles[index].y + 1) / 2, (blankTiles[index].x + 1) / 2, 0f);
            GameObject instance = Instantiate(gameObject, position, Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
            blankTiles.RemoveAt(index);
        }
    }

    private void instantiateFloor()
    {
        for (int i = 1; i <= width; i++)
        {
            for (int j = 1; j <= height; ++j)
            {
                GameObject instance = Instantiate(floorTile, new Vector3(j, i, 0f), Quaternion.identity) as GameObject;
                instance.transform.SetParent(mapHolder);
            }
        }
    }

    private void instantiateWalls()
    {
       
        for (int i = 0; i < maze.GetLength(0); i += 2)
        {
            for (int j = 1; j < maze.GetLength(1); j += 2)
            {
                if (maze[i, j] == Tile.Wall)
                {
                    GameObject toinstantiate =  wall;
                    Vector3 position = new Vector3( j / 2 + 1, i / 2  + 0.5f, 0f);
                    GameObject instance = Instantiate(toinstantiate, position, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapHolder);
                    //horizontal wall
                    instance.transform.rotation *= Quaternion.Euler(0, 0, 90);
                }
            }
        }
        for (int i = 1; i < maze.GetLength(0); i += 2)
        {
            for (int j = 0; j < maze.GetLength(1); j += 2)
            {
                if (maze[i, j] == Tile.Wall)
                {
                    GameObject toinstantiate = wall;
                    Vector3 position = new Vector3(j / 2 + 0.5f, i / 2 + 1, 0f);
                    GameObject instance = Instantiate(toinstantiate, position, Quaternion.identity) as GameObject;
                    instance.transform.SetParent(mapHolder);
                }
            }
        }


        //corners 
    }

    private void instantiateExit()
    {
        for (int i = 0; i <= width + 1; i++)
        {
            GameObject instance = Instantiate(exit, new Vector3(0, i, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
            instance = Instantiate(exit, new Vector3(height + 1, i, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
        }
        for (int j = 0; j <= height + 1; j++)
        {
            GameObject instance = Instantiate(exit, new Vector3(j, 0, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
            instance = Instantiate(exit, new Vector3(j, width + 1, 0f), Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
        }
    }

    private void instantiateList(ListObject firstElement, GameObject gameObject)
    {
        ListObject currElement = firstElement;
        ListObject nextElement = null;
        Vector3 position = new Vector3((currElement.getY() + 1) / 2, (currElement.getX() + 1) / 2, 0f);
        GameObject instance = Instantiate(gameObject, position, Quaternion.identity) as GameObject;
        instance.transform.SetParent(mapHolder);
        ConnectedListObject currObject = instance.GetComponent<ConnectedListObject>();
        GameObject firstInstance = instance;
        while (currElement.getNext() != null && currElement.getNext() != firstElement)
        {
            nextElement = currElement.getNext();
            position = new Vector3((nextElement.getY() + 1) / 2, (nextElement.getX() + 1) / 2, 0f);
            instance = Instantiate(gameObject, position, Quaternion.identity) as GameObject;
            instance.transform.SetParent(mapHolder);
            currObject.SetNext(instance);

            currElement = currElement.getNext();
            currObject = instance.GetComponent<ConnectedListObject>();
        }

        if (currElement.getNext() == firstElement)
        {
            currObject.SetNext(firstInstance);
        }
    }

    public void BreakWall(Transform wallTransform, int xDir, int yDir) 
    {
        wallTransform.gameObject.SetActive(false);
        Vector3 position = new Vector3(wallTransform.position.x, wallTransform.position.y, 0f);
        GameObject instance = Instantiate(brokenWall, position, Quaternion.identity) as GameObject;
        instance.transform.SetParent(mapHolder);
        if (yDir != 0)
            instance.transform.rotation *= Quaternion.Euler(0, 0, 90);
    }

    private void DestroyPreviousMap()
    {
        if (mapHolder != null)
        {
            mapHolder.gameObject.SetActive(false);
            var children = new List<GameObject>();
            foreach (Transform child in mapHolder) children.Add(child.gameObject);
            children.ForEach(child => Destroy(child));

            Debug.Log("DESTROY");
            Destroy(mapHolder.gameObject);
            mapHolder = null;
        }
    }
}

