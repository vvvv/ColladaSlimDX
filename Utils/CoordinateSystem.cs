using SharpDX;
using DNMatrix = dnAnalytics.LinearAlgebra.Matrix;
using DNMatrixImpl = dnAnalytics.LinearAlgebra.DenseMatrix;
using DNSolver = dnAnalytics.LinearAlgebra.Solvers.Direct.LUSolver;

namespace ColladaSharpDX.Utils
{
	
	public enum CoordinateSystemType { LeftHanded, RightHanded };

	public class CoordinateSystem
	{
		
		private float meter;
		private CoordinateSystemType type;
		private Vector3 up;
		private Vector3 right;
		private Matrix matrix = Matrix.Identity;
		
		/// <summary>
		/// The type of the coordinate system.
		/// It's either left or right handed.
		/// </summary>
		public CoordinateSystemType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
				UpdateMatrix();
			}
		}
		
		/// <summary>
		/// The vector in the up direction of the coordinate system.
		/// </summary>
		public Vector3 Up
		{
			get
			{
				return up;
			}
			set
			{
				up = value;
				UpdateMatrix();
			}
		}
		
		/// <summary>
		/// The vector in the right direction of the coordinate system.
		/// </summary>
		public Vector3 Right
		{
			get
			{
				return right;
			}
			set
			{
				right = value;
				UpdateMatrix();
			}
		}
		
		/// <summary>
		/// The vector in the inward direction of the coordinate system.
		/// </summary>
		public Vector3 Inward
		{
			get
			{
				float lr = 1f;
				if (type == CoordinateSystemType.RightHanded)
					lr = -1f;
				
				return Vector3.Cross(right, up) * lr;
			}
		}
		
		/// <summary>
		/// How many real-world meters in one
		/// distance unit as a floating-point number. For
		/// example, 1.0 for the name "meter"; 1000 for the
		/// name "kilometer"; 0.3048 for the name "foot".
		/// </summary>
		public float Meter
		{
			get
			{
				return meter;
			}
			set
			{
				meter = value;
				UpdateMatrix();
			}
		}
		
		public Matrix Matrix
		{
			get
			{
				return matrix;
			}
		}
		
		private CoordinateSystem()
		{
			
		}
		
		public CoordinateSystem(CoordinateSystem cs) :
			this(
				cs.Type,
				cs.Up,
				cs.Right,
				cs.Meter)
		{
			
		}
		
		public CoordinateSystem(CoordinateSystemType type) : 
			this(type, 
			     new Vector3(0f, 1f, 0f),
			     new Vector3(1f, 0f, 0f))
		{
			
		}
		
		public CoordinateSystem(
			CoordinateSystemType type, 
			Vector3 up, 
			Vector3 right) :
			this(type, 
			     up,
			     right,
			     1f)
		{
			
		}
		
		public CoordinateSystem(
			CoordinateSystemType type, 
			Vector3 up, 
			Vector3 right,
			float meter)
		{
			this.type = type;
			this.up = up;
			this.right = right;
			this.meter = meter;
			UpdateMatrix();
		}
		
		public static Matrix ConversionMatrix(
			CoordinateSystem Source,
			CoordinateSystem Target)
		{
			DNSolver solver = new DNSolver();
			DNMatrix conversionMatrix = solver.Solve(
				MatrixToDNMatrix(Source.Matrix),
				MatrixToDNMatrix(Target.Matrix));
			return DNMatrixToMatrix(conversionMatrix);
		}
		
		private void UpdateMatrix()
		{
			Vector3 inward = Inward;
			matrix[0, 0] = right.X / meter;
			matrix[1, 0] = right.Y / meter;
			matrix[2, 0] = right.Z / meter;
			matrix[0, 1] = up.X / meter;
			matrix[1, 1] = up.Y / meter;
			matrix[2, 1] = up.Z / meter;
			matrix[0, 2] = inward.X / meter;
			matrix[1, 2] = inward.Y / meter;
			matrix[2, 2] = inward.Z / meter;
		}
				
		private static DNMatrix MatrixToDNMatrix(Matrix m)
		{
			return new DNMatrixImpl(
				new double[,] {
					{ m[0,0], m[0,1], m[0,2], m[0,3] },
					{ m[1,0], m[1,1], m[1,2], m[1,3] },
					{ m[2,0], m[2,1], m[2,2], m[2,3] },
					{ m[3,0], m[3,1], m[3,2], m[3,3] }
				});
		}
		
		private static Matrix DNMatrixToMatrix(DNMatrix m)
		{
			Matrix result = new Matrix();
			for (int row = 0; row < m.Rows; row++)
			{
				for (int col = 0; col < m.Columns; col++)
				{
					result[row, col] = (float) m[row, col];
				}
			}
			return result;
		}
		
	}
	
}