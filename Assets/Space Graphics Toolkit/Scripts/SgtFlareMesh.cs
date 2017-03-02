using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtFlareMesh))]
public class SgtFlareMesh_Editor : SgtEditor<SgtFlareMesh>
{
	protected override void OnInspector()
	{
		var updateMesh  = false;
		var updateApply = false;
		
		BeginError(Any(t => t.Flare == null));
			DrawDefault("Flare", ref updateApply);
		EndError();
		BeginError(Any(t => t.Detail <= 2));
			DrawDefault("Detail", ref updateMesh);
		EndError();
		BeginError(Any(t => t.Radius <= 0.0f));
			DrawDefault("Radius", ref updateMesh);
		EndError();

		Separator();
		
		DrawDefault("Wave", ref updateMesh);

		if (Any(t => t.Wave == true))
		{
			BeginIndent();
				DrawDefault("WaveStrength", ref updateMesh);
				BeginError(Any(t => t.WavePoints < 0));
					DrawDefault("WavePoints", ref updateMesh);
				EndError();
				BeginError(Any(t => t.WavePower < 1.0f));
					DrawDefault("WavePower", ref updateMesh);
				EndError();
				DrawDefault("WavePhase", ref updateMesh);
			EndIndent();
		}

		Separator();
		
		DrawDefault("Noise", ref updateMesh);

		if (Any(t => t.Noise == true))
		{
			BeginIndent();
				BeginError(Any(t => t.NoiseStrength < 0.0f));
					DrawDefault("NoiseStrength", ref updateMesh);
				EndError();
				BeginError(Any(t => t.NoisePoints <= 0));
					DrawDefault("NoisePoints", ref updateMesh);
				EndError();
				DrawDefault("NoisePhase", ref updateMesh);
				DrawDefault("NoiseSeed", ref updateMesh);
			EndIndent();
		}

		if (updateMesh  == true) DirtyEach(t => t.UpdateMesh ());
		if (updateApply == true) DirtyEach(t => t.UpdateApply());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Flare Mesh")]
public class SgtFlareMesh : MonoBehaviour
{
	[Tooltip("The flare this mesh will be applied to")]
	public SgtFlare Flare;
	
	[Tooltip("The amount of points used to make the flare mesh")]
	public int Detail = 512;
	
	[Tooltip("The base radius of the flare in local space")]
	public float Radius = 2.0f;

	[Tooltip("Deform the flare based on cosine wave?")]
	public bool Wave;

	[Tooltip("The strength of the wave in local space")]
	public float WaveStrength = 5.0f;

	[Tooltip("The amount of wave peaks")]
	public int WavePoints = 4;

	[Tooltip("The sharpness of the waves")]
	public float WavePower = 5.0f;

	[Tooltip("The angle offset of the waves")]
	public float WavePhase;

	[Tooltip("Deform the flare based on noise?")]
	public bool Noise;

	[Tooltip("The strength of the noise in local space")]
	public float NoiseStrength = 5.0f;

	[Tooltip("The amount of noise points")]
	public int NoisePoints = 50;

	[Tooltip("The sharpness of the noise")]
	public float NoisePower = 5.0f;
	
	[Tooltip("The angle offset of the noise")]
	public float NoisePhase;

	[Tooltip("The random seed used for the random noise")]
	[SgtSeed]
	public int NoiseSeed;
	
	[System.NonSerialized]
	private Mesh generatedMesh;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;

	private static List<float> points = new List<float>();
	
	public Mesh GeneratedMesh
	{
		get
		{
			return generatedMesh;
		}
	}
	
#if UNITY_EDITOR
	[ContextMenu("Export Mesh")]
	public void ExportMesh()
	{
		SgtHelper.ExportAssetDialog(generatedMesh, "Flare Mesh");
	}
#endif

	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		if (Detail > 2)
		{
			if (generatedMesh == null)
			{
				generatedMesh = SgtHelper.CreateTempMesh("Flare Mesh (Generated)");

				UpdateApply();
			}
			
			var total     = Detail + 1;
			var positions = new Vector3[total];
			var coords1   = new Vector2[total];
			var indices   = new int[Detail * 3];
			var angleStep = (Mathf.PI * 2.0f) / Detail;
			var noiseStep = 0.0f;

			if (Noise == true && NoisePoints > 0)
			{
				SgtHelper.BeginRandomSeed(NoiseSeed);
				{
					points.Clear();

					for (var i = 0; i < NoisePoints; i++)
					{
						points.Add(Random.value);
					}

					noiseStep = NoisePoints / (float)Detail;
				}
				SgtHelper.EndRandomSeed();
			}

			for (var point = 0; point < Detail; point++)
			{
				var v     = point + 1;
				var angle = angleStep * point;
				var x     = Mathf.Sin(angle);
				var y     = Mathf.Cos(angle);
				var r     = Radius;
				
				if (Wave == true)
				{
					var waveAngle = (angle + WavePhase * Mathf.Deg2Rad) * WavePoints;

					r += Mathf.Pow(Mathf.Cos(waveAngle) * 0.5f + 0.5f, WavePower * WavePower) * WaveStrength;
				}

				if (Noise == true && NoisePoints > 0)
				{
					var noise  = Mathf.Repeat(noiseStep * point + NoisePhase, NoisePoints);
					var index  = (int)noise;
					var frac   = noise % 1.0f;
					var pointA = points[index];
					var pointB = points[(index + 1) % NoisePoints];
					var pointC = points[(index + 2) % NoisePoints];
					var pointD = points[(index + 3) % NoisePoints];

					r += SgtHelper.CubicInterpolate(pointA, pointB, pointC, pointD, frac) * NoiseStrength;
				}

				positions[v] = new Vector3(x * r, y * r, 0.0f);
				coords1[v] = new Vector2(1.0f, 0.0f);
			}
			
			for (var tri = 0; tri < Detail; tri++)
			{
				var i  = tri * 3;
				var v0 = tri + 1;
				var v1 = tri + 2;

				if (v1 >= total)
				{
					v1 = 1;
				}

				indices[i + 0] = 0;
				indices[i + 1] = v0;
				indices[i + 2] = v1;
			}
			
			generatedMesh.Clear();
			generatedMesh.vertices  = positions;
			generatedMesh.uv        = coords1;
			generatedMesh.triangles = indices;
			generatedMesh.RecalculateNormals();
			generatedMesh.RecalculateBounds();
		}
	}
	
	[ContextMenu("Update Apply")]
	public void UpdateApply()
	{
		if (Flare != null)
		{
			Flare.Mesh = generatedMesh;

			Flare.UpdateMesh();
		}
	}

	protected virtual void OnEnable()
	{
		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected virtual void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;

			if (Flare == null)
			{
				Flare = GetComponent<SgtFlare>();
			}

			CheckUpdateCalls();
		}
	}

	protected virtual void OnDestroy()
	{
		if (generatedMesh != null)
		{
			generatedMesh.Clear();

			SgtObjectPool<Mesh>.Add(generatedMesh);
		}
	}
	
	private void CheckUpdateCalls()
	{
		if (generatedMesh == null)
		{
			UpdateMesh();
		}

		UpdateApply();
	}
}