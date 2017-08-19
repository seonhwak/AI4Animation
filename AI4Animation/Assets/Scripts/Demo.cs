﻿using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class Demo : MonoBehaviour {

	public Transform Root;

	public float Scale = 1f;
	public float Speed = 0f;
	public float Phase = 0f;

	private PFNN Network;
	private Character Character;
	private Trajectory Trajectory;

	public float XAxis = 0f;
	public float YAxis = 0f;
	public float Turn = 0f;

	private const float M_PI = 3.14159265358979323846f;

	void Start() {
		//Network = new PFNN(PFNN.MODE.CONSTANT);
		Character = new Character(transform, Root);
		Trajectory = new Trajectory(transform);

		//Predict();
	}

	void Update() {
		Scale = Mathf.Max(1e-5f, Scale);

		PreUpdate();
		//Predict();
		//RegularUpdate();
		PostUpdate();
		
		//Vector3 angles = Quaternion.LookRotation(Trajectory.Velocities[Trajectory.Length/2], Vector3.up).eulerAngles;
		//angles.x = 0f;
		//transform.rotation = Quaternion.Euler(0f,90f,0f) * Quaternion.Euler(angles);
	}

	private void Predict() {
		/* Input Trajectory Positions / Directions */
		for(int i=0; i<Trajectory.Length; i+=10) {
			int w = (Trajectory.Length)/10;
			Vector3 pos = Quaternion.Inverse(Character.Transform.rotation) * (Trajectory.Positions[i] - Character.Transform.position);
			Vector3 dir = Quaternion.Inverse(Character.Transform.rotation) * Trajectory.Velocities[i].normalized;  
			Network.Xp[(w*0)+i/10, 0] = 0;
			Network.Xp[(w*1)+i/10, 0] = 0;
			Network.Xp[(w*2)+i/10, 0] = 0;
			Network.Xp[(w*3)+i/10, 0] = 0;
		}

		/* Input Trajectory Gaits */
		for (int i=0; i<Trajectory.Length; i+=10) {
			int w = (Trajectory.Length)/10;
			Network.Xp[(w*4)+i/10, 0] = 0;
			Network.Xp[(w*5)+i/10, 0] = 0;
			Network.Xp[(w*6)+i/10, 0] = 0;
			Network.Xp[(w*7)+i/10, 0] = 0;
			Network.Xp[(w*8)+i/10, 0] = 0;
			Network.Xp[(w*9)+i/10, 0] = 0;
		}

		//TODO: Maybe take previous state? But why?
		for(int i=0; i<Character.Joints.Length; i++) {
			int o = Trajectory.Length;
			Vector3 pos; Quaternion rot;
			Vector3 vel = Quaternion.Inverse(Character.Transform.rotation) * transform.forward;
			Character.Joints[i].GetConfiguration(out pos, out rot);
			pos = 1f/Scale * pos;
			//glm::vec3 prv = glm::inverse(prev_root_rotation) *  character->joint_velocities[i];
			Network.Xp[o+(Character.Joints.Length*3*0)+i*3+0, 0] = pos.x;
			Network.Xp[o+(Character.Joints.Length*3*0)+i*3+1, 0] = pos.y;
			Network.Xp[o+(Character.Joints.Length*3*0)+i*3+2, 0] = pos.z;
			Network.Xp[o+(Character.Joints.Length*3*1)+i*3+0, 0] = 0;
			Network.Xp[o+(Character.Joints.Length*3*1)+i*3+1, 0] = 0;
			Network.Xp[o+(Character.Joints.Length*3*1)+i*3+2, 0] = 0;
		}

		/* Input Trajectory Heights */
		for (int i=0; i<Trajectory.Length; i+=10) {
			int o = Trajectory.Length + Character.Joints.Length*3*2;
			int w = Trajectory.Length/10;
			/*
			glm::vec3 position_r = trajectory->positions[i] + (trajectory->rotations[i] * glm::vec3( trajectory->width, 0, 0));
			glm::vec3 position_l = trajectory->positions[i] + (trajectory->rotations[i] * glm::vec3(-trajectory->width, 0, 0));
			pfnn->Xp(o+(w*0)+(i/10)) = heightmap->sample(glm::vec2(position_r.x, position_r.z)) - root_position.y;
			pfnn->Xp(o+(w*1)+(i/10)) = trajectory->positions[i].y - root_position.y;
			pfnn->Xp(o+(w*2)+(i/10)) = heightmap->sample(glm::vec2(position_l.x, position_l.z)) - root_position.y;
			*/
		}

		Phase += Speed * Time.deltaTime;
		Matrix<float> result = Network.Predict(Mathf.Repeat(Phase, 2f*M_PI));

		/*
		string output = string.Empty;
		for(int i=0; i<Network.YDim; i++) {
			output += result[i, 0] +  " ";
		}
		Debug.Log(output);
		*/

		for(int i=0; i<Character.Joints.Length; i++) {
			int opos = 8+(((Trajectory.Length/2)/10)*4)+(Character.Joints.Length*3*0);
			int orot = 8+(((Trajectory.Length/2)/10)*4)+(Character.Joints.Length*3*2);
			
			Vector3 position = Scale * new Vector3(result[opos+i*3+0, 0], result[opos+i*3+1, 0], result[opos+i*3+2, 0]);
			//Quaternion rotation = Quaternion.Euler(new Vector3(result[orot+i*3+2, 0], result[orot+i*3+0, 0], result[orot+i*3+1, 0]));

			Vector3 pos;
			Quaternion rot;
			Character.Joints[i].GetConfiguration(out pos, out rot);
			//Quaternion rotation = quat_exp(new Vector3(result[orot+i*3+0, 0], result[orot+i*3+1, 0], result[orot+i*3+2, 0]));

			Character.Joints[i].SetConfiguration(position, rot);

			//Debug.Log(position);
			//Debug.Log(rotation);
		}
	}

	private Quaternion quat_exp(Vector3 l) {
		float w = l.magnitude;
		Quaternion q = w < 0.01f ? new Quaternion(0f, 0f, 0f, 1f) : new Quaternion(
			l.x * (Mathf.Sin(w) / w),
			l.y * (Mathf.Sin(w) / w),
			l.z * (Mathf.Sin(w) / w),
			Mathf.Cos(w)
			);
		float div = Mathf.Sqrt(q.w*q.w + q.x*q.x + q.y*q.y + q.z*q.z);
		return new Quaternion(q.x/div, q.y/div, q.z/div, q.w/div);
	}

	private void PreUpdate() {
		HandleInput();

		//Update Trajectory Targets
		float acceleration = 30f;
		float damping = 10f;
		float decay = 2.5f;

		int current = Trajectory.Length/2;
		int last = Trajectory.Length-1;

		Trajectory.TargetDirection = /*transform.rotation **/ new Vector3(XAxis, 0f, YAxis).normalized;
		Trajectory.TargetDirection.y = 0f;
		Vector3 velocity = Utility.Interpolate(Trajectory.TargetVelocity, Vector3.zero, damping * Time.deltaTime);
		velocity = velocity + acceleration * Time.deltaTime * Trajectory.TargetDirection;
		//Trajectory.TargetVelocity = Utility.Interpolate(Trajectory.TargetVelocity, Vector3.zero, damping * Time.deltaTime);
		//Trajectory.TargetVelocity = Trajectory.TargetVelocity + acceleration * Time.deltaTime * Trajectory.TargetDirection;
		Vector3 target = Trajectory.TargetPosition + Time.deltaTime * Trajectory.TargetVelocity;
		//Trajectory.TargetPosition = Trajectory.TargetPosition + Time.deltaTime * Trajectory.TargetVelocity;
		if(!Physics.CheckSphere(target, 0.1f, LayerMask.GetMask("Obstacles"))) {
			Trajectory.TargetPosition = target;
			Trajectory.TargetVelocity = velocity;
		}

		if(Trajectory.TargetDirection.magnitude == 0f) {
			Trajectory.TargetPosition = Utility.Interpolate(Trajectory.TargetPosition, transform.position, decay * Time.deltaTime);
			Trajectory.TargetVelocity = Utility.Interpolate(Trajectory.TargetVelocity, Vector3.zero, decay * Time.deltaTime);
			for(int i=current+1; i<Trajectory.Length; i++) {
				Trajectory.Positions[i] = Utility.Interpolate(Trajectory.Positions[i], transform.position, decay * Time.deltaTime);
				Trajectory.Velocities[i] = Utility.Interpolate(Trajectory.Velocities[i], Vector3.zero, decay * Time.deltaTime);
			}
		}
		
		//Predict Trajectory
		//float rate = 10f * Time.deltaTime;
		float rate = 0.5f;

		Trajectory.Positions[last] = Trajectory.TargetPosition;
		Trajectory.Velocities[last] = Trajectory.TargetVelocity;

		float pastDamp = 1.5f;
		float futureDamp = 1.5f;
		for(int i=Trajectory.Length-2; i>=0; i--) {
			float factor = (float)(i+1)/(float)Trajectory.Length;
			factor = 2f * factor - 1f;
			factor = 1f - Mathf.Abs(factor);
			factor = Utility.Normalise(factor, 1f/(float)Trajectory.Length, ((float)Trajectory.Length-1f)/(float)Trajectory.Length, 1f - 60f / Trajectory.Length, 1f);

			if(i < current) {
				Trajectory.Positions[i] = 
					Trajectory.Positions[i] + Utility.Interpolate(
						Mathf.Pow(factor, pastDamp) * (Trajectory.Positions[i+1] - Trajectory.Positions[i]), 
						Trajectory.Positions[i+1] - Trajectory.Positions[i],
						rate
					);

				Trajectory.Velocities[i] = 
					Trajectory.Velocities[i] + Utility.Interpolate(
						Mathf.Pow(factor, pastDamp) * (Trajectory.Velocities[i+1] - Trajectory.Velocities[i]), 
						Trajectory.Velocities[i+1] - Trajectory.Velocities[i],
						rate
					);
			} else {
				Trajectory.Positions[i] = 
					Trajectory.Positions[i] + Utility.Interpolate(
						Mathf.Pow(factor, futureDamp) * (Trajectory.Positions[i+1] - Trajectory.Positions[i]), 
						Trajectory.Positions[i+1] - Trajectory.Positions[i],
						rate
					);

				Trajectory.Velocities[i] = 
					Trajectory.Velocities[i] + Utility.Interpolate(
						Mathf.Pow(factor, futureDamp) * (Trajectory.Velocities[i+1] - Trajectory.Velocities[i]), 
						Trajectory.Velocities[i+1] - Trajectory.Velocities[i],
						rate
					);
			}
		}
	}

	/*
	private void RegularUpdate() {
		//Character.Move(new Vector2(XAxis, YAxis));
		//Character.Turn(Turn);

		//int current = Trajectory.Length/2;
		//int last = Trajectory.Length-1;
		//transform.position = Trajectory.Positions[current];
		//transform.rotation = Quaternion.LookRotation(Trajectory.Directions[current], Vector3.up);
		
		//Network.Predict(0.5f);
	}
	*/

	private void PostUpdate() {
		//Adjust Trajectory
		int current = Trajectory.Length/2;
		int last = Trajectory.Length-1;

		Vector3 error = (transform.position - Trajectory.Positions[current]);
		for(int i=0; i<Trajectory.Length; i++) {
			float factor = (float)i / (float)(Trajectory.Length-1);
			Trajectory.Positions[i] += factor * error;
			Trajectory.Velocities[i] = Trajectory.Velocities[i].magnitude * (Trajectory.Velocities[i] + factor * error).normalized;
		}

		for(int i=0; i<Trajectory.Length; i++) {
			Trajectory.Positions[i].y = GetHeight(Trajectory.Positions[i].x, Trajectory.Positions[i].z);
			Vector3 start = Trajectory.Positions[i];
			Vector3 end = Trajectory.Positions[i] + 0.1f * Trajectory.Velocities[i].normalized;
			end.y = (GetHeight(end.x, end.z) - start.y) / 0.1f;
			Trajectory.Velocities[i] = Trajectory.Velocities[i].magnitude * new Vector3(Trajectory.Velocities[i].x, end.y, Trajectory.Velocities[i].z).normalized;
		}

		Trajectory.TargetPosition = Trajectory.Positions[last];

		//Character.Phase = GetPhase();
	}

	private float GetHeight(float x, float y) {
		RaycastHit hit;
		bool intersection = Physics.Raycast(new Vector3(x,-10f,y), Vector3.up, out hit, LayerMask.GetMask("Ground"));
		if(!intersection) {
			intersection = Physics.Raycast(new Vector3(x,10f,y), Vector3.down, out hit, LayerMask.GetMask("Ground"));
		}
		if(intersection) {
			return hit.point.y;
		} else {
			return 0f;
		}
	}

	private float GetPhase() {
		float stand_amount = 0f;
		float factor = 0.9f;
		return Mathf.Repeat(Character.Phase + stand_amount*factor + (1f-factor), 2f*M_PI);
	}

	private void HandleInput() {
		XAxis = 0f;
		YAxis = 0f;
		Turn = 0f;
		if(Input.GetKey(KeyCode.W)) {
			YAxis += 1f;
		}
		if(Input.GetKey(KeyCode.S)) {
			YAxis -= 1f;
		}
		if(Input.GetKey(KeyCode.A)) {
			XAxis -= 1f;
		}
		if(Input.GetKey(KeyCode.D)) {
			XAxis += 1f;
		}
		if(Input.GetKey(KeyCode.Q)) {
			Turn -= 1f;
		}
		if(Input.GetKey(KeyCode.E)) {
			Turn += 1f;
		}
	}

	void OnDrawGizmos() {
		if(!Application.isPlaying) {
			return;
		}
		Gizmos.color = Color.black;
		for(int i=0; i<Trajectory.Positions.Length-1; i++) {
			Gizmos.DrawLine(Trajectory.Positions[i], Trajectory.Positions[i+1]);
		}
		Gizmos.color = Color.blue;
		for(int i=0; i<Trajectory.Positions.Length; i++) {
			Vector3 ortho = Quaternion.Euler(0f, 90f, 0f) * Trajectory.Velocities[i];
			Vector3 left = Trajectory.Positions[i] - 0.15f * ortho.normalized;
			left.y = GetHeight(left.x, left.z);
			Vector3 right = Trajectory.Positions[i] + 0.15f * ortho.normalized;
			right.y = GetHeight(right.x, right.z);
			Gizmos.DrawLine(Trajectory.Positions[i], left);
			Gizmos.DrawLine(Trajectory.Positions[i], right);
			Gizmos.DrawSphere(left, 0.01f);
			Gizmos.DrawSphere(right, 0.01f);
		}
		Gizmos.color = Color.green;
		for(int i=0; i<Trajectory.Positions.Length; i++) {
			Gizmos.DrawLine(Trajectory.Positions[i], Trajectory.Positions[i] + Trajectory.Velocities[i]);
		}
		Gizmos.color = Color.red;
		for(int i=0; i<Trajectory.Positions.Length; i++) {
			Gizmos.DrawSphere(Trajectory.Positions[i], 0.015f);
		}

		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(Trajectory.TargetPosition, 0.03f);
		Gizmos.color = Color.red;
		Gizmos.DrawLine(Trajectory.TargetPosition, Trajectory.TargetPosition + Trajectory.TargetDirection);
		Gizmos.color = Color.green;
		Gizmos.DrawLine(Trajectory.TargetPosition, Trajectory.TargetPosition + Trajectory.TargetVelocity);
	}

}
