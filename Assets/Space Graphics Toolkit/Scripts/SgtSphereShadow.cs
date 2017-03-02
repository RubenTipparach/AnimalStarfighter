using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtSphereShadow))]
public class SgtSphereShadow_Editor : SgtEditor<SgtSphereShadow>
{
	protected override void OnInspector()
	{
		var updateTexture = false;

		BeginError(Any(t => t.Light == null));
			DrawDefault("Light");
		EndError();
		BeginError(Any(t => t.Width < 1));
			DrawDefault("Width", ref updateTexture);
		EndError();
		DrawDefault("Format", ref updateTexture);
		BeginError(Any(t => t.PowerR < 1.0f));
			DrawDefault("PowerR", ref updateTexture);
		EndError();
		BeginError(Any(t => t.PowerG < 1.0f));
			DrawDefault("PowerG", ref updateTexture);
		EndError();
		BeginError(Any(t => t.PowerB < 1.0f));
			DrawDefault("PowerB", ref updateTexture);
		EndError();
		BeginError(Any(t => t.Opacity < 0.0f));
			DrawDefault("Opacity", ref updateTexture);
		EndError();
		BeginError(Any(t => t.RadiusMin < 0.0f || t.RadiusMin >= t.RadiusMax));
			DrawDefault("RadiusMin");
		EndError();
		BeginError(Any(t => t.RadiusMax < 0.0f || t.RadiusMin >= t.RadiusMax));
			DrawDefault("RadiusMax");
		EndError();

		if (updateTexture == true) DirtyEach(t => t.UpdateTexture());
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Sphere Shadow")]
public class SgtSphereShadow : SgtShadow
{
	[Tooltip("The resolution of the surface/space optical thickness transition in pixels")]
	public int Width = 256;
	
	[Tooltip("The format of this texture")]
	public TextureFormat Format = TextureFormat.ARGB32;

	[Tooltip("The power of the sunset red channel transition")]
	public float PowerR = 2.0f;
	
	[Tooltip("The power of the sunset green channel transition")]
	public float PowerG = 2.0f;
	
	[Tooltip("The power of the sunset blue channel transition")]
	public float PowerB = 2.0f;
	
	[Tooltip("The opacity shadow")]
	public float Opacity = 1.0f;
	
	[Tooltip("The inner radius of the sphere in local coordinates")]
	[FormerlySerializedAs("InnerRadius")]
	public float RadiusMin = 1.0f;
	
	[Tooltip("The outer radius of the sphere in local coordinates")]
	[FormerlySerializedAs("OuterRadius")]
	public float RadiusMax = 2.0f;
	
	[System.NonSerialized]
	private Texture2D generatedTexture;
	
	[SerializeField]
	[HideInInspector]
	private bool startCalled;
	
	public Texture2D GeneratedTexture
	{
		get
		{
			return generatedTexture;
		}
	}
	
	public override Texture GetTexture()
	{
		if (generatedTexture == false)
		{
			UpdateTexture();
		}
		
		return generatedTexture;
	}

	[ContextMenu("Update Textures")]
	public void UpdateTexture()
	{
		if (Width > 0)
		{
			// Destroy if invalid
			if (generatedTexture != null)
			{
				if (generatedTexture.width != Width || generatedTexture.height != 1 || generatedTexture.format != Format)
				{
					generatedTexture = SgtHelper.Destroy(generatedTexture);
				}
			}

			// Create?
			if (generatedTexture == null)
			{
				generatedTexture = SgtHelper.CreateTempTexture2D("Sphere Shadow (Generated)", Width, 1, Format);

				generatedTexture.wrapMode = TextureWrapMode.Clamp;
			}

			var color = Color.clear;
			var stepX = 1.0f / (Width - 1);

			for (var x = 0; x < Width; x++)
			{
				var u = x * stepX;

				WriteTexture(u, x);
			}
			
			generatedTexture.Apply();
		}
	}

	private void WriteTexture(float u, int x)
	{
		var color = default(Color);
		
		color.r = 1.0f - Mathf.Pow(1.0f - u, PowerR) * Opacity;
		color.g = 1.0f - Mathf.Pow(1.0f - u, PowerG) * Opacity;
		color.b = 1.0f - Mathf.Pow(1.0f - u, PowerB) * Opacity;
		color.a = 1.0f;
		
		generatedTexture.SetPixel(x, 0, color);
	}
	
	public override bool CalculateShadow(ref Matrix4x4 matrix, ref float ratio)
	{
		if (base.CalculateShadow(ref matrix, ref ratio) == true)
		{
			var direction = default(Vector3);
			var position  = default(Vector3);
			var color     = default(Color);
			
			SgtHelper.CalculateLight(Light, transform.position, null, null, ref position, ref direction, ref color);
			
			var dot      = Vector3.Dot(direction, transform.up);
			var radiusXZ = (transform.lossyScale.x + transform.lossyScale.z) * 0.5f * RadiusMax;
			var radiusY  = transform.lossyScale.y * RadiusMax;
			var radius   = GetRadius(radiusY, radiusXZ, dot * Mathf.PI * 0.5f);
			var rotation = Quaternion.FromToRotation(direction, Vector3.back);
			var vector   = rotation * transform.up;
			var spin     = Quaternion.LookRotation(Vector3.forward, new Vector2(-vector.x, vector.y)); // Orient the shadow ellipse
			var scale    = SgtHelper.Reciprocal3(new Vector3(radiusXZ, radius, 1.0f));
			var shadowT  = SgtHelper.Translation(-transform.position);
			var shadowR  = SgtHelper.Rotation(spin * rotation);
			var shadowS  = SgtHelper.Scaling(scale);
			
			matrix = shadowS * shadowR * shadowT;
			ratio  = SgtHelper.Divide(RadiusMax, RadiusMax - RadiusMin);
			
			return true;
		}
		
		return false;
	}

	private float GetRadius(float a, float b, float theta)
	{
		var s = Mathf.Sin(theta);
		var c = Mathf.Cos(theta);
		var z = Mathf.Sqrt((a*a)*(s*s)+(b*b)*(c*c));

		if (z != 0.0f)
		{
			return (a * b) / z;
		}

		return a;
	}
	
	protected virtual void OnEnable()
	{
		if (startCalled == true)
		{
			CheckUpdateCalls();
		}
	}

	protected override void Start()
	{
		if (startCalled == false)
		{
			startCalled = true;
			
			CheckUpdateCalls();
		}
	}
	
	protected virtual void OnDestroy()
	{
		SgtHelper.Destroy(generatedTexture);
	}
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var matrix = default(Matrix4x4);
			var ratio  = default(float);

			Gizmos.matrix = transform.localToWorldMatrix;
			
			Gizmos.DrawWireSphere(Vector3.zero, RadiusMin);
			Gizmos.DrawWireSphere(Vector3.zero, RadiusMax);
			
			if (CalculateShadow(ref matrix, ref ratio) == true)
			{
				Gizmos.matrix = matrix.inverse;

				var distA = 0.0f;
				var distB = 1.0f;
				var scale = 1.0f * Mathf.Deg2Rad;

				for (var i = 0; i < 10; i++)
				{
					var posA  = new Vector3(0.0f, 0.0f, distA);
					var posB  = new Vector3(0.0f, 0.0f, distB);

					Gizmos.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Pow(0.75f, i) * 0.125f);

					for (var a = 1; a <= 360; a++)
					{
						posA.x = posB.x = Mathf.Sin(a * scale);
						posA.y = posB.y = Mathf.Cos(a * scale);
						
						Gizmos.DrawLine(posA, posB);
					}

					distA = distB;
					distB = distB * 2.0f;
				}
			}
		}
	}
#endif
	
	private void CheckUpdateCalls()
	{
		if (generatedTexture == null)
		{
			UpdateTexture();
		}
	}
}