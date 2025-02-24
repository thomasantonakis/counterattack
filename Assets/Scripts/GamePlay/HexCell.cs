using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;  // Import TextMeshPro namespace

[ExecuteInEditMode]
public class HexCell : MonoBehaviour
{
    public Vector3Int coordinates;
    public float hexRadius;
    public bool isKickOff = false;
    public bool isOutOfBounds = false;
    public int isInPenaltyBox = 0; // -1 Left, 0 No, 1 Right
    public int isInFinalThird = 0; // -1 Left, 0 No, 1 Right
    public int isInGoal = 0; // -1 Left, 0 No, 1 Right
    public bool isDifficultShotPosition = false;
    public int isInCircle = 0; // -1 Left, 0 No, 1 Right, 5 Center
    public bool isDark = false;
    public bool isDefenseOccupied = false;
    public bool isAttackOccupied = false;
    public TextMeshPro coordinatesText;  // Reference for the TextMeshPro
    private Renderer hexRenderer;
    private Color originalColor;
    public PlayerToken occupyingToken;
    public bool CanShootFrom = false; // Displayed in Inspector
    public bool CanHeadFrom = false; // Displayed in Inspector
    public Dictionary<HexCell, List<HexCell>> ShootingPaths; // Dictionary of shooting paths
    public Dictionary<HexCell, List<HexCell>> HeadingPaths; // Dictionary of heading paths

    void Awake()
    {
        // Use MeshRenderer directly instead of a generic Renderer
        hexRenderer = GetComponent<MeshRenderer>();
        if (hexRenderer == null)
        {
            Debug.LogError("HexCell MeshRenderer is missing! Check this cell's prefab or components.");
        }
    }

    public void InitializeHex(Color initialColor)
    {
        if (hexRenderer == null)
        {
            Debug.LogError("HexCell MeshRenderer is still missing during InitializeHex! Initialization skipped for this cell.");
            return;  // Exit if no MeshRenderer is found
        }
        // Set the original color and apply it to the hex
        originalColor = initialColor;
        hexRenderer.material.color = originalColor;
    }

    public void SetCoordinates(int x, int z)
    {
        coordinates = new Vector3Int(x, 0, z);
        if (coordinatesText != null) 
        {
            coordinatesText.text = $"{x}, {z}";  // Set the text to display the coordinates
        }
    }
    
    public void HighlightHex(string reason, float darkness = 0)
    {
        if (hexRenderer == null)
        {
            Debug.LogError($"Hex {coordinates} is missing a renderer. Cannot highlight.");
            return;
        }
        if (
            isInGoal != 0 && transform.position.y == 0
            // TODO: if we are in a proper state (Attackig MP, Reposition, Shot, Header at GOAL)
        )
        {
            transform.position += Vector3.up * 0.03f; // Put it back.
        }
        // Define your color logic
        Color colorToApply = Color.white; // Default to white

        // Apply the color to the hex based on the reason
        // Debug.Log($"Highlighting hex {name} for reason: {reason}");
        switch (reason)
        {
            case "hover":
                colorToApply = originalColor * 0.5f;  // Darken the hex on hover
                break;
            case "ballPath":
                colorToApply = Color.blue;  // Use the provided color for the ball path
                break;
            case "dangerousPass":
                colorToApply = Color.magenta;  // Use the provided color for the ball path
                break;
            case "impossiblePass":
                colorToApply = Color.magenta * 0.5f;  // Use the provided color for the ball path
                break;
            case "highPass":
                colorToApply = Color.yellow;
                break;
            case "highPassTarget":
                colorToApply = new Color(51, 204, 242);
                break;
            case "longPass":
                colorToApply = Color.blue * 3f;
                break;
            case "longPassDifficult":
                colorToApply = Color.blue * 2f;
                break;
            case "isDefenseOccupied":
                colorToApply = Color.red;
                break;
            case "isAttackOccupied":
                colorToApply = Color.green;
                break;
            case "PaceAvailable":
                colorToApply = Color.yellow;
                break;
            case "DefenderZOI":
                colorToApply = Color.yellow * 0.5f;
                break;
            case "reposition":
                colorToApply = Color.gray;// * 0.5f;
                break;
            case "nutmeggableDef":
                colorToApply = new Color(51, 204, 242);// * 0.5f;
                break;
            case "CanShootFrom":
                colorToApply = Color.white / darkness;// * 0.5f;
                break;
            // Add other cases if needed
            default:
                colorToApply = originalColor;  // Reset to original color if no valid reason
                break;
        }
        // Set the color directly to override all material properties
        hexRenderer.material.SetColor("_Color", colorToApply);
        // // Apply the color to the hex based on the reason
        // hexRenderer.material.color = reason switch
        // {
        //   "hover" => originalColor * 0.5f,// Darken the hex on hover
        //   "ballPath" => Color.blue,// Use the provided color for the ball path
        //   "dangerousPass" => Color.magenta,// Use the provided color for the ball path
        //   "impossiblePass" => Color.magenta * 0.5f,// Use the provided color for the ball path
        //   "longPass" => Color.blue * 3f,
        //   "longPassDifficult" => Color.blue * 2f,
        //   "isDefenseOccupied" => Color.red,
        //   "isAttackOccupied" => Color.green,
        //   // Add other cases if needed
        //   _ => originalColor,// Reset to original color if no valid reason
        // };
    }

    public void ResetHighlight()
    {
        if (hexRenderer == null)
        {
            return;
        }
        if (isInGoal != 0 && transform.position.y > 0)
        {
            transform.position -= Vector3.up * 0.03f; // Put it back.
        }
        // If the hex is defense-occupied, reset it to red, else reset to the original color
        if (isDefenseOccupied)
        {
            hexRenderer.material.color = Color.red;
        }
        else if (isAttackOccupied)
        {
            hexRenderer.material.color = Color.green;
        }
        else
        {
            hexRenderer.material.color = originalColor;  // Reset to either light or dark green
        }
    }

    public Vector3[] GetHexCorners()
    {
        // [0] SE
        // [1] E
        // [2] NE
        // [3] NW
        // [4] W
        // [5] SW
        Vector3[] corners = new Vector3[6];
        float radius = 0.5f;  // Adjust this based on your hex tile size

        // Adjust the angle for the 90-degree rotation applied in CreateCell
        Quaternion rotation = Quaternion.Euler(0, 90, 0);  // Apply 90 degrees rotation on Y-axis

        for (int i = 0; i < 6; i++)
        {
            // Calculate the angle for each corner (flat-topped hexagon)
            float angle_deg = 60 * i + 30;  // Offset by 30 degrees for flat-topped hexes
            float angle_rad = Mathf.Deg2Rad * angle_deg;  // Convert degrees to radians

            // Calculate each corner's position relative to the hex's center (x, z)
            Vector3 corner = new Vector3(
                radius * Mathf.Cos(angle_rad),  // X component
                transform.position.y,           // Y component stays the same
                radius * Mathf.Sin(angle_rad)   // Z component
            );

            // Apply the rotation to each corner
            corners[i] = transform.position + rotation * corner;  // Apply rotation and add to hex's center position

            // Log each corner for debugging
            // Debug.Log($"Hex ({coordinates.x}, {coordinates.z}) - Corner [{i}] found at ({corners[i].x}, {corners[i].y}, {corners[i].z})");
        }
        return corners;
    }

    public Vector3[] GetHexEdgeMidpoints()
    {
        // [0] SW
        // [1] NW
        // [2] N
        // [3] NE
        // [4] SE
        // [5] S
        Vector3[] corners = GetHexCorners();  // First, calculate the corners
        Vector3[] midpoints = new Vector3[6];  // Array to store the midpoints of the 6 edges

        for (int i = 0; i < 6; i++)
        {
            // Get the next corner index (wrap around for the last corner)
            int nextCornerIndex = (i + 1) % 6;

            // Calculate the midpoint between corner i and the next corner
            midpoints[i] = (corners[i] + corners[nextCornerIndex]) / 2;

            // Log each midpoint for debugging
            // Debug.Log($"Hex ({coordinates.x}, {coordinates.z}) - Edge midpoint [{i}] found at ({midpoints[i].x}, {midpoints[i].y}, {midpoints[i].z})");
        }

        return midpoints;
    }

    public Vector3 GetHexCenter()
    {
        // The center of the hex is simply its current position
        Vector3 center = transform.position;

        // Log the center for debugging purposes
        // Debug.Log($"Hex ({coordinates.x}, {coordinates.z}) - Center found at ({center.x}, {center.y}, {center.z})");

        return center;
    }

    // Directions for odd and even columns
    private static readonly Vector2Int[] evenColumnDirections = new Vector2Int[]
    {
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, -1), // Southwest
        new Vector2Int(-1, 0),  // Northwest
        new Vector2Int(0, 1),    // North
        new Vector2Int(1, 0),   // Northeast
        new Vector2Int(1, -1)  // Southeast
    };

    private static readonly Vector2Int[] oddColumnDirections = new Vector2Int[]
    {
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0),  // Southwest
        new Vector2Int(-1, 1),  // Northwest
        new Vector2Int(0, 1),    // North
        new Vector2Int(1, 1),   // Northeast
        new Vector2Int(1, 0)   // Southeast
    };

    // Function to return the proper direction set based on the x coordinate (even or odd)
    public Vector2Int[] GetDirectionVectors()
    {
        // Return the appropriate direction vectors based on the x coordinate
        return (coordinates.x % 2 == 0) ? evenColumnDirections : oddColumnDirections;
    }

    public HexCell[] GetNeighbors(HexGrid grid)
    {
        List<HexCell> validNeighbors = new List<HexCell>();
        HexCell[] neighbors = new HexCell[6];
        // Choose directions based on whether the row (z) is even or odd
        Vector2Int[] offsetDirections = GetDirectionVectors();  // Get appropriate directions

        for (int i = 0; i < offsetDirections.Length; i++)
        {
            int newX = coordinates.x + offsetDirections[i].x;
            int newZ = coordinates.z + offsetDirections[i].y;

            HexCell neighborHex = grid.GetHexCellAt(new Vector3Int(newX, 0, newZ));  // Fetch neighbor in offset coords
            if (
                neighborHex != null 
                && (
                    !neighborHex.isOutOfBounds
                    || neighborHex.isInGoal != 0 // A out of bounds neighbor which is inGoal, it's a valid neighbor.
                )
            )
            {
                // neighbors[i] = neighborHex;
                validNeighbors.Add(neighborHex);  // ✅ Only add valid neighbors
            }
        }
        // Debug log the neighbors for this hex
        // Debug.Log($"Neighbors of hex ({coordinates.x}, {coordinates.z}): {string.Join(", ", neighbors.Select(n => n?.coordinates.ToString() ?? "null"))}");
        // return neighbors;
        return validNeighbors.ToArray();
    }

    public PlayerToken GetOccupyingToken()
    {
        return occupyingToken;
    }

}
