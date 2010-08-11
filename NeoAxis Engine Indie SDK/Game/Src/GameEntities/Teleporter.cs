// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Teleporter"/> entity type.
	/// </summary>
	public class TeleporterType : MapObjectType
	{
	}

	/// <summary>
	/// Defines the teleporter for transfering objects.
	/// </summary>
	public class Teleporter : MapObject
	{
		[FieldSerialize]
		bool active = true;
		[FieldSerialize]
		Teleporter destination;

		const float regionDepth = 2.0f;
		Region region;

		TeleporterType _type = null; public new TeleporterType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets a value indicating whether the teleporter is currently active. 
		/// </summary>
		[Description( "A value indicating whether the teleporter is currently active." )]
		[DefaultValue( true )]
		public bool Active
		{
			get { return active; }
			set
			{
				active = value;
				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					UpdateRegion();
			}
		}

		/// <summary>
		/// Gets or sets the destination teleporter.
		/// </summary>
		[Description( "The destination teleporter." )]
		public Teleporter Destination
		{
			get { return destination; }
			set
			{
				if( value == this )
					throw new Exception( "To itself to refer it is impossible." );

				if( destination != null )
					RemoveRelationship( destination );
				destination = value;
				if( destination != null )
					AddRelationship( destination );
				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					UpdateRegion();
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				UpdateRegion();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( region != null )
			{
				region.SetDeleted();
				region = null;
			}
			base.OnDestroy();
		}

		void UpdateRegion()
		{
			bool needRegion = active && destination != null;

			if( needRegion && region == null )
			{
				RegionType regionType = (RegionType)EntityTypes.Instance.GetByName( "ManualRegion" );
				if( regionType == null )
				{
					regionType = (RegionType)EntityTypes.Instance.ManualCreateType(
						"ManualRegion",
						EntityTypes.Instance.GetClassInfoByEntityClassName( "Region" ) );
					regionType.NetworkType = EntityNetworkTypes.ServerOnly;
				}
				region = (Region)Entities.Instance.Create( regionType, Parent );

				region.ShapeType = Region.ShapeTypes.Box;
				region.CheckType = Region.CheckTypes.Center;

				UpdateRegionTransform();
				region.AllowSave = false;
				region.EditorSelectable = false;
				if( !EntitySystemWorld.Instance.IsEditor() )
					region.ObjectIn += new Region.ObjectInOutDelegate( region_ObjectIn );
				region.PostCreate();
			}

			if( !needRegion && region != null )
			{
				region.SetDeleted();
				region = null;
			}
		}

		void UpdateRegionTransform()
		{
			region.Position = Position + Rotation * new Vec3( -regionDepth / 2, 0, 0 );
			region.Rotation = Rotation;
			region.Scale = new Vec3( regionDepth, Scale.Y, Scale.Z );
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			if( region != null )
				UpdateRegionTransform();
		}

		void region_ObjectIn( Entity entity, MapObject obj )
		{
			if( !active || destination == null )
				return;
			if( obj == this )
				return;

			Vec3 localOldPosOffset = ( obj.OldPosition - Position ) * Rotation.GetInverse();
			if( localOldPosOffset.X < -.3f )
				return;

			foreach( Body body in obj.PhysicsModel.Bodies )
			{
				body.Rotation = body.Rotation * Rotation.GetInverse() * destination.Rotation;
				body.OldRotation = body.Rotation;

				Vec3 localPosOffset = ( body.Position - Position ) * Rotation.GetInverse();
				localPosOffset.Y = -localPosOffset.Y;
				localPosOffset.X = .3f;

				body.Position = destination.Position + localPosOffset * destination.Rotation;
				body.OldPosition = body.Position;

				Vec3 vel = body.LinearVelocity * Rotation.GetInverse();
				vel.X = -vel.X;
				vel.Y = -vel.Y;
				vel *= destination.Rotation;
				body.LinearVelocity = vel;

				vel = body.AngularVelocity * Rotation.GetInverse();
				vel.X = -vel.X;
				vel.Y = -vel.Y;
				vel *= destination.Rotation;
				body.AngularVelocity = vel;
			}

			Unit unit = obj as Unit;

			if( unit != null )
			{
				PlayerIntellect playerIntellect = unit.Intellect as PlayerIntellect;
				if( playerIntellect != null )
				{
					Vec3 vec = playerIntellect.LookDirection.GetVector();

					Vec3 v = vec * Rotation.GetInverse();
					v.X = -v.X;
					v.Y = -v.Y;
					v *= destination.Rotation;
					playerIntellect.LookDirection = SphereDir.FromVector( v );
				}
			}

			//!!!!!!need check telefrag
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );
			if( entity == destination )
			{
				destination = null;

				if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
					UpdateRegion();
			}
		}
	}
}
