using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtProminence))]
public class SgtProminence_Editor : SgtEditor<SgtProminence>
{
	protected override void OnInspector()
	{
		var updateMaterial = false;
		var updateMesh     = false;
		var updatePlanes   = false;

		DrawDefault("Color", ref updateMaterial);
		DrawDefault("Brightness", ref updateMaterial);
		DrawDefault("RenderQueue", ref updateMaterial);
		DrawDefault("RenderQueueOffset", ref updateMaterial);
		
		Separator();
		
		BeginError(Any(t => t.MainTex == null));
			DrawDefault("MainTex", ref updateMaterial);
		EndError();
		DrawDefault("CameraOffset"); // Updated automatically
		DrawDefault("Seed", ref updatePlanes);
		BeginError(Any(t => t.PlaneCount < 1));
			DrawDefault("PlaneCount", ref updatePlanes);
		EndError();
		BeginError(Any(t => t.PlaneDetail < 3));
			DrawDefault("PlaneDetail", ref updateMesh);
		EndError();
		BeginError(Any(t => t.InnerRadius < 0.0f || t.InnerRadius >= t.OuterRadius));
			DrawDefault("InnerRadius", ref updateMesh);
		EndError();
		BeginError(Any(t => t.OuterRadius < 0.0f || t.InnerRadius >= t.OuterRadius));
			DrawDefault("OuterRadius", ref updateMesh);
		EndError();
		
		Separator();
		
		DrawDefault("FadeEdge", ref updateMaterial);
		
		if (Any(t => t.FadeEdge == true))
		{
			BeginIndent();
				BeginError(Any(t => t.FadePower < 0.0f));
					DrawDefault("FadePower", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		DrawDefault("ClipNear", ref updateMaterial);
		
		if (Any(t => t.ClipNear == true))
		{
			BeginIndent();
				BeginError(Any(t => t.ClipPower < 0.0f));
					DrawDefault("ClipPower", ref updateMaterial);
				EndError();
			EndIndent();
		}
		
		if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		if (updateMesh     == true) DirtyEach(t => t.UpdateMesh    ());
		if (updatePlanes   == true) DirtyEach(t => t.UpdatePlanes  ());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Prominence")]
public class SgtProminence : MonoBehaviour
{
	// All active and enabled prominences in the scene
	public static List<SgtProminence> AllProminences = new List<SgtProminence>();
	
	[Tooltip("The main texture of the prominence")]
	public Texture MainTex;

	[Tooltip("The color tint of the prominence")]
	public Color Color = Color.white;

	[Tooltip("The color brightness of the prominence")]
	public float Brightness = 1.0f;

	[Tooltip("The render queue group of the prominence")]
	public SgtRenderQueue RenderQueue = SgtRenderQueue.Transparent;

	[Tooltip("The render queue offset of the prominence")]
	public int RenderQueueOffset;
	
	[Tooltip("The random seed used when generating the prominence planes")]
	[SgtSeed]
	public int Seed;

	[Tooltip("The inner radius of the prominence planes in local coordinates")]
	public float InnerRadius = 1.0f;

	[Tooltip("The outer radius of the prominence planes in local coordinates")]
	public float OuterRadius = 2.0f;

	[Tooltip("The amount of planes used to build the prominence")]
	public int PlaneCount = 8;

	[Tooltip("The amount of quads used to build each plane")]
	[FormerlySerializedAs("Detail")]
	public int PlaneDetail = 10;

	[Tooltip("Should the plane fade out when it's viewed edge-on?")]
	public bool FadeEdge;

	[Tooltip("How sharp the transition between visible and invisible is")]
	public float FadePower = 2.0f;

	[Tooltip("Should the plane fade out when it's in front of the star?")]
	public bool ClipNear;

	[Tooltip("How sharp the transition between visible and invisible is")]
	public float ClipPower = 2.0f;

	[Tooltip("How much this prominence gets shifted toward the camera when rendering in world coordinates")]
	[FormerlySerializedAs("ObserverOffset")]
	public float CameraOffset;
	
	// The planes used to make up this prominence
	[FormerlySerializedAs("planes")]
	public List<SgtProminencePlane> Planes;
	
	// The material applied to all segments
	[System.NonSerialized]
	public Material Material;

	// The mesh applied to all segments
	[System.NonSerialized]
	public Mesh Mesh;

	[SerializeField]
	[HideInInspector]
	private bool startCalled;

	[System.NonSerialized]
	private bool updateMaterialCalled;

	[System.NonSerialized]
	protected bool updateMeshCalled;

	[System.NonSerialized]
	private bool updatePlanesCalled;

	public float Width
	{
		get
		{
			return OuterRadius - InnerRadius;
		}
	}

	[ContextMenu("Update Material")]
	public void UpdateMaterial()
	{
		updateMaterialCalled = true;

		if (Material == null)
		{
			Material = SgtHelper.CreateTempMaterial("Promminence (Generated)", SgtHelper.ShaderNamePrefix + "Prominence");

			if (Planes != null)
			{
				for (var i = Planes.Count - 1; i >= 0; i--)
				{
					var plane = Planes[i];

					if (plane != null)
					{
						plane.SetMaterial(Material);
					}
				}
			}
		}

		var color       = SgtHelper.Premultiply(SgtHelper.Brighten(Color, Brightness));
		var renderQueue = (int)RenderQueue + RenderQueueOffset;

		if (Material.renderQueue != renderQueue)
		{
			Material.renderQueue = renderQueue;
		}

		Material.SetTexture("_MainTex", MainTex);
		Material.SetColor("_Color", color);
		Material.SetVector("_WorldPosition", transform.position);

		SgtHelper.SetTempMaterial(Material);

		if (FadeEdge == true)
		{
			SgtHelper.EnableKeyword("SGT_A");

			Material.SetFloat("_FadePower", FadePower);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_A");
		}

		if (ClipNear == true)
		{
			SgtHelper.EnableKeyword("SGT_B");

			Material.SetFloat("_ClipPower", ClipPower);
		}
		else
		{
			SgtHelper.DisableKeyword("SGT_B");
		}
	}

	[ContextMenu("Update Mesh")]
	public void UpdateMesh()
	{
		updateMeshCalled = true;

		if (Mesh == null)
		{
			Mesh = SgtHelper.CreateTempMesh("Plane");

			if (Planes != null)
			{
				for (var i = Planes.Count - 1; i >= 0; i--)
				{
					var plane = Planes[i];

					if (plane != null)
					{
						plane.SetMesh(Mesh);
					}
				}
			}
		}
		else
		{
			Mesh.Clear();
		}

		if (PlaneDetail >= 2)
		{
			var detail    = Mathf.Min(PlaneDetail, SgtHelper.QuadsPerMesh / 2); // Limit the amount of vertices that get made
			var positions = new Vector3[detail * 2 + 2];
			var normals   = new Vector3[detail * 2 + 2];
			var coords1   = new Vector2[detail * 2 + 2];
			var coords2   = new Vector2[detail * 2 + 2];
			var indices   = new int[detail * 6];
			var angleStep = SgtHelper.Divide(Mathf.PI * 2.0f, detail);
			var coordStep = SgtHelper.Reciprocal(detail);
			
			for (var i = 0; i <= detail; i++)
			{
				var coord = coordStep * i;
				var angle = angleStep * i;
				var sin   = Mathf.Sin(angle);
				var cos   = Mathf.Cos(angle);
				var offV  = i * 2;
				
				positions[offV + 0] = new Vector3(sin * InnerRadius, 0.0f, cos * InnerRadius);
				positions[offV + 1] = new Vector3(sin * OuterRadius, 0.0f, cos * OuterRadius);

				normals[offV + 0] = Vector3.up;
				normals[offV + 1] = Vector3.up;

				coords1[offV + 0] = new Vector2(0.0f, coord * InnerRadius);
				coords1[offV + 1] = new Vector2(1.0f, coord * OuterRadius);

				coords2[offV + 0] = new Vector2(InnerRadius, 0.0f);
				coords2[offV + 1] = new Vector2(OuterRadius, 0.0f);
			}

			for (var i = 0; i < detail; i++)
			{
				var offV = i * 2;
				var offI = i * 6;

				indices[offI + 0] = offV + 0;
				indices[offI + 1] = offV + 1;
				indices[offI + 2] = offV + 2;
				indices[offI + 3] = offV + 3;
				indices[offI + 4] = offV + 2;
				indices[offI + 5] = offV + 1;
			}
			
			Mesh.vertices  = positions;
			Mesh.normals   = normals;
			Mesh.uv        = coords1;
			Mesh.uv2       = coords2;
			Mesh.triangles = indices;
		}
	}
	
	[ContextMenu("Update Planes")]
	public void UpdatePlanes()
	{
		updatePlanesCalled = true;

		SgtHelper.BeginRandomSeed(Seed);
		{
			for (var i = 0; i < PlaneCount; i++)
			{
				var plane = GetOrAddPlane(i);
				
				plane.SetRotation(Random.rotationUniform);
			}
		}
		SgtHelper.EndRandomSeed();
		
		// Remove any excess
		if (Planes != null)
		{
			var min = Mathf.Max(0, PlaneCount);

			for (var i = Planes.Count - 1; i >= min; i--)
			{
				SgtProminencePlane.Pool(Planes[i]);

				Planes.RemoveAt(i);
			}
		}
	}

	public static SgtProminence CreateProminence(int layer = 0, Transform parent = null)
	{
		return CreateProminence(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	public static SgtProminence CreateProminence(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
	{
		var gameObject = SgtHelper.CreateGameObject("Prominence", layer, parent, localPosition, localRotation, localScale);
		var prominence = gameObject.AddComponent<SgtProminence>();

		return prominence;
	}

#if UNITY_EDITOR
	[MenuItem(SgtHelper.GameObjectMenuPrefix + "Prominence", false, 10)]
	public static void CreateProminenceMenuItem()
	{
		var parent     = SgtHelper.GetSelectedParent();
		var prominence = CreateProminence(parent != null ? parent.gameObject.layer : 0, parent);

		SgtHelper.SelectAndPing(prominence);
	}
#endif

	protected virtual void OnEnable()
	{
		AllProminences.Add(this);

		Camera.onPreCull    += CameraPreCull;
		Camera.onPreRender  += CameraPreRender;
		Camera.onPostRender += CameraPostRender;

		if (Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				var plane = Planes[i];

				if (plane != null)
				{
					plane.gameObject.SetActive(true);
				}
			}
		}

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
			
			CheckUpdateCalls();
		}
	}

	protected virtual void OnDisable()
	{
		AllProminences.Remove(this);

		Camera.onPreCull    -= CameraPreCull;
		Camera.onPreRender  -= CameraPreRender;
		Camera.onPostRender -= CameraPostRender;

		if (Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				var plane = Planes[i];

				if (plane != null)
				{
					plane.gameObject.SetActive(false);
				}
			}
		}
	}

	protected virtual void OnDestroy()
	{
		if (Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				SgtProminencePlane.MarkForDestruction(Planes[i]);
			}
		}

		SgtObjectPool<Mesh>.Add(Mesh, m => m.Clear());
		
		SgtHelper.Destroy(Material);
	}

#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.matrix = transform.localToWorldMatrix;

		Gizmos.DrawWireSphere(Vector3.zero, InnerRadius);

		Gizmos.DrawWireSphere(Vector3.zero, OuterRadius);
	}
#endif

	private void CameraPreCull(Camera camera)
	{
		if (CameraOffset != 0.0f && Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				var plane = Planes[i];

				if (plane != null)
				{
					var planeTransform = plane.transform;
					var observerDir    = (planeTransform.position - camera.transform.position).normalized;

					plane.BeginSave();
					{
						planeTransform.position += observerDir * CameraOffset;
					}
					plane.EndSave(camera);
				}
			}
		}
	}

	private void CameraPreRender(Camera camera)
	{
		if (Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				var plane = Planes[i];

				if (plane != null)
				{
					plane.Restore(camera);
				}
			}
		}
	}

	private void CameraPostRender(Camera camera)
	{
		if (Planes != null)
		{
			for (var i = Planes.Count - 1; i >= 0; i--)
			{
				var plane = Planes[i];

				if (plane != null)
				{
					plane.Restore();
				}
			}
		}
	}

	private SgtProminencePlane GetOrAddPlane(int index)
	{
		var plane = default(SgtProminencePlane);

		if (Planes == null)
		{
			Planes = new List<SgtProminencePlane>();
		}

		if (index < Planes.Count)
		{
			plane = Planes[index];
		}
		else
		{
			Planes.Add(plane);
		}

		if (plane == null)
		{
			plane = Planes[index] = SgtProminencePlane.Create(this);

			plane.SetMaterial(Material);
			plane.SetMesh(Mesh);
		}

		return plane;
	}

	private void CheckUpdateCalls()
	{
		if (updateMaterialCalled == false)
		{
			UpdateMaterial();
		}

		if (updateMeshCalled == false)
		{
			UpdateMesh();
		}

		if (updatePlanesCalled == false)
		{
			UpdatePlanes();
		}
	}
}