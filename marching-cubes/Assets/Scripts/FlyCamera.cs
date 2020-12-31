using UnityEngine;

// Script by IJM: http://answers.unity3d.com/questions/29741/mouse-look-script.html
// Changed to fit standard C# conventions
// Extended by William Svoboda 12-06-20: https://github.com/disstillwill/IW-Fall-2020/blob/master/marching-cubes/Assets/Scripts/FlyCamera.cs
// - Added movement code
// - Added brush mechanics for Marching Cubes demo
// Basic flying camera controller that mimics Minecraft's flying controls.
// Drag and drop on camera to use.
// WASD: Basic movement
// Space/Shift: Move camera up/down
// Mouse 1: Enable brush
// Mouse 2: Disable brush 

public class FlyCamera : MonoBehaviour
{
	// Public parameters
	public float sensitivityX = 15f;	// Mouse sensitivity
	public float sensitivityY = 15f;
	public float minimumX = -360f;		// Angle ranges
	public float maximumX = 360f;
	public float minimumY = -89f;
	public float maximumY = 89f;
	public float speed = 10f;			// Movement speed
	public GameObject brush = null;		// Brush object

	// Private parameters
	private float rotationX = 0f;
	private float rotationY = 0f;
	private Quaternion originalRotation;

	// Start is called before the first frame update
	void Start()
	{
		originalRotation = transform.localRotation;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// Update is called once per frame
	void Update()
	{
		// Unlock cursor if escape key pressed
		if (Input.GetKeyDown(KeyCode.Escape))
        {
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
		if (Input.GetMouseButtonDown(0))
        {
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		// Read mouse input
		rotationX += Input.GetAxis("Mouse X") * sensitivityX;
		rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

		// Clamp angle and calculate rotation
		rotationX = ClampAngle(rotationX, minimumX, maximumX);
		rotationY = ClampAngle(rotationY, minimumY, maximumY);
		Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
		Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

		// Set rotation
		transform.localRotation = originalRotation * xQuaternion * yQuaternion;

		// Read keyboard input and calculate movement
		Vector3 move = new Vector3();
		move = transform.TransformDirection(move);
		if (Input.GetKey(KeyCode.W))				// Forwards
		{
			move += transform.forward;
		}
		if (Input.GetKey(KeyCode.S))				// Backwards
		{
			move -= transform.forward;
		}
		if (Input.GetKey(KeyCode.D))				// Right
		{
			move += transform.right;
		}
		if (Input.GetKey(KeyCode.A))				// Left
		{
			move -= transform.right;
		}
		// Ignore camera angle in movement
		move.y = 0;
		if (Input.GetKey(KeyCode.Space))			// Up
		{
			move += Vector3.up;
		}
		if (Input.GetKey(KeyCode.LeftShift) 
			|| Input.GetKey(KeyCode.RightShift))	// Down
		{
			move -= Vector3.up;
		}
		move.Normalize();
		transform.Translate(move * speed * Time.deltaTime, Space.World);

        // Read brush input
        if (Input.GetMouseButtonDown(0))			// Draw
		{
			brush.GetComponent<Collider>().enabled = true;
			GridPoint.draw = true;
			Debug.Log("Pressed left click");
		}
		if (Input.GetMouseButtonDown(1))			// Erase
		{
			brush.GetComponent<Collider>().enabled = true;
			GridPoint.draw = false;
			Debug.Log("Pressed right click");
		}
		if (Input.GetMouseButtonUp(0) 
			|| Input.GetMouseButtonUp(1))			// Stop
		{
			brush.GetComponent<Collider>().enabled = false;
			Debug.Log("Released left/right click");
		}

	}

	// Clamp an angle between the provided minimum and maximum
	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360f)
			angle += 360f;
		if (angle > 360f)
			angle -= 360f;
		return Mathf.Clamp(angle, min, max);
	}
}
