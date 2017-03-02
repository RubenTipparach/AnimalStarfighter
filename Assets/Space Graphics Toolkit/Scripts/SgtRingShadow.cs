using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(SgtRingShadow))]
public class SgtRingShadow_Editor : SgtEditor<SgtRingShadow>
{
	protected override void OnInspector()
	{
		BeginError(Any(t => t.Light == null));
			DrawDefault("Light");
		EndError();
		BeginError(Any(t => t.Texture == null));
			DrawDefault("Texture");
		EndError();
		DrawDefault("RingMesh");
		BeginDisabled(Any(t => SgtHelper.Enabled(t.RingMesh)));
			BeginError(Any(t => t.RadiusMin < 0.0f || t.RadiusMin >= t.RadiusMax));
				DrawDefault("RadiusMin");
			EndError();
			BeginError(Any(t => t.RadiusMax < 0.0f || t.RadiusMin >= t.RadiusMax));
				DrawDefault("RadiusMax");
			EndError();
		EndDisabled();
	}
}
#endif

[ExecuteInEditMode]
[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring Shadow")]
public class SgtRingShadow : SgtShadow
{
	[Tooltip("The texture of the shadow (left = inside, right = outside)")]
	public Texture Texture;
	
	[Tooltip("The ring that this shadow is being cast from")]
	public SgtRingMesh RingMesh;
	
	[Tooltip("The inner radius of the ring casting this shadow (auto set if Ring is set)")]
	[FormerlySerializedAs("InnerRadius")]
	public float RadiusMin = 1.0f;
	
	[Tooltip("The outer radius of the ring casting this shadow (auto set if Ring is set)")]
	[FormerlySerializedAs("OuterRadius")]
	public float RadiusMax = 2.0f;
	
	public override Texture GetTexture()
	{
		return Texture;
	}
	
	public override bool CalculateShadow(ref Matrix4x4 matrix, ref float ratio)
	{
		if (base.CalculateShadow(ref matrix, ref ratio) == true)
		{
			if (Texture != null)
			{
				if (SgtHelper.Enabled(RingMesh) == true)
				{
					RadiusMin = RingMesh.RadiusMin;
					RadiusMax = RingMesh.RadiusMax;
				}
				
				var direction = default(Vector3);
				var position  = default(Vector3);
				var color     = default(Color);
				
				SgtHelper.CalculateLight(Light, transform.position, null, null, ref position, ref direction, ref color);
				
				var rotation = Quaternion.FromToRotation(direction, Vector3.back);
				var squash   = Vector3.Dot(direction, transform.up); // Find how squashed the ellipse is based on light direction
				var width    = transform.lossyScale.x * RadiusMax;
				var length   = transform.lossyScale.z * RadiusMax;
				var axis     = rotation * transform.up; // Find the transformed up axis
				var spin     = Quaternion.LookRotation(Vector3.forward, new Vector2(-axis.x, axis.y)); // Orient the shadow ellipse
				var scale    = SgtHelper.Reciprocal3(new Vector3(width, length * Mathf.Abs(squash), 1.0f));
				var skew     = Mathf.Tan(SgtHelper.Acos(-squash));
				
				var shadowT = SgtHelper.Translation(-transform.position);
				var shadowR = SgtHelper.Rotation(spin * rotation); // Spin the shadow so lines up with its tilt
				var shadowS = SgtHelper.Scaling(scale); // Scale the ring into an oval
				var shadowK = SgtHelper.ShearingZ(new Vector2(0.0f, skew)); // Skew the shadow so it aligns with the ring plane
				
				matrix = shadowS * shadowK * shadowR * shadowT;
				ratio  = SgtHelper.Divide(RadiusMax, RadiusMax - RadiusMin);
				
				return true;
			}
		}
		
		return false;
	}
	
#if UNITY_EDITOR
	protected virtual void OnDrawGizmosSelected()
	{
		if (SgtHelper.Enabled(this) == true)
		{
			var matrix = default(Matrix4x4);
			var ratio  = default(float);
			
			if (CalculateShadow(ref matrix, ref ratio) == true)
			{
				Gizmos.matrix = matrix.inverse;
				
				var distA = 0.0f;
				var distB = 1.0f;
				var scale = 1.0f * Mathf.Deg2Rad;
				var inner = SgtHelper.Divide(RadiusMin, RadiusMax);
				
				for (var i = 1; i < 10; i++)
				{
					var posA  = new Vector3(0.0f, 0.0f, distA);
					var posB  = new Vector3(0.0f, 0.0f, distB);

					Gizmos.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Pow(0.75f, i) * 0.125f);

					for (var a = 1; a <= 360; a++)
					{
						posA.x = posB.x = Mathf.Sin(a * scale);
						posA.y = posB.y = Mathf.Cos(a * scale);
						
						Gizmos.DrawLine(posA, posB);

						posA.x = posB.x = posA.x * inner;
						posA.y = posB.y = posA.y * inner;

						Gizmos.DrawLine(posA, posB);
					}

					distA = distB;
					distB = distB * 2.0f;
				}
			}
		}
	}
#endif
}