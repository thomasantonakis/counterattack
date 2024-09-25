using UnityEngine;
using System.Collections.Generic;

public class PitchLines : MonoBehaviour
{
    public HexGrid hexGrid;  // Reference to the hex grid to get hex positions
    private int ignoreRaycastLayer; // Declare this at the class level
    List<GameObject> pitchObjects = new List<GameObject>();


    void Start()
    {
        // Wait for the grid to initialize, then draw lines
        StartCoroutine(WaitForGridAndDrawLines());
        // Example: Assuming "lineObjects" is an array/list of the line objects
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        foreach (GameObject pitchObject in pitchObjects)
        {
            pitchObject.layer = ignoreRaycastLayer;
        }
    }

    // Coroutine to wait until the grid is ready, then draw lines
    System.Collections.IEnumerator WaitForGridAndDrawLines()
    {
        // Wait until the HexGrid has finished creating cells
        yield return new WaitUntil(() => hexGrid != null && hexGrid.IsGridInitialized());  // Check if grid is ready

        Debug.Log("Grid is ready, drawing lines...");
        // Dummy Line
        // DrawLineAsQuad(new Vector3(0, 0, 0), new Vector3(10, 0, 0), 0.15f);
        
        // Pitch boundaries
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12)).GetHexCorners()[5]
            , hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12)).GetHexCorners()[0]
            , 0.05f
            , "Bottom Side Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12)).GetHexCorners()[0]
            , hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12)).GetHexCorners()[2]
            , 0.05f
            , "Right Goal Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12)).GetHexCorners()[2]
            , hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12)).GetHexCorners()[3]
            , 0.05f
            , "Top Side Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12)).GetHexCorners()[3]
            , hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12)).GetHexCorners()[5]
            , 0.05f
            , "Left Goal Line")
        );
        // Half Court Line
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(0, 0, 12)).GetHexEdgeMidpoints()[2]
            , hexGrid.GetHexCellAt(new Vector3Int(0, 0, -12)).GetHexEdgeMidpoints()[5]
            , 0.05f
            , "Half Court Line")
        );
        // Left Penalty Box
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-12, 0, -7)).GetHexEdgeMidpoints()[5]
            , hexGrid.GetHexCellAt(new Vector3Int(-12, 0, 7)).GetHexEdgeMidpoints()[2]
            , 0.05f
            , "Left Pen Box Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -7)).GetHexCorners()[5]
            , hexGrid.GetHexCellAt(new Vector3Int(-12, 0, -7)).GetHexEdgeMidpoints()[5]
            , 0.05f
            , "Left Pen Bottom Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 7)).GetHexCorners()[3]
            , hexGrid.GetHexCellAt(new Vector3Int(-12, 0, 7)).GetHexEdgeMidpoints()[2]
            , 0.05f
            , "Left Pen Top Line")
        );
        // Right Penalty Box
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(12, 0, -7)).GetHexEdgeMidpoints()[5]
            , hexGrid.GetHexCellAt(new Vector3Int(12, 0, 7)).GetHexEdgeMidpoints()[2]
            , 0.05f
            , "Right Pen Box Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -7)).GetHexCorners()[0]
            , hexGrid.GetHexCellAt(new Vector3Int(12, 0, -7)).GetHexEdgeMidpoints()[5]
            , 0.05f
            , "Right Pen Bottom Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 7)).GetHexCorners()[2]
            , hexGrid.GetHexCellAt(new Vector3Int(12, 0, 7)).GetHexEdgeMidpoints()[2]
            , 0.05f
            , "Right Pen Top Line")
        );
        // Left 6-yard Box
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 5)).GetHexCenter()
            , 0.05f
            , "Left 6yB Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -5)).GetHexCorners()[4]
            , 0.05f
            , "Left 6yB Bottom")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 5)).GetHexCorners()[4]
            , 0.05f
            , "Left 6yB Top")
        );
        // Right 6-yard Box
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(16, 0, 5)).GetHexCenter()
            , 0.05f
            , "Right 6yB Line")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(18, 0, -5)).GetHexCorners()[1]
            , 0.05f
            , "Right 6yB Bottom")
        );
        pitchObjects.Add(
          DrawLineAsQuad(
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 5)).GetHexCenter()
            , hexGrid.GetHexCellAt(new Vector3Int(18, 0, 5)).GetHexCorners()[1]
            , 0.05f
            , "Right 6yB Top")
        );
        // Load Sprite for Ball Spots
        Sprite circleSprite = Resources.Load<Sprite>("circle");
        // Kick Off Point
        pitchObjects.Add(DrawDot(hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0)).GetHexCenter(), 0.08f, circleSprite, "Kick Off"));
        // Right Pen Spot
        pitchObjects.Add(DrawDot(hexGrid.GetHexCellAt(new Vector3Int(14, 0, 0)).GetHexCenter(), 0.08f, circleSprite ,"Right Pen Spot"));
        // Left Pen Spot
        pitchObjects.Add(DrawDot(hexGrid.GetHexCellAt(new Vector3Int(-14, 0, 0)).GetHexCenter(), 0.08f, circleSprite, "Left Pen Spot"));
        // Half court circle
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0)).GetHexCenter() //center
            , 3f // radius float
            , 360 // segment integer
            , 0.05f // thichness float
            , 0 // start angle integer
            , 360 // end angle integer
            , "Half Court Circle")
        );
        // Right Penalty Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 0)).GetHexCenter() //center
            , 3f // radius float
            , 360 // segment integer
            , 0.05f // thichness float
            , 120 // start angle integer
            , 240 // end angle integer
            , "Right Penalty Arc")
        );
        // Left Penalty Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, 0)).GetHexCenter() //center
            , 3f // radius float
            , 360 // segment integer
            , 0.05f // thichness float
            , 300 // start angle integer
            , 420 // end angle integer
            , "Left Penalty Arc")
        );
        // Bottom Left Corner Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12)).GetHexCorners()[5] //center
            , .3f // radius float
            , 360 // segment integer
            , 0.03f // thichness float
            , 0 // start angle integer
            , 90 // end angle integer
            , "Bottom Left Corner")
        );
        // Top Left Corner Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12)).GetHexCorners()[3] //center
            , .3f // radius float
            , 360 // segment integer
            , 0.03f // thichness float
            , 270 // start angle integer
            , 360 // end angle integer
            , "Top Left Corner")
        );
        // Bottom Right Corner Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12)).GetHexCorners()[0] //center
            , .3f // radius float
            , 360 // segment integer
            , 0.03f // thichness float
            , 90 // start angle integer
            , 180 // end angle integer
            , "Bottom Right Corner")
        );
        // Top Right Corner Arc
        pitchObjects.Add(
          DrawCircleOrArc(
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12)).GetHexCorners()[2] //center
            , .3f // radius float
            , 360 // segment integer
            , 0.03f // thichness float
            , 180 // start angle integer
            , 270 // end angle integer
            , "Top Right Corner")
        );
        Debug.Log("Lines Drawn...");
    }
    
    // Draw a line between two points with a given thickness
    public GameObject DrawLineAsQuad(Vector3 start, Vector3 end, float thickness, string lineName = "Line", Transform parent = null)
    {
        // Create a new GameObject with a Quad
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = lineName;  // Assign the name to the GameObject
        line.layer = ignoreRaycastLayer; // Ignore from RayCasts
        // Set the parent to the specified parent, or keep it unparented if parent is null
        if (parent != null)
        {
            line.transform.parent = parent;
        }
        else 
        {
            line.transform.parent = transform;  // Set the parent to PitchLines or any other game object
        }

        // Set the position at the midpoint between the start and end points
        Vector3 midPoint = (start + end) / 2f;
        line.transform.position = new Vector3(midPoint.x, 0.03f, midPoint.z);  // Slightly above the ground

        // Calculate the direction of the line
        Vector3 direction = end - start;
        float lineLength = direction.magnitude;

        // Scale the quad to match the length of the line and thickness
        line.transform.localScale = new Vector3(lineLength, thickness, 1);  // X represents length, Y is thickness

        // Rotate the quad to align with the direction of the line
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        line.transform.rotation = Quaternion.Euler(90, -angle, 0);  // Rotate on the Y axis to align with the line

        // Assign a simple material to the quad
        Renderer renderer = line.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Unlit/Color"));
        renderer.material.color = Color.white;  // Set the color of the line (white in this case)

        // Debug.Log($"Quad line drawn from {start} to {end} with thickness {thickness}");
        return line;
    }

    public Vector3[] GetCirclePoints(Vector3 center, float radius, int segmentCount, float startAngle = 0, float endAngle = 360)
    {
        Vector3[] circlePoints = new Vector3[segmentCount];
        float angleStep = (endAngle - startAngle) / segmentCount;  // The angle between each point

        for (int i = 0; i < segmentCount; i++)
        {
            // Calculate the angle for this segment
            float currentAngle_deg = startAngle + angleStep * i;
            float currentAngle_rad = Mathf.Deg2Rad * currentAngle_deg;

            // Calculate the position of the point on the circle
            circlePoints[i] = new Vector3(
                center.x + radius * Mathf.Cos(currentAngle_rad),
                center.y,  // Keep Y the same
                center.z + radius * Mathf.Sin(currentAngle_rad)
            );

            // Log each point for debugging
            // Debug.Log($"Circle point [{i}] at ({circlePoints[i].x}, {circlePoints[i].y}, {circlePoints[i].z})");
        }

        return circlePoints;
    }

    public GameObject DrawCircleOrArc(Vector3 center, float radius, int segmentCount, float thickness, float startAngle = 0, float endAngle = 360, string circleName = "Circle")
    {
        // Create a parent GameObject for the circle
        GameObject circleParent = new GameObject(circleName);  // The parent object for the entire circle

        Vector3[] circlePoints = GetCirclePoints(center, radius, segmentCount, startAngle, endAngle);
        // Draw quads between consecutive points
        for (int i = 0; i < circlePoints.Length - 1; i++)
        {
            DrawLineAsQuad(circlePoints[i], circlePoints[i + 1], thickness, $"Segment {i}", circleParent.transform);
        }

        // Optionally connect the last point to the first if drawing a full circle
        if (endAngle - startAngle == 360)
        {
            DrawLineAsQuad(circlePoints[circlePoints.Length - 1], circlePoints[0], thickness, "Last Segment", circleParent.transform);
        }
        // // Optionally set the entire circle parent object as a child of another object, like PitchLines
        circleParent.transform.parent = GameObject.Find("PitchLines").transform;
        return circleParent;
    }

    public GameObject DrawDot(Vector3 position, float radius, Sprite circleSprite, string lineName = "Unnamed Dot")
    {
        // Create a new GameObject to hold the SpriteRenderer
        GameObject dot = new GameObject("Dot");
        dot.name = lineName;  // Assign the name to the GameObject
        dot.layer = ignoreRaycastLayer; // Ignore from RayCasts
        dot.transform.parent = transform;  // Set the parent to PitchLines or any other game object

        // Add a SpriteRenderer component to the GameObject
        SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();

        if (circleSprite != null)
        {
            sr.sprite = circleSprite;  // Set the sprite to the circle sprite
        }
        else
        {
            Debug.LogError("Circle sprite not assigned.");
        }

        // Set the position of the sprite (slightly above the ground)
        dot.transform.position = new Vector3(position.x, 0.03f, position.z);

        // Scale the sprite to match the desired radius
        dot.transform.localScale = new Vector3(radius, radius, 1);  // X and Y define the size of the dot

        // Rotate the sprite to lie flat on the XZ plane
        dot.transform.rotation = Quaternion.Euler(90, 0, 0);  // Rotate the sprite to lie flat

        // Debug.Log($"Sprite dot drawn at ({position.x}, {position.y}, {position.z}) with radius {radius}");
        return dot;
    }
}
