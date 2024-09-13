using UnityEngine;
using TMPro;  // Import TextMeshPro namespace

[ExecuteInEditMode]
public class HexCell : MonoBehaviour
{
    public Vector3Int coordinates;
    // Hex features for the soccer pitch
    public bool isKickOff = false;
    public bool isOutOfBounds = false;
    public bool isInPenaltyBox = false;
    public bool isInFinalThird = false;
    public bool isDifficultShotPosition = false;
    public TextMeshPro coordinatesText;  // Reference for the TextMeshPro
    private Renderer hexRenderer;
    public float hexRadius = 0.5f;
    private Color originalColor;
    private Color highlightColor = Color.black;  // Color to highlight the hex
    private static readonly Vector3Int[] directions = {
        new Vector3Int(0, 0, 1),   // Top
        new Vector3Int(1, 0, 0),   // Top-right
        new Vector3Int(1, 0, -1),  // Bottom-right
        new Vector3Int(0, 0, -1),  // Bottom
        new Vector3Int(-1, 0, -1), // Bottom-left
        new Vector3Int(-1, 0, 0)   // Top-left
    };

    void Start()
    {
        // Store the renderer and the original material color
        hexRenderer = GetComponent<Renderer>();
        originalColor = hexRenderer.material.color;
    }

    public void SetCoordinates(int x, int z)
    {
        coordinates = new Vector3Int(x, 0, z);
        if (coordinatesText != null) 
        {
            coordinatesText.text = $"{x}, {z}";  // Set the text to display the coordinates
        }
    }
    public void HighlightHex()
    {
        // Set a highlight color (black outline)
        hexRenderer.material.color = highlightColor;
    }

    public void ResetHighlight()
    {
        // Reset to the original color
        hexRenderer.material.color = originalColor;
    }

    // Get world coordinates of the 6 corners of this hex
    public Vector3[] GetHexCorners()
    {
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

    public HexCell[] GetNeighbors(HexGrid grid)
    {
        HexCell[] neighbors = new HexCell[6];
        for (int i = 0; i < 6; i++)
        {
            Vector3Int neighborCoords = coordinates + directions[i];
            neighbors[i] = grid.GetHexCellAt(neighborCoords);
        }
        return neighbors;
    }
}
