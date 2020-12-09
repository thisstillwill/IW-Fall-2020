using UnityEngine;

// A GridPoint represents a single vertex in a GridCell with a position, value, and size
public class GridPoint : MonoBehaviour
{
    // Handle build requests when a GridPoint's value changes
    public delegate void PointValueChange(ref GridPoint gp);
    public static event PointValueChange OnPointValueChange;

    // Public parameters
    static public bool draw = true;     // Draw or erase

    // Private parameters
    private Vector3 position = Vector3.zero;
    private float value = 0f;
    private float size = 0.1f;
    private float dPaint = 2f;        // Value added/removed

    // Set the GridPoint's position relative to its parent (the grid)
    public Vector3 Position
    {
        get
        {
            return position;
        }
        set
        {
            this.position = new Vector3(value.x, value.y, value.z);
            if (this != null)
                this.transform.localPosition = position;
        }
    }
    
    // Set the GridPoint's value (a sampling of the isofield at the GridPoint's position)
    public float Value
    {
        get
        {
            return value;
        }
        set
        {
            this.value = value;
        }
    }
    
    // Set the size of the GridPoint relative to its parent (the grid)
    public float Size
    {
        get
        {
            return size;
        }
        set
        {
            size = value;
            if (this != null)
                this.transform.localScale = new Vector3(size, size, size);
        }
    }

    // Add or remove to the GridPoint's value while brush is activated
    private void OnTriggerStay(Collider other)
    {
        // Clamp the amount drawn/erased between 0 and 1
        if (draw)
        {
            this.Value = Mathf.Clamp(this.Value - dPaint * Time.deltaTime, 0f, 1f);
        }
        else {
            this.Value = Mathf.Clamp(this.Value + dPaint * Time.deltaTime, 0f, 1f);
        }
        // Make a build request
        if (OnPointValueChange != null)
        {
            GridPoint gp = this.GetComponent<GridPoint>();
            OnPointValueChange(ref gp);
        }
    }
}
