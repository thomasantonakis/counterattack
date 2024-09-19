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
    private HexCell lastHoveredHex = null;  // Store the last hovered hex

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

        // Dark Green Hexes
        if ((x % 2 == 0) ? (z % 3 == 0) : ((z+2) % 3 == 0))
        {
            cell.isDark = true;
        }

        // Debug to visually differentiate hexes in Unity (optional)
        // if (cell.isDark) cell.GetComponent<Renderer>().material.color = new Color(84 / 255f, 207 / 255f, 76 / 255f, 255f / 255f);
        if (cell.isDark) cell.GetComponent<Renderer>().material.color = new Color(0 / 255f, 129 / 255f, 56 / 255f, 255f / 255f);
        // if (cell.isDark) cell.GetComponent<Renderer>().material.color = Color.blue;
        // else if (cell.isKickOff) cell.GetComponent<Renderer>().material.color = Color.red;
        // else if (cell.isOutOfBounds) cell.GetComponent<Renderer>().material.color = Color.blue;
        // else if (cell.isDifficultShotPosition) cell.GetComponent<Renderer>().material.color = Color.magenta;
        // else if (cell.isInPenaltyBox) cell.GetComponent<Renderer>().material.color = Color.yellow;
        // else if (cell.isInFinalThird) cell.GetComponent<Renderer>().material.color = Color.green;
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

    public List<HexCell> GetHexesInRange(HexCell centerHex, int range)
    {
        List<HexCell> hexesInRange = new List<HexCell>();

        Vector3Int centerCoords = centerHex.coordinates;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dz = Mathf.Max(-range, -dx - range); dz <= Mathf.Min(range, -dx + range); dz++)
            {
                int dy = -dx - dz;
                Vector3Int currentCoords = new Vector3Int(centerCoords.x + dx, centerCoords.y + dy, centerCoords.z + dz);

                HexCell hex = GetHexCellAt(currentCoords);
                if (hex != null)
                {
                    hexesInRange.Add(hex);
                }
            }
        }

        return hexesInRange;
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

                    hex.HighlightHex();  // Highlight the current hex
                    // Debug.Log($"Hovering over hex at: {hex.coordinates}");
                    lastHoveredHex = hex;
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
