using UnityEngine;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour
{
    private bool gridInitialized = false;  // Track if the grid is fully created
    private int width = 48;  // Number of hex tiles in the grid's width
    private int height = 36; // Number of hex tiles in the grid's heightb
    public Vector3 gridCenter = new Vector3(0, 0, 0);  // Center of your grid
    float hexRadius = 0.5f;
    [SerializeField] private HexCell hexCellPrefab; // Reference to the hex cell prefab
    // [SerializeField] public HexCell hexCellPrefab;  // Reference to hex cell prefab
    private HexCell[,] cells;  // 2D array to hold the cells
    private Color lightColor = new Color(0.2f, 0.8f, 0.2f); 
    private Color darkColor = new Color(0 / 255f, 129 / 255f, 56 / 255f, 255f / 255f);
    private HexCell lastHoveredHex = null;  // Store the last hovered hex
    public Ball ball;


    private void Start()
    {
        if (cells == null)
        {
            // Initialize the cells array with the correct dimensions
            cells = new HexCell[width, height];
            Debug.Log("HexGrid initialized. Creating grid...");
            CreateGrid();  // Generate the grid
            gridInitialized = true;  // Mark the grid as initialized
        }
        // Create out-of-bounds planes around the grid
        CreateOutOfBoundsPlanes(this);
        InitializeDefenseObstacles(20);
    }

    public void InitializeDefenseObstacles(int obstacleCount)
    {
        int totalHexes = cells.Length;
        List<HexCell> potentialObstacles = new List<HexCell>();

        foreach (HexCell hex in cells)
        {
            if (hex == null)
            {
                Debug.LogError("HexCell is null in the grid. Ensure grid initialization is correct.");
                continue;
            }
            
            if (!hex.isOutOfBounds)
            {
                potentialObstacles.Add(hex);
            }
        }

        // Shuffle and pick random hexes for obstacles
        for (int i = 0; i < obstacleCount && potentialObstacles.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, potentialObstacles.Count);
            HexCell defenseHex = potentialObstacles[randomIndex];
            defenseHex.isDefenseOccupied = true;  // Mark this hex as an obstacle
            defenseHex.HighlightHex("isDefenseOccupied");
            potentialObstacles.RemoveAt(randomIndex);  // Remove it from the list
        }

        Debug.Log($"{obstacleCount} defense-occupied hexes initialized.");
    }

    void Update()
    {
        DetectHexUnderMouse();
    }

    void CreateGrid()
    {
        for (int z = -height / 2; z < height / 2; z++)
        {
            for (int x = -width / 2; x < width / 2; x++)
            {
                CreateCell(x, z);
            }
        }
        Debug.Log("HexGrid created with dimensions: " + width + "x" + height);
    }

    public bool IsGridInitialized()
    {
        return gridInitialized;
    }

    void CreateCell(int x, int z)
    {
        // Adjust for proper horizontal and vertical spacing based on the hexagon's size
        // Assuming the hexagon prefab has a width of 1 unit
        float hexWidth = 1f;  // Width of the hexagon tile
        float hexHeight = 0.866f * hexWidth;  // Vertical height, typically sqrt(3)/2 for a regular hexagon
        // Adjust horizontal and vertical offsets for a flat-topped layout
        float zOffset = (x % 2 == 0) ? 0f : hexHeight / 2f;  // Stagger every other column
        Vector3 position = new Vector3(x * 0.75f * hexWidth, 0, z * hexHeight + zOffset);

        // Instantiate the hex cell at the calculated position
        HexCell cell = Instantiate(hexCellPrefab, position, Quaternion.identity, transform);
        cell.coordinates = new Vector3Int(x, 0, z);
        cell.name = $"HexCell [{x}, {z}]";  // Assign the name to the GameObject
        // Assign dark hex status here
        if (ShouldBeDarkHex(x, z))
        {
            cell.isDark = true;
        }
        // Apply either lightColor or darkColor when initializing the hex
        Color hexColor = cell.isDark ? darkColor : lightColor;
        cell.InitializeHex(hexColor);
        // Check array bounds and log the creation of each cell
        int arrayX = x + width / 2;
        int arrayZ = z + height / 2;
        if (arrayX >= 0 && arrayX < width && arrayZ >= 0 && arrayZ < height)
        {
            cells[arrayX, arrayZ] = cell;
            // Debug.Log($"HexCell created at array position [{arrayX}, {arrayZ}] with world position {position}");
        }
        else
        {
            Debug.LogError($"Invalid position for HexCell [{x}, {z}] with array index [{arrayX}, {arrayZ}]");
        }

        // Store the cell in the array (with proper offset to account for negative coordinates)
        // cells[x + width / 2, z + height / 2] = cell; 

        // Rotate the hex tile by 90 degrees to make it flat-topped
        cell.transform.rotation = Quaternion.Euler(0, 90, 0);  // Rotate 90 degrees to make it flat-topped

        AssignHexFeatures(cell);
    }
    
    void AssignHexFeatures(HexCell cell)
    {
        int x = cell.coordinates.x;
        int z = cell.coordinates.z;

        // Example: Kick off Hex in the middle
        if (x == 0 && z == 0 )
        {
            cell.isKickOff = true;
        }

        // Example: Set out of bounds on the edges
        if ( x > 18 || x < -18 || ((x % 2 == 0) ? z >= 13 || z <= -13 : z >= 13 || z < -13 ))
        {
            cell.isOutOfBounds = true;
        }

        // Example: Set penalty boxes (hard-coded positions for now)
        if ((x >= -18 && x <= -12 && ((x % 2 == 0) ? z > -8 && z < 8 : z >= -8 && z < 7 )) ||  // Left penalty box
            (x >= 12 && x <= 18 && ((x % 2 == 0) ? z > -8 && z < 8 : z >= -8 && z <= 7 )))  // Right penalty box
        {
            cell.isInPenaltyBox = true;
        }

        // Example: Set final thirds
        if ( x <= 18 && x >= 8 && ((x % 2 == 0) ? (z < 13 && z > -13) : (z < 13 && z > -13)) || x >= -18 && x <= -8 && ((x % 2 == 0) ? (z < 13 && z > -13) : (z < 13 && z > -13)) )
        {
            cell.isInFinalThird = true;
        }

        // Example: Set difficult shot positions (arbitrary, near the corners)
        if ((x == -18 || x == 18) && (z >= 6 || z <= -6) ||
            (x == -17 || x == 17) && (z >= 8 || z <= -9) ||
            (x == -16 || x == 16) && (z >= 10 || z <= -10) ||
            (x == -15 || x == 15) && (z >= 11 || z <= -11) ||
            (x == -14 || x == 14) && (z >= 12 || z <= -12))
        {
            cell.isDifficultShotPosition = true;
        }

        // // Dark Green Hexes
        // if ((x % 2 == 0) ? (z % 3 == 0) : ((z+2) % 3 == 0))
        // {
        //     cell.isDark = true;
        // }

        // Debug to visually differentiate hexes in Unity (optional)
        // else if (cell.isKickOff) cell.GetComponent<Renderer>().material.color = Color.red;
        // else if (cell.isOutOfBounds) cell.GetComponent<Renderer>().material.color = Color.blue;
        // else if (cell.isDifficultShotPosition) cell.GetComponent<Renderer>().material.color = Color.magenta;
        // else if (cell.isInPenaltyBox) cell.GetComponent<Renderer>().material.color = Color.yellow;
        // else if (cell.isInFinalThird) cell.GetComponent<Renderer>().material.color = Color.green;
    }
    private bool ShouldBeDarkHex(int x, int z)
    {
        // Define your logic to determine which hexes should be dark
        return (x % 2 == 0) ? (z % 3 == 0) : ((z+2) % 3 == 0);  // Example: every other hex is dark
    }
    // Method to get the HexCell based on its coordinates
    public HexCell GetHexCellAt(Vector3Int coords)
    {
        int x = coords.x + width / 2;  // Offset to positive index
        int z = coords.z + height / 2; // Offset to positive index
        // Ensure the coordinates are within bounds of the grid
        if (x >= 0 && x < width && z >= 0 && z < height)
        {
            if (cells[x, z] == null)
            {
                Debug.LogError($"HexCell is null at [{x}, {z}]");
            }
            return cells[x, z];
        }
        Debug.LogError($"Requested HexCell is out of bounds at [{coords.x}, {coords.z}]");
        return null;  // Return null if out of bounds
    }

    public static List<HexCell> GetHexesInRange(HexGrid hexGrid, HexCell startHex, int range)
    {
        List<HexCell> hexesInRange = new List<HexCell>();
        Vector3Int startCubeCoords = HexGridUtils.OffsetToCube(startHex.coordinates.x, startHex.coordinates.z);  // Convert offset to cube

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(range, -dx + range); dy++)
            {
                int dz = -dx - dy;
                Vector3Int currentCubeCoords = new Vector3Int(startCubeCoords.x + dx, startCubeCoords.y + dy, startCubeCoords.z + dz);
                // Convert cube coords back to offset (to pass in as input)
                Vector2Int offsetCoords = HexGridUtils.CubeToOffset(currentCubeCoords);
                // Create a Vector3Int from offset coordinates
                Vector3Int offsetCoordsVec3 = new Vector3Int(offsetCoords.x, 0, offsetCoords.y);
                // Get the hex at the current coordinates using GetHexCellAt(Vector3Int)
                HexCell hex = hexGrid.GetHexCellAt(offsetCoordsVec3);
                if (hex != null)
                {
                    hexesInRange.Add(hex);
                }
            }
        }
        return hexesInRange;
    }

    public List<HexCell> GetDefenderHexes()
    {
        List<HexCell> defenderHexes = new List<HexCell>();

        // Loop through all hex cells in the grid
        foreach (HexCell cell in cells)
        {
            if (cell != null && cell.isDefenseOccupied)
            {
                defenderHexes.Add(cell);
            }
        }

        return defenderHexes;
    }

    public List<HexCell> GetDefenderNeighbors(List<HexCell> defenderHexes)
    {
        List<HexCell> defenderNeighbors = new List<HexCell>();

        foreach (HexCell defenderHex in defenderHexes)
        {
            HexCell[] neighbors = defenderHex.GetNeighbors(this);  // Assuming GetNeighbors already works
            foreach (HexCell neighbor in neighbors)
            {
                if (neighbor != null && !defenderNeighbors.Contains(neighbor))
                {
                    defenderNeighbors.Add(neighbor);
                }
            }
        }

        return defenderNeighbors;
    }

    public bool IsPassDangerous(List<HexCell> pathHexes, List<HexCell> defenderNeighbors)
    {
        // Check if any of the path hexes are in the defender's ZOI (neighbors)
        foreach (HexCell pathHex in pathHexes)
        {
            if (defenderNeighbors.Contains(pathHex))
            {
                Debug.Log($"Hex {pathHex.coordinates} is within a defender's ZOI, making the pass dangerous.");
                return true;
            }
        }
        return false;
    }
    
    public void CreateOutOfBoundsPlanes(HexGrid grid, float planeHeight = 0.05f)
    {
        // Get the outermost hex cells from the grid
        HexCell topLeftHex = grid.GetHexCellAt(new Vector3Int(-18, 0, 12));
        HexCell bottomRightHex = grid.GetHexCellAt(new Vector3Int(18, 0, -12));

        // Create a material for the out-of-bounds areas
        Material blueMaterial = new Material(Shader.Find("Unlit/Color"));
        blueMaterial.color = Color.blue;
        float horizontalOffset = width * hexRadius;
        float verticalOffset = height * hexRadius;
        // Layer index for the "IgnoreRaycast" layer
        int ignoreRaycastLayer = LayerMask.NameToLayer("IgnoreRaycast");

        // Left Plane (to the left of the grid)
        GameObject leftPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftPlane.name = "Left Out-of-Bounds";
        leftPlane.layer = ignoreRaycastLayer;
        leftPlane.transform.localScale = new Vector3(horizontalOffset * 2 , horizontalOffset * 2, 1);  // Scale based on grid height
        leftPlane.transform.position = new Vector3(topLeftHex.GetHexCorners()[5].x - horizontalOffset , planeHeight, 0);  // Place at the left edge
        leftPlane.transform.rotation = Quaternion.Euler(90, 0, 0);  // Align with the XZ plane
        leftPlane.GetComponent<Renderer>().material = blueMaterial;

        // Right Plane (to the left of the grid)
        GameObject rightPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightPlane.name = "Right Out-of-Bounds";
        rightPlane.layer = ignoreRaycastLayer;
        rightPlane.transform.localScale = new Vector3(horizontalOffset * 2 , horizontalOffset * 2, 1);  // Scale based on grid height
        rightPlane.transform.position = new Vector3(bottomRightHex.GetHexCorners()[2].x + horizontalOffset , planeHeight, 0);  // Place at the left edge
        rightPlane.transform.rotation = Quaternion.Euler(90, 0, 0);  // Align with the XZ plane
        rightPlane.GetComponent<Renderer>().material = blueMaterial;

        // Top Plane (above the grid)
        GameObject topPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        topPlane.name = "Top Out-of-Bounds";
        topPlane.layer = ignoreRaycastLayer;
        topPlane.transform.localScale = new Vector3(verticalOffset * 2 , verticalOffset * 2, 1);  // Scale based on grid height
        topPlane.transform.position = new Vector3(0 , planeHeight, topLeftHex.GetHexCorners()[2].z + verticalOffset);  // Place at the left edge
        topPlane.transform.rotation = Quaternion.Euler(90, 0, 0);  // Align with the XZ plane
        topPlane.GetComponent<Renderer>().material = blueMaterial;

        // Bottom Plane (below the grid)
        GameObject bottomPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bottomPlane.name = "Bottom Out-of-Bounds";
        bottomPlane.layer = ignoreRaycastLayer;
        bottomPlane.transform.localScale = new Vector3(verticalOffset * 2 , verticalOffset * 2, 1);  // Scale based on grid height
        bottomPlane.transform.position = new Vector3(0 , planeHeight, bottomRightHex.GetHexCorners()[5].z - verticalOffset );  // Place at the left edge
        bottomPlane.transform.rotation = Quaternion.Euler(90, 0, 0);  // Align with the XZ plane
        bottomPlane.GetComponent<Renderer>().material = blueMaterial;

        // Optionally, parent these planes to a game object (e.g., "OutOfBounds")
        GameObject outOfBoundsParent = new GameObject("OutOfBoundsPlanes");
        leftPlane.transform.parent = outOfBoundsParent.transform;
        rightPlane.transform.parent = outOfBoundsParent.transform;
        topPlane.transform.parent = outOfBoundsParent.transform;
        bottomPlane.transform.parent = outOfBoundsParent.transform;
    }
    
    void DetectHexUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;


        if (Physics.Raycast(ray, out hit))
        {
            HexCell hex = hit.collider.GetComponent<HexCell>();

            if (hex != null)
            {
                // Only highlight when the mouse moves to a new hex
                if (hex != lastHoveredHex)
                {
                    if (lastHoveredHex != null)
                    {
                        lastHoveredHex.ResetHighlight();  // Reset the last hovered hex
                    }

                    hex.HighlightHex("hover");  // Highlight the current hex
                    // Debug.Log($"Hovering over hex at: {hex.coordinates}");
                    lastHoveredHex = hex;
                }
                // Check if we are in Standard Pass mode
                if (MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
                {
                    // Assuming GameInputManager is attached to a GameObject in the scene
                    GameInputManager gameInputManager = FindObjectOfType<GameInputManager>();

                    if (gameInputManager != null)
                    {
                        // Highlight the path to the hovered hex if in Easy Mode
                        if (MatchManager.Instance.difficulty_level == 1)
                        {
                            HexCell ballHex = ball.GetCurrentHex();
                            if (ballHex != null)
                            {
                                gameInputManager.HighlightGroundPathToHex(hex);  // Call the method in GameInputManager
                                // Check if the pass is dangerous
                                gameInputManager.CheckForDangerousPath(hex);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("GameInputManager not found in the scene!");
                    }
                }
            }
        }
        else
        {
            // Reset the last hovered hex when the mouse is not over any hex
            if (lastHoveredHex != null)
            {
                lastHoveredHex.ResetHighlight();
                lastHoveredHex = null;
            }
        }
    }
}
