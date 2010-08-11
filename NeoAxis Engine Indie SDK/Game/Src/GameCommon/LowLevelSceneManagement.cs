// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace GameCommon
{
	//This class only for advanced users.
	//Usually this class need to change only for optimizations.
	//Bad idea to realize here changing visibility of game objects. Use MapObject.OnRender(), SceneNode.Visible for this.
	public static class LowLevelSceneManagement
	{
		static Plane[] GetClipPlanesForSpotLightShadowGeneration( Camera camera, RenderLight light )
		{
			Quat rotation = Quat.FromDirectionZAxisUp( light.Direction );

			Plane[] clipPlanes = new Plane[ 9 ];

			//side planes
			Plane[] sideClipPlanes = light.GetSpotSideClipPlanes();
			for( int n = 0; n < 8; n++ )
				clipPlanes[ n ] = sideClipPlanes[ n ];

			//far plane
			float range = light.AttenuationRange;
			if( range > 10000 )
				range = 10000;
			Vec3 dir1 = rotation * Vec3.YAxis;
			Vec3 dir2 = rotation * Vec3.ZAxis;
			Vec3 p = light.Position + light.Direction * range;
			clipPlanes[ 8 ] = Plane.FromVectors( dir1, dir2, p );

			return clipPlanes;
		}

		static Plane[] GetClipPlanesForDirectionalLightShadowGeneration( Camera camera,
			RenderLight light, float farClipDistance )
		{
			float shadowFarDistance = SceneManager.Instance.ShadowFarDistance;

			Frustum cameraFrustum = FrustumUtils.GetFrustumByCamera( camera, farClipDistance );

			Vec3 cameraPosition = cameraFrustum.Origin;

			Vec3 farCenterPoint;
			{
				float distance = shadowFarDistance;
				//small border
				distance *= 1.05f;

				//not optimal
				distance *= MathFunctions.Sqrt( 2 );

				farCenterPoint = cameraPosition + cameraFrustum.Axis.Item0 * distance;
			}
			Vec3 pyramidCenter = ( cameraPosition + farCenterPoint ) * .5f;

			Plane farPlane;
			{
				Vec3 normal = ( farCenterPoint - cameraPosition ).GetNormalize();
				float distance = Vec3.Dot( normal, farCenterPoint );
				farPlane = new Plane( normal, distance );
			}

			Vec3[] farCorners = new Vec3[ 4 ];
			{
				//4 - top-right far, 5 - top-left far, 6 - bottom-left far, 7 - bottom-right far.
				Vec3[] points = camera.GetWorldSpaceCorners();

				for( int n = 0; n < 4; n++ )
				{
					Ray ray = new Ray( cameraPosition, points[ n + 4 ] - cameraPosition );
					float scale;
					farPlane.RayIntersection( ray, out scale );
					farCorners[ n ] = ray.GetPointOnRay( scale );
				}
			}

			Vec3[] pyramidPoints = new Vec3[ 5 ];
			{
				pyramidPoints[ 0 ] = cameraPosition;
				for( int n = 0; n < 4; n++ )
					pyramidPoints[ n + 1 ] = farCorners[ n ];
			}

			Line[] pyramidEdges = new Line[ 8 ];
			{
				for( int n = 0; n < 4; n++ )
				{
					pyramidEdges[ n ] = new Line( cameraPosition, farCorners[ n ] );
					pyramidEdges[ n + 4 ] = new Line( farCorners[ n ],
						farCorners[ ( n + 1 ) % 4 ] );
				}
			}

			List<Plane> clipPlanes = new List<Plane>( 7 );
			{
				Vec3 lightDirectionOffset = light.Direction * 10000.0f;

				//back planes
				{
					if( farPlane.GetSide( farCenterPoint - lightDirectionOffset ) ==
						Plane.Side.Negative )
					{
						clipPlanes.Add( farPlane );
					}

					for( int n = 0; n < 4; n++ )
					{
						Plane plane = Plane.FromPoints( cameraPosition, farCorners[ n ],
							farCorners[ ( n + 1 ) % 4 ] );

						if( plane.GetSide( cameraPosition - lightDirectionOffset ) ==
							Plane.Side.Negative )
						{
							clipPlanes.Add( farPlane );
						}
					}
				}

				//generate edge planes
				foreach( Line pyramidEdge in pyramidEdges )
				{
					Vec3 p1 = pyramidEdge.Start;
					Vec3 p2 = pyramidEdge.End;
					Vec3 p3 = p1 - lightDirectionOffset;

					Plane plane;
					{
						plane = Plane.FromPoints( p1, p2, p3 );
						if( plane.GetSide( pyramidCenter ) == Plane.Side.Positive )
							plane = Plane.FromPoints( p2, p1, p3 );
					}

					bool existsPyramidPointsOnBothSides = false;
					{
						Plane.Side side = Plane.Side.No;
						bool sideInitialized = false;

						foreach( Vec3 pyramidPoint in pyramidPoints )
						{
							if( pyramidPoint.Equals( pyramidEdge.Start, .00001f ) )
								continue;
							if( pyramidPoint.Equals( pyramidEdge.End, .00001f ) )
								continue;

							Plane.Side s = plane.GetSide( pyramidPoint );

							if( sideInitialized )
							{
								if( side == Plane.Side.Negative && s == Plane.Side.Positive ||
									side == Plane.Side.Positive && s == Plane.Side.Negative )
								{
									existsPyramidPointsOnBothSides = true;
									break;
								}
							}
							else
							{
								side = s;
								sideInitialized = true;
							}
						}
					}
					if( existsPyramidPointsOnBothSides )
						continue;

					clipPlanes.Add( plane );
				}
			}

			return clipPlanes.ToArray();
		}

		static void WalkSpotLightShadowGeneration( Camera camera, RenderLight light,
			Set<SceneNode> outSceneNodes, Set<StaticMeshObject> outStaticMeshObjects )
		{
			Plane[] clipPlanes = GetClipPlanesForSpotLightShadowGeneration( camera, light );
			Sphere clipSphere = new Sphere( light.Position, light.AttenuationRange );

			SceneManager.Instance._SceneGraph.GetObjects( clipPlanes,
				_SceneObjectGroups.SceneNode | _SceneObjectGroups.StaticMeshObject,
				delegate( _ISceneObject sceneObject )
				{
					//clip by sphere
					if( !clipSphere.IsIntersectsBounds( sceneObject._SceneData.Bounds ) )
						return;

					_SceneObjectGroups objGroups = sceneObject._SceneData.Groups;

					//StaticMeshObject
					if( ( objGroups & _SceneObjectGroups.StaticMeshObject ) != 0 )
					{
						StaticMeshObject staticMeshObject = (StaticMeshObject)sceneObject;

						if( !staticMeshObject.Visible )
							return;
						if( !staticMeshObject.CastShadows )
							return;

						outStaticMeshObjects.Add( staticMeshObject );
						return;
					}

					//SceneNode
					if( ( objGroups & _SceneObjectGroups.SceneNode ) != 0 )
					{
						SceneNode sceneNode = (SceneNode)sceneObject;

						if( !sceneNode.Visible )
							return;
						//if( !x.CastShadows )
						//{
						//   can be optimized
						//}
						outSceneNodes.Add( sceneNode );
						return;
					}

					Log.Fatal( "InternalSceneManagement: WalkSpotLightShadowGeneration: invalid sceneObject." );
				} );
		}

		static void WalkPointLightShadowGeneration( Camera camera, RenderLight light,
			Set<SceneNode> outSceneNodes, Set<StaticMeshObject> outStaticMeshObjects )
		{
			//not optimal. need consider camera frustum.

			float range = light.AttenuationRange;
			if( range >= 10000.0f )
				range = 10000.0f;
			Sphere sphere = new Sphere( light.Position, range );

			SceneManager.Instance._SceneGraph.GetObjects( ref sphere,
				_SceneObjectGroups.SceneNode | _SceneObjectGroups.StaticMeshObject,
				delegate( _ISceneObject sceneObject )
				{
					_SceneObjectGroups objGroups = sceneObject._SceneData.Groups;

					//StaticMeshObject
					if( ( objGroups & _SceneObjectGroups.StaticMeshObject ) != 0 )
					{
						StaticMeshObject staticMeshObject = (StaticMeshObject)sceneObject;

						if( !staticMeshObject.Visible )
							return;
						if( !staticMeshObject.CastShadows )
							return;
						outStaticMeshObjects.Add( staticMeshObject );
						return;
					}

					//SceneNode
					if( ( objGroups & _SceneObjectGroups.SceneNode ) != 0 )
					{
						SceneNode sceneNode = (SceneNode)sceneObject;

						if( !sceneNode.Visible )
							return;
						//if( !x.CastShadows )
						//{
						//   can be optimized
						//}
						outSceneNodes.Add( sceneNode );
						return;
					}

					Log.Fatal( "InternalSceneManagement: WalkPointLightShadowGeneration: invalid sceneObject." );
				} );
		}

		static void WalkDirectionalLightShadowGeneration( Camera camera, RenderLight light,
			float farClipDistance, Set<SceneNode> outSceneNodes,
			Set<StaticMeshObject> outStaticMeshObjects )
		{
			Plane[] clipPlanes = GetClipPlanesForDirectionalLightShadowGeneration( camera,
				light, farClipDistance );

			SceneManager.Instance._SceneGraph.GetObjects( clipPlanes,
				_SceneObjectGroups.SceneNode | _SceneObjectGroups.StaticMeshObject,
				delegate( _ISceneObject sceneObject )
				{
					_SceneObjectGroups objGroups = sceneObject._SceneData.Groups;

					//StaticMeshObject
					if( ( objGroups & _SceneObjectGroups.StaticMeshObject ) != 0 )
					{
						StaticMeshObject staticMeshObject = (StaticMeshObject)sceneObject;

						if( !staticMeshObject.Visible )
							return;
						if( !staticMeshObject.CastShadows )
							return;
						outStaticMeshObjects.Add( staticMeshObject );
						return;
					}

					//SceneNode
					if( ( objGroups & _SceneObjectGroups.SceneNode ) != 0 )
					{
						SceneNode sceneNode = (SceneNode)sceneObject;

						if( !sceneNode.Visible )
							return;
						//if( !x.CastShadows )
						//{
						//   can be optimized
						//}
						outSceneNodes.Add( sceneNode );
						return;
					}

					Log.Fatal( "InternalSceneManagement: WalkDirectionalLightShadowGeneration: invalid sceneObject." );
				} );
		}

		//you can use this method for debugging purposes.
		static void WalkAll( Set<SceneNode> outSceneNodes,
			Set<StaticMeshObject> outStaticMeshObjects )
		{
			SceneManager.Instance._SceneGraph.EnumerateAllObjects(
				delegate( _ISceneObject sceneObject )
				{
					_SceneObjectGroups objGroups = sceneObject._SceneData.Groups;

					//StaticMeshObject
					if( ( objGroups & _SceneObjectGroups.StaticMeshObject ) != 0 )
					{
						StaticMeshObject staticMeshObject = (StaticMeshObject)sceneObject;

						if( !staticMeshObject.Visible )
							return;
						outStaticMeshObjects.Add( staticMeshObject );
						return;
					}

					//SceneNode
					if( ( objGroups & _SceneObjectGroups.SceneNode ) != 0 )
					{
						SceneNode sceneNode = (SceneNode)sceneObject;

						if( !sceneNode.Visible )
							return;
						outSceneNodes.Add( sceneNode );
						return;
					}
				} );
		}

		public static void WalkForShadowGeneration( Camera camera, RenderLight light,
			float farClipDistance, Set<SceneNode> outSceneNodes,
			Set<StaticMeshObject> outStaticMeshObjects )
		{
			switch( light.Type )
			{
			case RenderLightType.Spot:
				WalkSpotLightShadowGeneration( camera, light, outSceneNodes,
					outStaticMeshObjects );
				break;

			case RenderLightType.Point:
				WalkPointLightShadowGeneration( camera, light, outSceneNodes,
					outStaticMeshObjects );
				break;

			case RenderLightType.Directional:
				WalkDirectionalLightShadowGeneration( camera, light, farClipDistance,
					outSceneNodes, outStaticMeshObjects );
				break;
			}
		}

		public static void WalkForCamera( Camera camera, float farClipDistance,
			Set<SceneNode> outSceneNodes, Set<StaticMeshObject> outStaticMeshObjects )
		{
			Frustum frustum = FrustumUtils.GetFrustumByCamera( camera, farClipDistance );

			if( EngineDebugSettings.FrustumTest && camera.AllowFrustumTestMode )
			{
				frustum.HalfWidth *= .5f;
				frustum.HalfHeight *= .5f;
			}

			SceneManager.Instance._SceneGraph.GetObjects( ref frustum, false,
				_SceneObjectGroups.SceneNode | _SceneObjectGroups.StaticMeshObject,
				delegate( _ISceneObject sceneObject )
				{
					_SceneObjectGroups objGroups = sceneObject._SceneData.Groups;

					//StaticMeshObject
					if( ( objGroups & _SceneObjectGroups.StaticMeshObject ) != 0 )
					{
						StaticMeshObject staticMeshObject = (StaticMeshObject)sceneObject;

						if( !staticMeshObject.Visible )
							return;
						outStaticMeshObjects.Add( staticMeshObject );
						return;
					}

					//SceneNode
					if( ( objGroups & _SceneObjectGroups.SceneNode ) != 0 )
					{
						SceneNode sceneNode = (SceneNode)sceneObject;

						if( !sceneNode.Visible )
							return;
						outSceneNodes.Add( sceneNode );
						return;
					}

					Log.Fatal( "InternalSceneManagement: WalkForCamera: invalid sceneObject." );
				} );
		}

	}

}
