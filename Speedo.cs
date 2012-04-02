using UnityEngine;
using System.Collections;

 

public class Speedo : MonoBehaviour
{
	
	public Texture2D dialTex;
	public Texture2D needleTex;
	public Vector2 dialPos;
	public float topSpeed;
	public float stopAngle;
	public float topSpeedAngle;
	public float speed;
	
	// Cached rigidbody
	Rigidbody body;
	
	void Start()
	{
		Transform trs = transform;
		while (trs != null && trs.rigidbody == null)
			trs = trs.parent;
		if (trs != null)
			body = trs.rigidbody;
		
		dialPos.x = Screen.width - 260;
		dialPos.y = Screen.height - 230;
	}
	
	void Update()
	{
		speed = body.velocity.magnitude * 3.6f;
	}
	
	void  OnGUI()
	{
	    GUI.DrawTexture(new Rect(dialPos.x, dialPos.y, dialTex.width, dialTex.height), dialTex);
	    Vector2 centre = new Vector2(dialPos.x + dialTex.width / 2, dialPos.y + dialTex.height / 2);
	    Matrix4x4 savedMatrix = GUI.matrix;
	    float speedFraction = speed / topSpeed;
	    float needleAngle = Mathf.Lerp(stopAngle, topSpeedAngle, speedFraction);
	    GUIUtility.RotateAroundPivot(needleAngle, centre);
	    GUI.DrawTexture(new Rect(centre.x, centre.y - needleTex.height / 2, needleTex.width, needleTex.height), needleTex);
	    GUI.matrix = savedMatrix;
	}
}