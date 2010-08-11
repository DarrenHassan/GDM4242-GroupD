// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="BlinkGrid"/> entity type.
	/// </summary>
	public class BlinkGridType : MapObjectType
	{
		[FieldSerialize]
		string materialName = "";
		[FieldSerialize]
		Vec2i gridSize = new Vec2i( 8, 8 );
		[FieldSerialize]
		float updateTime = 1;

		[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
		public string MaterialName
		{
			get { return materialName; }
			set { materialName = value; }
		}

		[DefaultValue( typeof( Vec2i ), "8 8" )]
		public Vec2i GridSize
		{
			get { return gridSize; }
			set 
			{
				if( value.X <= 0 || value.X > 60 || value.Y <= 0 || value.Y > 60 )
				{
					Log.Warning( "Invalid GridSize. Should be in an interval [1, 60]." );
					return;
				}
				gridSize = value;
			}
		}

		[DefaultValue( 1.0f )]
		public float UpdateTime
		{
			get { return updateTime; }
			set { updateTime = value; }
		}
	}

	/// <summary>
	/// Example of dynamic geometry.
	/// </summary>
	public class BlinkGrid : MapObject
	{
		Mesh mesh;
		MapObjectAttachedMesh attachedMesh;

		float updateTimeRemaining;

		bool needUpdateVertices;
		bool needUpdateIndices;

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		struct Vertex
		{
			public Vec3 position;
			public Vec3 normal;
			public Vec2 texCoord;
		}

		///////////////////////////////////////////

		BlinkGridType _type = null; public new BlinkGridType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			CreateMesh();
			AttachMesh();
			AddTimer();
		}

		protected override void OnDestroy()
		{
			DetachMesh();
			DestroyMesh();
			base.OnDestroy();
		}

		void CreateMesh()
		{
			string meshName = MeshManager.Instance.GetUniqueName( "DynamicMesh" );
			mesh = MeshManager.Instance.CreateManual( meshName );
			
			//set mesh gabarites
			Bounds bounds = new Bounds( new Vec3( -.1f, -.5f, -.5f ), new Vec3( .1f, .5f, .5f ) );
			mesh.SetBoundsAndRadius( bounds, bounds.GetRadius() );

			SubMesh subMesh = mesh.CreateSubMesh();
			subMesh.UseSharedVertices = false;

			int maxVertices = ( Type.GridSize.X + 1 ) * ( Type.GridSize.Y + 1 );
			int maxIndices = Type.GridSize.X * Type.GridSize.Y * 6;
			
			//init vertexData
			VertexDeclaration declaration = subMesh.VertexData.VertexDeclaration;
			declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			declaration.AddElement( 0, 24, VertexElementType.Float2, 
				VertexElementSemantic.TextureCoordinates, 0 );

			VertexBufferBinding bufferBinding = subMesh.VertexData.VertexBufferBinding;
			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				32, maxVertices, HardwareBuffer.Usage.DynamicWriteOnly );
			bufferBinding.SetBinding( 0, vertexBuffer, true );

			//init indexData
			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
				HardwareIndexBuffer.IndexType._16Bit, maxIndices, HardwareBuffer.Usage.DynamicWriteOnly );
			subMesh.IndexData.SetIndexBuffer( indexBuffer, true );

			//set material	
			subMesh.MaterialName = Type.MaterialName;

			needUpdateVertices = true;
			needUpdateIndices = true;
		}

		void DestroyMesh()
		{
			if( mesh != null )
			{
				mesh.Dispose();
				mesh = null;
			}
		}

		void AttachMesh()
		{
			attachedMesh = new MapObjectAttachedMesh();
			attachedMesh.CastDynamicShadows = false;
			attachedMesh.MeshName = mesh.Name;
			Attach( attachedMesh );
		}

		void DetachMesh()
		{
			if( attachedMesh != null )
			{
				Detach( attachedMesh );
				attachedMesh = null;
			}
		}

		void DoTick()
		{
			updateTimeRemaining -= TickDelta;
			if( updateTimeRemaining < 0 )
			{
				updateTimeRemaining += Type.UpdateTime;
				needUpdateIndices = true;
			}
		}

		protected override void OnTick()
		{
			base.OnTick();
			DoTick();
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();
			DoTick();
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( attachedMesh != null )
			{
				bool visible = camera.IsIntersectsFast( MapBounds );
				attachedMesh.Visible = visible;
				if( visible )
				{
					//update mesh if needed
					if( needUpdateVertices )
					{
						UpdateMeshVertices();
						needUpdateVertices = false;
					}
					if( needUpdateIndices )
					{
						UpdateMeshIndices();
						needUpdateIndices = false;
					}
				}
			}
		}

		void UpdateMeshVertices()
		{
			SubMesh subMesh = mesh.SubMeshes[ 0 ];

			Vec2 cellSize = 1.0f / Type.GridSize.ToVec2();

			HardwareVertexBuffer vertexBuffer = subMesh.VertexData.VertexBufferBinding.GetBuffer( 0 );
			unsafe
			{
				Vertex* buffer = (Vertex*)vertexBuffer.Lock(
					HardwareBuffer.LockOptions.Normal ).ToPointer();

				subMesh.VertexData.VertexCount = ( Type.GridSize.X + 1 ) * ( Type.GridSize.Y + 1 );

				for( int y = 0; y < Type.GridSize.Y + 1; y++ )
				{
					for( int x = 0; x < Type.GridSize.X + 1; x++ )
					{
						Vertex vertex = new Vertex();
						vertex.position = new Vec3( 0,
							(float)( x - Type.GridSize.X / 2 ) * cellSize.X,
							(float)( y - Type.GridSize.Y / 2 ) * cellSize.Y );
						vertex.normal = new Vec3( 1, 0, 0 );
						vertex.texCoord = new Vec2( (float)x / (float)Type.GridSize.X,
							1.0f - (float)y / (float)Type.GridSize.Y );

						*buffer = vertex;
						buffer++;
					}
				}
				
				vertexBuffer.Unlock();
			}
		}

		void UpdateMeshIndices()
		{
			if( mesh == null )
				return;

			SubMesh subMesh = mesh.SubMeshes[ 0 ];

			HardwareIndexBuffer indexBuffer = subMesh.IndexData.IndexBuffer;

			unsafe
			{
				ushort* buffer = (ushort*)indexBuffer.Lock( HardwareBuffer.LockOptions.Normal ).ToPointer();

				subMesh.IndexData.IndexCount = 0;

				for( int y = 0; y < Type.GridSize.Y; y++ )
				{
					for( int x = 0; x < Type.GridSize.X; x++ )
					{
						bool enableCell = World.Instance.Random.Next( 2 ) == 0;

						if( enableCell )
						{
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * y + x ); buffer++;
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * y + x + 1 ); buffer++;
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * ( y + 1 ) + x + 1 ); buffer++;
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * ( y + 1 ) + x + 1 ); buffer++;
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * ( y + 1 ) + x ); buffer++;
							*buffer = (ushort)( ( Type.GridSize.X + 1 ) * y + x ); buffer++;

							subMesh.IndexData.IndexCount += 6;
						}
					}
				}

				indexBuffer.Unlock();
			}
		}

	}
}
