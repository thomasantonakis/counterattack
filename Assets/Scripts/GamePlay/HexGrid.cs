using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System; // Now it will recognize JsonConvert

public class HexGrid : MonoBehaviour
{
    private bool gridInitialized = false;  // Track if the grid is fully created
    private int width = 48;  // Number of hex tiles in the grid's width
    private int height = 36; // Number of hex tiles in the grid's heightb
    public Vector3 gridCenter = new Vector3(0, 0, 0);  // Center of your grid
    float hexRadius = 0.5f;
    [SerializeField] private HexCell hexCellPrefab; // Reference to the hex cell prefab
    public HexCell[,] cells;  // 2D array to hold the cells
    private Color lightColor = new Color(0.2f, 0.8f, 0.2f); 
    private Color darkColor = new Color(0 / 255f, 129 / 255f, 56 / 255f, 255f / 255f);
    private HexCell lastHoveredHex = null;  // Store the last hovered hex
    private Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>> shootingPaths = new Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>>();
    public List<HexCell> highlightedHexes = new List<HexCell>();
    [Header("Dependencies")]
    public Ball ball;
    public GroundBallManager groundBallManager;
    

    private void Start()
    {
        if (cells == null)
        {
            // Initialize the cells array with the correct dimensions
            cells = new HexCell[width, height];
            Debug.Log("HexGrid initialized. Creating grid...");
            CreateGrid();  // Generate the grid
        }
        // Create out-of-bounds planes around the grid
        CreateOutOfBoundsPlanes(this);
        // Path to save or load the shooting paths JSON
        string path = Path.Combine(Application.persistentDataPath, "shootingpaths/shootingPaths.json");
        // if (false)
        if (File.Exists(path))
        {
            Debug.Log("Found JSON with Shooting Paths. Deserializing...");
            string json = File.ReadAllText(path);
            shootingPaths = DeserializeShootingPaths(json);
            AssignShootingPathsToHexes();
        }
        else
        {
            Debug.Log("No shooting paths found. Calculating paths...");
            CalculateShootingPaths();
            SaveShootingPathsToJson();
            AssignShootingPathsToHexes();
        }
        // HighlightShootingHexes();
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(12, 0, 0)).GetHexCenter()}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(19, 0, -4)).GetHexCenter()}");
        // float zNE183 = GetHexCellAt(new Vector3Int(18, 0, 3)).GetHexCorners()[2].z;
        // Debug.Log($"zNE183: {zNE183}");
        // float zSE18_3 = GetHexCellAt(new Vector3Int(18, 0, -3)).GetHexCorners()[0].z;
        // Debug.Log($"zSE18_3: {zSE18_3}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(2, 0, 0)).GetHexCorners()[1]}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(2, 0, 0)).GetHexCorners()[2]}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(2, 0, 0)).GetHexCorners()[3]}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(2, 0, 0)).GetHexCorners()[4]}");
        // Debug.Log($"Thomas Log: {GetHexCellAt(new Vector3Int(2, 0, 0)).GetHexCorners()[5]}");
        // InitializePlayers(10, 10);
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
        gridInitialized = true;  // Mark the grid as initialized
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
        // Assign the layer to the GameObject
        cell.gameObject.layer = LayerMask.NameToLayer("HexGrid");
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
        if (x >= 12 && x <= 18 && ((x % 2 == 0) ? z > -8 && z < 8 : z >= -8 && z <= 7 ))  
        {
            cell.isInPenaltyBox = 1;
        }
        if (x >= -18 && x <= -12 && ((x % 2 == 0) ? z > -8 && z < 8 : z >= -8 && z < 7 ))  
        {
            cell.isInPenaltyBox = -1;
        }

        // Example: Set final thirds
        if ( x <= 18 && x >= 8 && ((x % 2 == 0) ? (z < 13 && z > -13) : (z < 13 && z >= -13)))
        {
            cell.isInFinalThird = 1;
        }
        if (x >= -18 && x <= -8 && ((x % 2 == 0) ? (z < 13 && z > -13) : (z < 13 && z >= -13)))
        {
            cell.isInFinalThird = -1;
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

    public List<PlayerToken> GetDefenders()
    {
        List<PlayerToken> defenderTokens = new List<PlayerToken>();
        List<HexCell> defenderHexes = GetDefenderHexes();
        // Loop through all hex cells in the grid
        foreach (HexCell cell in defenderHexes)
        {
            PlayerToken token  = cell.GetOccupyingToken();
            if (token != null)
            {
                defenderTokens.Add(token);
            }
        }
        return defenderTokens;
    }
    public List<HexCell> GetAttackerHexes()
    {
        List<HexCell> attackerHexes = new List<HexCell>();
        // Loop through all hex cells in the grid
        foreach (HexCell cell in cells)
        {
            if (cell != null && cell.isAttackOccupied)
            {
                attackerHexes.Add(cell);
            }
        }
        return attackerHexes;
    }

    public List<HexCell> GetDefenderNeighbors(List<HexCell> defenderHexes)
    {

        List<HexCell> defenderNeighbors = new List<HexCell>();

        foreach (HexCell defenderHex in defenderHexes)
        {
            HexCell[] neighbors = defenderHex.GetNeighbors(this);  // Assuming GetNeighbors already works
            foreach (HexCell neighbor in neighbors)
            {
                if (neighbor != null && !defenderNeighbors.Contains(neighbor) && !neighbor.isOutOfBounds)
                {
                    defenderNeighbors.Add(neighbor);
                }
            }
        }
        return defenderNeighbors;
    }

    public List<HexCell> GetAttackerHexesinRange(List<HexCell> attackerHexes, int range)
    {
        List<HexCell> hexesInRangeofAttackers = new List<HexCell>();

        foreach (HexCell attackerHex in attackerHexes)
        {
            List<HexCell> rangeOfAttackerHex = GetHexesInRange(this, attackerHex, range);
            foreach (HexCell rangeHex in rangeOfAttackerHex) // get me a more meaningful name to iterate on
            {
                if (rangeHex != null && !hexesInRangeofAttackers.Contains(rangeHex) && !rangeHex.isOutOfBounds)
                {
                    hexesInRangeofAttackers.Add(rangeHex);
                }
            }
        }
        return hexesInRangeofAttackers;
    }

    public bool IsPassDangerous(List<HexCell> pathHexes, List<HexCell> defenderNeighbors, bool isGk = false)
    {
        // Check if any of the path hexes are in the defender's ZOI (neighbors)
        foreach (HexCell pathHex in pathHexes)
        {
            if (isGk && pathHex != pathHexes.Last()) continue;  // Skip checking for GK
            if (defenderNeighbors.Contains(pathHex) && !pathHex.isAttackOccupied)
            {
                // Debug.Log($"Hex {pathHex.coordinates} is within a defender's ZOI, making the pass dangerous.");
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
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

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

    public void ClearHighlightedHexes()
    {
        foreach (HexCell hex in highlightedHexes)
        {
            if (hex != null) 
            {
                hex.ResetHighlight();  // Assuming this method resets the color of the hex
            }
        }
        highlightedHexes.Clear();  // Clear the list after resetting
    }
    
    void DetectHexUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            HexCell hoveredHex = hit.collider.GetComponent<HexCell>();
            if (hoveredHex != null && hoveredHex != lastHoveredHex)
            {
                if (lastHoveredHex != null)
                {
                    lastHoveredHex.ResetHighlight();
                }
                if (hoveredHex != null 
                    && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt
                    && MatchManager.Instance.difficulty_level == 1
                )
                {
                    // HighlightGroundPathToHex(hoveredHex);
                    var (isValid, isDangerous, pathHexes) = groundBallManager.ValidateGroundPassPath(hoveredHex, 11); // Use GameInputManager logic
                    if (isValid)
                    {
                        ClearHighlightedHexes();
                        groundBallManager.HighlightValidGroundPassPath(pathHexes, isDangerous); // Highlight based on danger
                    }
                }
            }
            else
            {
                if (lastHoveredHex != null)
                {
                    lastHoveredHex.ResetHighlight();
                    lastHoveredHex = null;
                }
            }
        }
    }

    // This method returns a list of valid movement hexes within the given range
    public List<HexCell> GetValidMovementHexes(HexCell startHex, int range)
    {
        List<HexCell> validHexes = new List<HexCell>();

        // Loop through all hexes in the grid to find valid hexes
        foreach (HexCell hex in cells)
        {
            int distance = HexGridUtils.GetHexDistance(startHex.coordinates, hex.coordinates);
            if (distance <= range && !hex.isAttackOccupied && !hex.isDefenseOccupied)
            {
                validHexes.Add(hex);
                Debug.Log($"Valid hex found: {hex.name}");
            }
        }
        Debug.Log($"Total valid hexes: {validHexes.Count}");
        return validHexes;
    }

    public void CalculateShootingPaths()
    {
        List<HexCell> allHexes = new List<HexCell>();
        int rows = cells.GetLength(0); // Assuming cells is a 2D array
        int cols = cells.GetLength(1);

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < cols; z++)
            {
                HexCell cell = cells[x, z];
                if (cell != null) // Ensure cell exists
                {
                    allHexes.Add(cell);
                }
            }
        }
        // List<HexCell> allHexes = cells(); // All hexes on the grid
        List<HexCell> canShootToHexes = allHexes.Where(hex => Mathf.Abs(hex.coordinates.x) == 19 && hex.coordinates.z >= -4 && hex.coordinates.z <= 3).ToList();
        // Debug.Log($"Can shoot to {canShootToHexes.Count} hexes.");
        List<HexCell> potentialCanShootFromHexes = allHexes.Where(hex => Mathf.Abs(hex.coordinates.x) >= 8 && !hex.isOutOfBounds).ToList();
        // List<HexCell> potentialCanShootFromHexes = new List<HexCell>
        // {
        //   GetHexCellAt(new Vector3Int(12, 0, 0)),
        //   GetHexCellAt(new Vector3Int(12, 0, 6)),
        //   GetHexCellAt(new Vector3Int(12, 0, -6))
        //   // GetHexCellAt(new Vector3Int(16, 0, -12))
        // };

        // Get NE and SE corner z-values for validation
        // HexCell hex183 = GetHexCellAt(new Vector3Int(18, 0, 3)); // NE corner
        HexCell hex18_3 = GetHexCellAt(new Vector3Int(18, 0, -3)); // SE corner
        // float zNE183 = hex183.GetHexCorners()[2].z;
        float zSE18_3 = hex18_3.GetHexCorners()[0].z;

        // Outer dictionary: CanShootFrom -> (CanShootTo -> Path)
        shootingPaths = new Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>>();

        foreach (HexCell fromHex in potentialCanShootFromHexes)
        {
            foreach (HexCell toHex in canShootToHexes)
            {
                if (fromHex.coordinates.x * toHex.coordinates.x < 0) 
                {
                    Debug.Log($"Skipping {fromHex.coordinates} to {toHex.coordinates} because they are on opposite sides of the field.");
                    continue;
                }
                Vector3Int fromCubeCoords = HexGridUtils.OffsetToCube(fromHex.coordinates.x, fromHex.coordinates.z);
                Vector3Int toCubeCoords = HexGridUtils.OffsetToCube(toHex.coordinates.x, toHex.coordinates.z);
                // Calculate the distance
                int distance = HexGridUtils.GetHexDistance(fromCubeCoords, toCubeCoords);
                Debug.Log($"Distance from {fromHex.coordinates} to {toHex.coordinates}: {distance}");
                if (distance > 11) 
                {
                    Debug.Log($"Distance from {fromHex.coordinates} to {toHex.coordinates} is too far: {distance}");
                    continue;
                }

                float intersectionZ = CalculateIntersectionWithGoalLine(fromHex, toHex);

                // Validate the intersection point
                if (Mathf.Abs(intersectionZ) >= MathF.Abs(zSE18_3))
                {
                    Debug.Log($"Line that connects centers of {fromHex.coordinates} and {toHex.coordinates} does not intersect with goal line. IntersectionZ: {intersectionZ}, zSE18_3: {zSE18_3}");
                    continue;
                }

                // Calculate the path using the ball's radius
                List<HexCell> path = groundBallManager.CalculateThickPath(fromHex, toHex, ball.ballRadius);
                if (path == null || path.Count == 0)
                {
                    Debug.Log($"No path found from {fromHex.coordinates} to {toHex.coordinates}.");
                    continue;
                }

                // Store the result in the dictionary
                if (!shootingPaths.ContainsKey(fromHex))
                {
                    shootingPaths[fromHex] = new Dictionary<HexCell, List<HexCell>>();
                }
                shootingPaths[fromHex][toHex] = path;

                // Mark the "from" hex as a valid shooting origin
                fromHex.CanShootFrom = true;
                // Debug.Log($"Shooting path found from {fromHex.coordinates} to {toHex.coordinates}.");
            }
            if (fromHex.CanShootFrom)
            {
                Debug.Log($"Finished calculating shooting paths from {fromHex.coordinates}, found {shootingPaths[fromHex].Count()} possible targets");
            }
            else
            {
                Debug.Log($"Finished calculating shooting paths from {fromHex.coordinates}, without finding ANY possible targets");
            }
        }
        Debug.Log("Shooting paths calculated!");
    }

    private void AssignShootingPathsToHexes()
    {
        if (shootingPaths == null)
        {
            Debug.LogError("shootingPaths is null! Ensure CalculateShootingPaths is called before this.");
            return;
        }

        foreach (var fromHex in shootingPaths.Keys)
        {
            if (fromHex == null)
            {
                Debug.LogWarning("Found null fromHex in shootingPaths keys.");
                continue; // Skip null keys
            }

            fromHex.CanShootFrom = true; // Assign CanShootFrom to true

            foreach (var toHex in shootingPaths[fromHex].Keys)
            {
                if (toHex == null)
                {
                    Debug.LogWarning($"Null toHex found in shootingPaths for {fromHex.coordinates}");
                    continue; // Skip null values
                }

                // Assign the dictionary of CanShootTo paths
                if (fromHex.ShootingPaths == null)
                {
                    // Debug.Log($"There was no ShootingPaths in fromHex: {fromHex.coordinates}");
                    fromHex.ShootingPaths = new Dictionary<HexCell, List<HexCell>>();
                }

                fromHex.ShootingPaths[toHex] = shootingPaths[fromHex][toHex];
            }
        }
        Debug.Log("Finished Assigning CanShootFrom and CanShootTo paths to hexes.");
    }
    private Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>> DeserializeShootingPaths(string json)
    {
        // Deserialize into a structure with string keys first
        var serializablePaths = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(json);

        var rehydratedPaths = new Dictionary<HexCell, Dictionary<HexCell, List<HexCell>>>();

        foreach (var fromEntry in serializablePaths)
        {
            Vector3Int fromCoords = ParseVector3Int(fromEntry.Key); // Convert string back to Vector3Int
            HexCell fromHex = GetHexCellAt(fromCoords);
            if (fromHex == null) continue;

            rehydratedPaths[fromHex] = new Dictionary<HexCell, List<HexCell>>();

            foreach (var toEntry in fromEntry.Value)
            {
                Vector3Int toCoords = ParseVector3Int(toEntry.Key); // Convert string back to Vector3Int
                HexCell toHex = GetHexCellAt(toCoords);
                if (toHex == null) continue;

                // Convert path strings back to HexCell objects
                List<HexCell> path = toEntry.Value
                    .Select(coordString => GetHexCellAt(ParseVector3Int(coordString)))
                    .Where(hex => hex != null)
                    .ToList();

                rehydratedPaths[fromHex][toHex] = path;
            }
        }

        return rehydratedPaths;
    }

    private Vector3Int ParseVector3Int(string vectorString)
    {
        vectorString = vectorString.Trim('(', ')'); // Remove parentheses
        string[] parts = vectorString.Split(',');  // Split by commas
        int x = int.Parse(parts[0].Trim());
        int y = int.Parse(parts[1].Trim());
        int z = int.Parse(parts[2].Trim());
        return new Vector3Int(x, y, z);
    }

    private void SaveShootingPathsToJson()
    {
        var serializablePaths = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var fromHex in shootingPaths.Keys)
        {
            string fromKey = fromHex.coordinates.ToString(); // Convert Vector3Int to string
            serializablePaths[fromKey] = new Dictionary<string, List<string>>();

            foreach (var toHex in shootingPaths[fromHex].Keys)
            {
                string toKey = toHex.coordinates.ToString(); // Convert Vector3Int to string
                List<string> pathKeys = shootingPaths[fromHex][toHex]
                    .Select(pathHex => pathHex.coordinates.ToString()) // Convert path HexCells to strings
                    .ToList();

                serializablePaths[fromKey][toKey] = pathKeys;
            }
        }

        // Serialize to JSON
        string json = JsonConvert.SerializeObject(serializablePaths, Formatting.Indented);
        string filePath = Path.Combine(Application.persistentDataPath, "shootingpaths/shootingPaths.json");
        File.WriteAllText(filePath, json);
        Debug.Log($"Shooting paths saved to {filePath}");
    }

    private void HighlightShootingHexes()
    {
        Debug.Log("Hello from HighlightShootingHexes");
        foreach (var fromHex in shootingPaths.Keys) // Keys are already HexCell objects
        {
            float count = shootingPaths[fromHex].Count;
            Debug.Log($"Hex {fromHex.coordinates} can shoot to {shootingPaths[fromHex].Count} hexes.");
            if (fromHex != null) 
            {
                fromHex.HighlightHex("CanShootFrom", count);
            }

            // Uncomment the following if you also want to highlight the paths to CanShootTo hexes
            // foreach (var toHex in shootingPaths[fromHex].Keys)
            // {
            //     List<HexCell> path = shootingPaths[fromHex][toHex];
            //     foreach (HexCell hex in path)
            //     {
            //         hex.HighlightHex("PathHighlight");
            //     }
            // }
        }
    }

    private float CalculateIntersectionWithGoalLine(HexCell fromHex, HexCell toHex)
    {

        // Calculate the intersection z-value
        Vector3 fromCenter = fromHex.GetHexCenter();
        Vector3 toCenter = toHex.GetHexCenter();

        // Extract world coordinates
        float x1 = fromCenter.x;
        float z1 = fromCenter.z;
        float x2 = toCenter.x;
        float z2 = toCenter.z;

        // Check if the line is vertical
        if (Mathf.Abs(x2 - x1) < Mathf.Epsilon)
        {
            Debug.LogError("The line is vertical. Cannot calculate intersection with goal line.");
            return float.NaN; // Return invalid value
        }

        // Calculate the slope and intercept of the line in world coordinates
        float slope = (z2 - z1) / (x2 - x1);
        float intercept = z1 - (slope * x1);
        float xNE183 = GetHexCellAt(new Vector3Int(18, 0, 3)).GetHexCorners()[2].x;
        float xSW_18_3 = GetHexCellAt(new Vector3Int(-18, 0, -3)).GetHexCorners()[5].x;
        Debug.Log($"slope is {slope}, intercept is {intercept}, xNE183 is {xNE183}, xSW_18_3 is {xSW_18_3}");
        // Calculate the intersection Z value when x = 18 (goal line)
        float xFinal = fromHex.coordinates.x > 0 ? xNE183 : xSW_18_3;
        float intersectionZ = (slope * xFinal) + intercept;
        Debug.Log($"Intersection Z value: {intersectionZ} for line from {fromHex.coordinates} to {toHex.coordinates}");
        return intersectionZ; // Intersection is out of bounds
        // return float.NaN;
    }

    public PlayerToken GetDefendingGK()
    {
        // **Step 1: Find the defending Goalkeeper**
        PlayerToken defendingGK = GetDefenders().Find(gk => gk.IsGoalKeeper); // TODO is not benched
        return defendingGK;
    }
    
    public List<HexCell> GetSavableHexes()
    {
        // **Step 1: Find the defending Goalkeeper**
        PlayerToken defendingGK = GetDefendingGK();
        HexCell gkHex = defendingGK?.GetCurrentHex();
        // **Step 2: Calculate Saveable Hexes**
        List<HexCell> saveableHexes = new List<HexCell>();
        if (gkHex != null)
        {
            saveableHexes.Add(gkHex);
            for (int i = 1; i <= 3; i++)  // Add 3 hexes forward and backward in the same column
            {
                saveableHexes.Add(GetHexCellAt(new Vector3Int(gkHex.coordinates.x, 0, gkHex.coordinates.z + i)));
                saveableHexes.Add(GetHexCellAt(new Vector3Int(gkHex.coordinates.x, 0, gkHex.coordinates.z - i)));
            }
        }
        return saveableHexes;
    }
}
