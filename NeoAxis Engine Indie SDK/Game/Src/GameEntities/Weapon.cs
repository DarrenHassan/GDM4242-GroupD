// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.MathEx;
using Engine.Utils;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.SoundSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Weapon"/> entity type.
	/// </summary>
	public abstract class WeaponType : DynamicType
	{
		public abstract class WeaponMode
		{
			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float betweenFireTime = 0;

			[FieldSerialize]
			string soundFire;

			[FieldSerialize]
			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			Vec3 startOffsetPosition;

			[FieldSerialize]
			[DefaultValue( typeof( Quat ), "0 0 0 1" )]
			Quat startOffsetRotation = Quat.Identity;

			[FieldSerialize]
			[DefaultValue( typeof( Vec2 ), "0 0" )]
			Range useDistanceRange;

			[FieldSerialize]
			List<float> fireTimes = new List<float>();

			[FieldSerialize]
			string fireAnimationName = "fire";

			[FieldSerialize]
			string fireUnitAnimationName = "";

			[DefaultValue( 0.0f )]
			public float BetweenFireTime
			{
				get { return betweenFireTime; }
				set { betweenFireTime = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundFire
			{
				get { return soundFire; }
				set { soundFire = value; }
			}

			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			public Vec3 StartOffsetPosition
			{
				get { return startOffsetPosition; }
				set { startOffsetPosition = value; }
			}

			[DefaultValue( typeof( Quat ), "0 0 0" )]
			public Quat StartOffsetRotation
			{
				get { return startOffsetRotation; }
				set { startOffsetRotation = value; }
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			public Range UseDistanceRange
			{
				get { return useDistanceRange; }
				set { useDistanceRange = value; }
			}

			public List<float> FireTimes
			{
				get { return fireTimes; }
			}

			[DefaultValue( "fire" )]
			public string FireAnimationName
			{
				get { return fireAnimationName; }
				set { fireAnimationName = value; }
			}

			public string FireUnitAnimationName
			{
				get { return fireUnitAnimationName; }
				set { fireUnitAnimationName = value; }
			}

			[Browsable( false )]
			public abstract bool IsInitialized
			{
				get;
			}
		}

		[FieldSerialize]
		string fpsMeshMaterialName = "";

		protected WeaponMode weaponNormalMode;
		protected WeaponMode weaponAlternativeMode;

		[FieldSerialize]
		string boneSlot = "";

		[FieldSerialize]
		[DefaultValue( "idle" )]
		string idleAnimationName = "idle";

		//

		[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
		public string FPSMeshMaterialName
		{
			get { return fpsMeshMaterialName; }
			set { fpsMeshMaterialName = value; }
		}

		[Browsable( false )]
		public WeaponMode WeaponNormalMode
		{
			get { return weaponNormalMode; }
		}

		[Browsable( false )]
		public WeaponMode WeaponAlternativeMode
		{
			get { return weaponAlternativeMode; }
		}

		public string BoneSlot
		{
			get { return boneSlot; }
			set { boneSlot = value; }
		}

		[DefaultValue( "idle" )]
		public string IdleAnimationName
		{
			get { return idleAnimationName; }
			set { idleAnimationName = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			//it is not known how will be used this sound (2D or 3D?).
			//Sound will preloaded as 3D only here.
			if( !string.IsNullOrEmpty( weaponNormalMode.SoundFire ) )
				SoundWorld.Instance.SoundCreate( weaponNormalMode.SoundFire, SoundMode.Mode3D );
			if( !string.IsNullOrEmpty( weaponAlternativeMode.SoundFire ) )
				SoundWorld.Instance.SoundCreate( weaponAlternativeMode.SoundFire, SoundMode.Mode3D );
		}
	}

	/// <summary>
	/// Defines the weapons. Both hand-held by characters or guns established on turret are weapons.
	/// </summary>
	public abstract class Weapon : Dynamic
	{
		MapObjectAttachedMesh mainMeshObject;
		bool fpsMeshMaterialNameEnabled;

		bool setForceFireRotation;
		Quat forceFireRotation;

		//

		WeaponType _type = null; public new WeaponType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			//get mainMeshObject
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMeshObject = attachedObject as MapObjectAttachedMesh;
				if( attachedMeshObject != null )
				{
					if( mainMeshObject == null )
						mainMeshObject = attachedMeshObject;
				}
			}

			if( fpsMeshMaterialNameEnabled && !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
				UpdateMainMeshObjectMaterial();

			AddTimer();
		}

		[Browsable( false )]
		abstract public bool Ready
		{
			get;
		}

		public delegate void PreFireDelegate( Weapon entity, bool alternative );
		public event PreFireDelegate PreFire;

		protected void DoPreFireEvent( bool alternative )
		{
			if( PreFire != null )
				PreFire( this, alternative );
		}

		public abstract bool TryFire( bool alternative );

		public void SetForceFireRotationLookTo( Vec3 lookTo )
		{
			setForceFireRotation = true;

			Quat rot;
			{
				Vec3 diff = lookTo - GetFirePosition( false );

				float dirh = MathFunctions.ATan( diff.Y, diff.X );
				float dirv = -MathFunctions.ATan( diff.Z, diff.ToVec2().LengthFast() );
				float halfdirh = dirh * .5f;
				rot = new Quat( new Vec3( 0, 0, MathFunctions.Sin( halfdirh ) ),
					MathFunctions.Cos( halfdirh ) );
				float halfdirv = dirv * .5f;
				rot *= new Quat( 0, MathFunctions.Sin( halfdirv ), 0, MathFunctions.Cos( halfdirv ) );
			}
			forceFireRotation = rot;
		}

		public void ResetForceFireRotationLookTo()
		{
			setForceFireRotation = false;
		}

		public abstract Quat GetFireRotation( bool alternative );

		protected Quat GetFireRotation( WeaponType.WeaponMode typeMode )
		{
			return ( setForceFireRotation ? forceFireRotation : Rotation ) * typeMode.StartOffsetRotation;
		}

		public abstract Vec3 GetFirePosition( bool alternative );

		protected Vec3 GetFirePosition( WeaponType.WeaponMode typeMode )
		{
			return Position + Rotation * typeMode.StartOffsetPosition;
		}

		protected override void OnUpdateBaseAnimation()
		{
			base.OnUpdateBaseAnimation();

			//idle animation
			{
				UpdateBaseAnimation( Type.IdleAnimationName, true, true, 1 );
				return;
			}
		}

		[Browsable( false )]
		public bool FPSMeshMaterialNameEnabled
		{
			get { return fpsMeshMaterialNameEnabled; }
			set
			{
				if( fpsMeshMaterialNameEnabled == value )
					return;
				fpsMeshMaterialNameEnabled = value;

				if( !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
					UpdateMainMeshObjectMaterial();
			}
		}

		void UpdateMainMeshObjectMaterial()
		{
			if( mainMeshObject == null )
				return;

			MeshObject meshObject = mainMeshObject.MeshObject;
			if( meshObject == null )
				return;

			//FPSMeshMaterialName
			if( fpsMeshMaterialNameEnabled && !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
			{
				meshObject.SetMaterialNameForAllSubObjects( Type.FPSMeshMaterialName );
				return;
			}

			//default materials
			Mesh mesh = meshObject.Mesh;
			for( int n = 0; n < meshObject.SubObjects.Length; n++ )
			{
				if( n < mesh.SubMeshes.Length )
					meshObject.SubObjects[ n ].MaterialName = mesh.SubMeshes[ n ].MaterialName;
			}
		}
	}
}
