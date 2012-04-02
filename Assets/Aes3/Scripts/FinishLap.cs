using UnityEngine;
using System.Collections;

public class FinishLap : MonoBehaviour
{
	
	public bool lapping;
	private CarController car;
	private Transform carCoG;
	private int curLap, wrongLaps;
	private bool enter, exit, wrong;
	
	void Start()
	{
		car = (CarController)FindObjectOfType(typeof(CarController));
		carCoG = car.centerOfMass;
		wrongLaps = 0;
	}
	
	void OnTriggerEnter()
	{
		lapping = true;
		if(carCoG.position.x < 845)
			enter = true;
		else
			wrong = true;
	}
	
	void OnTriggerExit()
	{
		lapping = false;
		if(carCoG.position.x > 862)
		{
			exit = true;
			wrong = false;
		}
		else
			wrongLaps++;
		
		if(curLap < 10 && enter && exit)
		{
			if(wrongLaps == 0)
				car.setCurLap(++curLap);
			else
				wrongLaps--;
		}
		enter = false;
		exit = false;
	}
}