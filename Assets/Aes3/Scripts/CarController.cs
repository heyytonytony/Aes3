using UnityEngine;
using System.Collections;

// This class is responsible for controlling inputs to the car.
// Change this code to implement other input types, such as support for analogue input, or AI cars.
[RequireComponent (typeof (Drivetrain))]
public class CarController : MonoBehaviour {

	//state
	public int state = 0;
	private bool[] flags = new bool[10];
	public Texture2D flag;
	
	// Add all wheels of the car here, so brake and steering forces can be applied to them.
	public Wheel[] wheels;
	
	// A transform object which marks the car's center of gravity.
	// Cars with a higher CoG tend to tilt more in corners.
	// The further the CoG is towards the rear of the car, the more the car tends to oversteer. 
	// If this is not set, the center of mass is calculated from the colliders.
	public Transform centerOfMass;

	// A factor applied to the car's inertia tensor. 
	// Unity calculates the inertia tensor based on the car's collider shape.
	// This factor lets you scale the tensor, in order to make the car more or less dynamic.
	// A higher inertia makes the car change direction slower, which can make it easier to respond to.
	public float inertiaFactor = 1.5f;
	
	// current input state
	float brake;
	float throttle;
	float throttleInput;
	float steering;
	float lastShiftTime = -1;
	float handbrake;
		
	// cached Drivetrain reference
	Drivetrain drivetrain;
	
	// How long the car takes to shift gears
	public float shiftSpeed = 0.8f;
	

	// These values determine how fast throttle value is changed when the accelerate keys are pressed or released.
	// Getting these right is important to make the car controllable, as keyboard input does not allow analogue input.
	// There are different values for when the wheels have full traction and when there are spinning, to implement 
	// traction control schemes.
		
	// How long it takes to fully engage the throttle
	public float throttleTime = 1.0f;
	// How long it takes to fully engage the throttle 
	// when the wheels are spinning (and traction control is disabled)
	public float throttleTimeTraction = 10.0f;
	// How long it takes to fully release the throttle
	public float throttleReleaseTime = 0.5f;
	// How long it takes to fully release the throttle 
	// when the wheels are spinning.
	public float throttleReleaseTimeTraction = 0.1f;

	// Turn traction control on or off
	public bool tractionControl = true;
	
	
	// These values determine how fast steering value is changed when the steering keys are pressed or released.
	// Getting these right is important to make the car controllable, as keyboard input does not allow analogue input.
	
	// How long it takes to fully turn the steering wheel from center to full lock
	public float steerTime = 1.2f;
	// This is added to steerTime per m/s of velocity, so steering is slower when the car is moving faster.
	public float veloSteerTime = 0.1f;

	// How long it takes to fully turn the steering wheel from full lock to center
	public float steerReleaseTime = 0.6f;
	// This is added to steerReleaseTime per m/s of velocity, so steering is slower when the car is moving faster.
	public float veloSteerReleaseTime = 0f;
	// When detecting a situation where the player tries to counter steer to correct an oversteer situation,
	// steering speed will be multiplied by the difference between optimal and current steering times this 
	// factor, to make the correction easier.
	public float steerCorrectionFactor = 4.0f;
	
	//lap times
	float[] lapTimes = new float[10];
	int currentLap = 0;
	

	// Used by SoundController to get average slip velo of all wheels for skid sounds.
	public float slipVelo
	{
		get {
			float val = 0.0f;
			foreach(Wheel w in wheels)
				val += w.slipVelo / wheels.Length;
			return val;
		}
	}

	// Initialize
	void Start () 
	{
		if (centerOfMass != null)
			rigidbody.centerOfMass = centerOfMass.localPosition;
		rigidbody.inertiaTensor *= inertiaFactor;
		drivetrain = GetComponent (typeof (Drivetrain)) as Drivetrain;
	}
	
	void Update () 
	{
		if(state == 0)
		{
			// Steering
			Vector3 carDir = transform.forward;
			float fVelo = rigidbody.velocity.magnitude;
			Vector3 veloDir = rigidbody.velocity * (1/fVelo);
			float angle = -Mathf.Asin(Mathf.Clamp( Vector3.Cross(veloDir, carDir).y, -1, 1));
			float optimalSteering = angle / (wheels[0].maxSteeringAngle * Mathf.Deg2Rad);
			if (fVelo < 1)
				optimalSteering = 0;
					
			float steerInput = 0;
			if (Input.GetKey (KeyCode.LeftArrow))
				steerInput = -1;
			if (Input.GetKey (KeyCode.RightArrow))
				steerInput = 1;
	
			if (steerInput < steering)
			{
				float steerSpeed = (steering>0)?(1/(steerReleaseTime+veloSteerReleaseTime*fVelo)) :(1/(steerTime+veloSteerTime*fVelo));
				if (steering > optimalSteering)
					steerSpeed *= 1 + (steering-optimalSteering) * steerCorrectionFactor;
				steering -= steerSpeed * Time.deltaTime;
				if (steerInput > steering)
					steering = steerInput;
			}
			else if (steerInput > steering)
			{
				float steerSpeed = (steering<0)?(1/(steerReleaseTime+veloSteerReleaseTime*fVelo)) :(1/(steerTime+veloSteerTime*fVelo));
				if (steering < optimalSteering)
					steerSpeed *= 1 + (optimalSteering-steering) * steerCorrectionFactor;
				steering += steerSpeed * Time.deltaTime;
				if (steerInput < steering)
					steering = steerInput;
			}
			
			// Throttle/Brake
	
			bool accelKey = Input.GetKey (KeyCode.UpArrow);
			bool brakeKey = Input.GetKey (KeyCode.DownArrow);
			
			if (drivetrain.automatic && drivetrain.gear == 0)
			{
				accelKey = Input.GetKey (KeyCode.DownArrow);
				brakeKey = Input.GetKey (KeyCode.UpArrow);
			}
			
			if (Input.GetKey (KeyCode.LeftShift))
			{
				throttle += Time.deltaTime / throttleTime;
				throttleInput += Time.deltaTime / throttleTime;
			}
			else if (accelKey)
			{
				if (drivetrain.slipRatio < 0.10f)
					throttle += Time.deltaTime / throttleTime;
				else if (!tractionControl)
					throttle += Time.deltaTime / throttleTimeTraction;
				else
					throttle -= Time.deltaTime / throttleReleaseTime;
	
				if (throttleInput < 0)
					throttleInput = 0;
				throttleInput += Time.deltaTime / throttleTime;
				brake = 0;
			}
			else 
			{
				if (drivetrain.slipRatio < 0.2f)
					throttle -= Time.deltaTime / throttleReleaseTime;
				else
					throttle -= Time.deltaTime / throttleReleaseTimeTraction;
			}
			throttle = Mathf.Clamp01 (throttle);
	
			if (brakeKey)
			{
				if (drivetrain.slipRatio < 0.2f)
					brake += Time.deltaTime / throttleTime;
				else
					brake += Time.deltaTime / throttleTimeTraction;
				throttle = 0;
				throttleInput -= Time.deltaTime / throttleTime;
			}
			else 
			{
				if (drivetrain.slipRatio < 0.2f)
					brake -= Time.deltaTime / throttleReleaseTime;
				else
					brake -= Time.deltaTime / throttleReleaseTimeTraction;
			}
			brake = Mathf.Clamp01 (brake);
			throttleInput = Mathf.Clamp (throttleInput, -1, 1);
					
			// Handbrake
			handbrake = Mathf.Clamp01 ( handbrake + (Input.GetKey (KeyCode.Space)? Time.deltaTime: -Time.deltaTime) );
			
			// Gear shifting
			float shiftThrottleFactor = Mathf.Clamp01((Time.time - lastShiftTime)/shiftSpeed);
			drivetrain.throttle = throttle * shiftThrottleFactor;
			drivetrain.throttleInput = throttleInput;
			
			if(Input.GetKeyDown(KeyCode.A))
			{
				lastShiftTime = Time.time;
				drivetrain.ShiftUp ();
			}
			if(Input.GetKeyDown(KeyCode.Z))
			{
				lastShiftTime = Time.time;
				drivetrain.ShiftDown ();
			}
	
			// Apply inputs
			foreach(Wheel w in wheels)
			{
				w.brake = brake;
				w.handbrake = handbrake;
				w.steering = steering;
			}
			
			//laptime updating
			lapTimes[currentLap] += Time.deltaTime;
		}
		
		
		//State changes
		if( centerOfMass.position.x > 1130 && centerOfMass.position.x < 1170 
			&& centerOfMass.position.z > 525 && centerOfMass.position.z < 570)
		{
			if(flags[1] == false){
				state = 1;
				Time.timeScale = 0;
				rigidbody.SetMaxAngularVelocity(0);
			}
			else if(flags[2] == false){
				state = 2;
			}
			else if(flags[3] == false){
				state = 3;
			}
			else if(flags[4] == false){
				state = 4;
			}
			else if(flags[5] == false){
				state = 5;
			}
			else {
				state = 0;
				Time.timeScale = 1;
				rigidbody.SetMaxAngularVelocity(1);
			}
		}
		else if(currentLap == 10)
		{
			Time.timeScale = 0;
		}

	}
	
	public void setCurLap(int curLap)
	{
		currentLap = curLap;
	}
	
	void OnGUI()
	{
		if(state == 1){
			// Make a box
			GUI.DrawTexture(new Rect(Screen.width/2-200, Screen.height/2-300, 400, 400), flag);
			GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-150,300,300), "\n\nCheckpoint 1\n\n This is your first checkpoint");
			if(GUI.Button(new Rect(Screen.width/2-50,Screen.height/2+50,100,20), "Continue")) {
				flags[1] = true;
			}
		}
		if(state == 2){
			// Make a box
			GUI.DrawTexture(new Rect(Screen.width/2-200, Screen.height/2-300, 400, 400), flag);			
			GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-150,300,300), "\n\nCheckpoint 1\n\n The point of these checkpoints\n is to allow you to change the\n variables of the physics\n equation F=MA");
			if(GUI.Button(new Rect(Screen.width/2-50,Screen.height/2+50,100,20), "Continue")) {
				flags[2] = true;
			}
		}
		if(state == 3){
			// Make a box
			GUI.DrawTexture(new Rect(Screen.width/2-200, Screen.height/2-300, 400, 400), flag);			
			GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-150,300,300), "\n\nCheckpoint 1\n\n The point of these changing\n the equations is so that\n you may better learn how they work.\n Selecting the proper answer\n makes your car go faster.");
			if(GUI.Button(new Rect(Screen.width/2-50,Screen.height/2+50,100,20), "Continue")) {
				flags[3] = true;
			}
		}
		if(state == 4){
			// Make a box
			GUI.DrawTexture(new Rect(Screen.width/2-200, Screen.height/2-300, 400, 400), flag);			
			GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-150,300,300), "\n\nCheckpoint 1\n\n For the first checkpoint \nwe will we will give you a choice\n of a flat bumper\n or a slick one.");
			if(GUI.Button(new Rect(Screen.width/2-50,Screen.height/2+50,100,20), "Continue")) {
				flags[4] = true;
			}
		}
		if(state == 5){
			// Make a box
			GUI.DrawTexture(new Rect(Screen.width/2-200, Screen.height/2-300, 400, 400), flag);			
			GUI.Box(new Rect(Screen.width/2-150,Screen.height/2-150,300,300), "\n\nCheckpoint 1\n\n F=M*A \nor Acceleration = force / mass\n\n your mass will stay constant for now\n your choice is between negative \nwind resistance forces");
			if(GUI.Button(new Rect(Screen.width/2-125,Screen.height/2+40,250,20), "Flat bumper  (F = -50 Newtons)")) {
				flags[5] = true;
			}
			if(GUI.Button(new Rect(Screen.width/2-125,Screen.height/2+70,250,20), "Slick bumper  (F = -20 Newtons)")) {
				flags[5] = true;
			}
		}
		GUI.Label(new Rect(Screen.width-250,60,200,50),"Center:" + centerOfMass.position +"\n" + "State: " + state, "box");
		//GUI.Label(new Rect(Screen.width-100,60,100,200),"km/h: " + rigidbody.velocity.magnitude * 3.6f, "box");
		//tractionControl = GUI.Toggle(new Rect(0,80,300,20), tractionControl, "Traction Control (bypassed by shift key)");
		
		GUI.Label(new Rect(Screen.width - 150, Screen.height - 80, 80, 20), Mathf.Round(rigidbody.velocity.magnitude * 3.6f) + " km/h");
		//tractionControl = GUI.Toggle(new Rect(0,80,300,20), tractionControl, "Traction Control (bypassed by shift key)");
		
		//elapsed time counter thing
		GUI.BeginGroup(new Rect(10, 60, 200, 360));
		GUI.Box(new Rect(0, 0, 200, 36*(currentLap+1)), "");
		for(int x = 0; x < 10; x++)
		{
			if(lapTimes[x] > 0)
				GUI.Label(new Rect(10, 6+x*36, 190, 36), "Lap " + (x+1) + ":\t\t" + lapTimes[x].ToString ("0.00"));
		}
		GUI.EndGroup();
	}
}
