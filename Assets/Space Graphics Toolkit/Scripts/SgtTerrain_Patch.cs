using UnityEngine;

public partial class SgtTerrain
{
	public delegate void CalculateDisplacementDelegate(Vector3 localPosition, ref float displacement);

	// Called when the displacement of a specific point is being calculated
	public CalculateDisplacementDelegate OnCalculateDisplacement;
	
	// Called when all mesh data should be modified via the static current___ fields
	public System.Action OnCalculateVertexData;

	// Called when a patch should be populated with objects
	public System.Action<SgtPatch> OnPopulatePatch;

	// Called when a patch should be cleared of objects
	public System.Action<SgtPatch> OnDepopulatePatch;

	// Used during patch mesh generation
	public static Vector3 currentPosition;
	public static Vector3 currentPointCenter;
	public static Vector2 currentCoord1;
	public static Vector2 currentCoord2;
	public static Color   currentColor;
	public static Vector3 currentNormal;
	public static Vector4 currentTangent;

	[System.NonSerialized]
	private Vector3[] positions;

	[System.NonSerialized]
	private Vector2[] coords1;

	[System.NonSerialized]
	private Vector2[] coords2;

	[System.NonSerialized]
	private Color[] colors;

	[System.NonSerialized]
	private Vector3[] normals;

	[System.NonSerialized]
	private Vector4[] tangents;
	
	[System.NonSerialized]
	private int[] indices;

	[System.NonSerialized]
	private Vector3[] spots;

	[System.NonSerialized]
	private Vector3[] cellNormals;

	[System.NonSerialized]
	private Vector3[] cellTangents;

	[System.NonSerialized]
	private int[] outerIndices;

	[System.NonSerialized]
	private int[] innerIndices;

	public void GenerateMesh(SgtPatch patch)
	{
		if (patch == null)
		{
			throw new System.ArgumentNullException("Cannot generate mesh for a null patch");
		}

		if (Resolution <= 0)
		{
			throw new System.ArgumentNullException("Cannot generate with a resolution of " + Resolution);
		}
		
		CalculatePoints(patch);

		CalculateCells(patch);

		CalculateVertexData(patch);

		CalculateIndices(patch);
		
		var mesh = patch.Mesh;

		// Reset mesh?
		if (mesh.vertexCount != positions.Length)
		{
			mesh.Clear();
		}

		mesh.vertices  = positions;
		mesh.uv        = coords1;
		mesh.uv2       = coords2;
		mesh.colors    = colors;
		mesh.normals   = normals;
		mesh.tangents  = tangents;
		mesh.triangles = indices;

		mesh.RecalculateBounds();

		patch.MeshCenter = mesh.bounds.center;
	}

	// Calculates all local points for the patch, including one extra border of points
	private void CalculatePoints(SgtPatch patch)
	{
		var quadRes  = Resolution;
		var spotMin  = -1;
		var spotMax  = quadRes + 2;
		var spotRes  = quadRes + 3;
		var spotPosB = patch.PointBL;
		var spotPosT = patch.PointTL;
		var spotVecB = patch.PointBR - spotPosB;
		var spotVecT = patch.PointTR - spotPosT;
		var spotStep = 1.0f / quadRes;

		SgtHelper.RequireArraySize(ref spots, spotRes * spotRes);

		for (var y = spotMin; y < spotMax; y++)
		{
			var spotOff = (y + 1) * spotRes + 1;
			var spotV   = y * spotStep;

			for (var x = spotMin; x < spotMax; x++)
			{
				var spotU = x * spotStep;
				var spot  = Vector3.LerpUnclamped(spotPosB + spotVecB * spotU, spotPosT + spotVecT * spotU, spotV);

				spot = GetSurfacePositionLocal(spot);
				
				spots[spotOff + x] = spot;
			}
		}
	}

	// Calculates the normal and tangent for each patch quad, including one extra border of quads
	private void CalculateCells(SgtPatch patch)
	{
		var quadRes = Resolution;
		var cellRes = quadRes + 2;
		var cellTot = cellRes * cellRes;
		var spotRes = quadRes + 3;
		
		SgtHelper.RequireArraySize(ref cellNormals , cellTot);
		SgtHelper.RequireArraySize(ref cellTangents, cellTot);
		
		for (var y = 0; y < cellRes; y++)
		{
			var spotOffBL = y * spotRes;
			var spotOffBR = spotOffBL + 1;
			var spotOffTL = spotOffBL + spotRes;
			var spotOffTR = spotOffTL + 1;
			var cellOff    = y * cellRes;

			for (var x = 0; x < cellRes; x++)
			{
				var spotBL = spots[spotOffBL + x];
				var spotBR = spots[spotOffBR + x];
				var spotTL = spots[spotOffTL + x];
				var spotTR = spots[spotOffTR + x];

				var spotVecB = spotBL - spotBR;
				var spotVecT = spotTL - spotTR;
				var spotVecL = spotBL - spotTL;
				var spotVecR = spotBR - spotTR;
				
				var spotVecH = spotVecB + spotVecT;
				var spotVecV = spotVecL + spotVecR;
				var cellIdx  = cellOff + x;

				cellNormals[cellIdx] = Vector3.Cross(spotVecH, spotVecV);

				cellTangents[cellIdx] = spotVecV;
			}
		}
	}

	private void CalculateVertexData(SgtPatch patch)
	{
		var quadRes       = Resolution;
		var vertRes       = quadRes + 1;
		var cellRes       = quadRes + 2;
		var spotRes       = quadRes + 3;
		var quadRecip     = 1.0f / quadRes;
		var coordPosB     = patch.CoordBL;
		var coordPosT     = patch.CoordTL;
		var coordVecB     = patch.CoordBR - coordPosB;
		var coordVecT     = patch.CoordTR - coordPosT;
		var totalVertices = vertRes * vertRes + quadRes * 4;

		SgtHelper.RequireArraySize(ref positions, totalVertices);
		SgtHelper.RequireArraySize(ref coords1  , totalVertices);
		SgtHelper.RequireArraySize(ref coords2  , totalVertices);
		SgtHelper.RequireArraySize(ref colors   , totalVertices);
		SgtHelper.RequireArraySize(ref normals  , totalVertices);
		SgtHelper.RequireArraySize(ref tangents , totalVertices);

		for (var y = 0; y < vertRes; y++)
		{
			var quadV     = y * quadRecip;
			var vertOff   = y * vertRes;
			var spotOff   = (y + 1) * spotRes + 1;
			var cellOffBL = y * cellRes;
			var cellOffBR = cellOffBL + 1;
			var cellOffTL = cellOffBL + cellRes;
			var cellOffTR = cellOffTL + 1;

			for (var x = 0; x < vertRes; x++)
			{
				var quadU     = x * quadRecip;
				var cellIdxBL = cellOffBL + x;
				var cellIdxBR = cellOffBR + x;
				var cellIdxTL = cellOffTL + x;
				var cellIdxTR = cellOffTR + x;
				var cellN     = cellNormals [cellIdxBL] + cellNormals [cellIdxBR] + cellNormals [cellIdxTL] + cellNormals [cellIdxTR];
				var cellT     = cellTangents[cellIdxBL] + cellTangents[cellIdxBR] + cellTangents[cellIdxTL] + cellTangents[cellIdxTR];
				
				currentPointCenter = (patch.PointBL + patch.PointBR + patch.PointTL + patch.PointTR) * 0.25f;
				currentPosition    = spots[spotOff + x];
				currentCoord1      = Vector3.LerpUnclamped(coordPosB + coordVecB * quadU, coordPosT + coordVecT * quadU, quadV);
				currentCoord2      = new Vector2(quadU, quadV);
				currentColor       = DefaultColor;
				currentNormal      = Normalize(cellN);
				currentTangent     = SgtHelper.NewVector4(Normalize(cellT), 1.0f);

				if (OnCalculateVertexData != null) OnCalculateVertexData();
				
				var vertexI = x + vertOff;

				positions[vertexI] = currentPosition;
				coords1  [vertexI] = currentCoord1;
				coords2  [vertexI] = currentCoord2;
				colors   [vertexI] = currentColor;
				normals  [vertexI] = currentNormal;
				tangents [vertexI] = currentTangent;
			}
		}
	}

	private Vector3 Normalize(Vector3 v)
	{
		var s = v.sqrMagnitude;

		if (s > 0.0f)
		{
			return v / Mathf.Sqrt(s);
		}

		return v;
	}

	private void CalculateIndices(SgtPatch patch)
	{
		var quadRes        = Resolution;
		var vertRes        = quadRes + 1;
		var mainIndexCount = quadRes * quadRes * 6;
		
		// Regen skirt indices?
		if (SgtHelper.RequireArraySize(ref indices, mainIndexCount) == true)
		{
			// Main vertices (optimize non-planar quads)
			for (var y = 0; y < quadRes; y++)
			{
				var indexO  = y * quadRes;
				var vertexO = y * vertRes;

				for (var x = 0; x < quadRes; x++)
				{
					var index     = (x + indexO) * 6;
					var vertexA   = x + vertexO;
					var vertexB   = vertexA + 1;
					var vertexC   = vertexA + vertRes;
					var vertexD   = vertexB + vertRes;
					var positionA = positions[vertexA];
					var positionB = positions[vertexB];
					var positionC = positions[vertexC];
					var positionD = positions[vertexD];

					// Turn edge?
					var a = Area(positionA, positionB, positionC) + Area(positionD, positionC, positionB);
					var b = Area(positionA, positionB, positionD) + Area(positionD, positionC, positionA);

					if (a <= b)
					{
						indices[index + 0] = vertexA;
						indices[index + 1] = vertexB;
						indices[index + 2] = vertexC;
						indices[index + 3] = vertexD;
						indices[index + 4] = vertexC;
						indices[index + 5] = vertexB;
					}
					else
					{
						indices[index + 0] = vertexA;
						indices[index + 1] = vertexB;
						indices[index + 2] = vertexD;
						indices[index + 3] = vertexD;
						indices[index + 4] = vertexC;
						indices[index + 5] = vertexA;
					}
				}
			}
		}
	}
	
	private float Area(Vector3 a, Vector3 b, Vector3 c)
	{
		return Vector3.Cross(a - b, a - c).magnitude * 0.5f;
	}
}