// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="MapChangeRegion"/> entity type.
	/// </summary>
	public class MapChangeRegionType : RegionType
	{
	}

	/// <summary>
	/// Gives an opportunity of moving of the player between maps. 
	/// When the player gets in this region game loads a new map.
	/// </summary>
	public class MapChangeRegion : Region
	{
		[FieldSerialize]
		string mapName;
		[FieldSerialize]
		string spawnPointName;

		//

		MapChangeRegionType _type = null; public new MapChangeRegionType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets the name of a map for loading.
		/// </summary>
		[Description( "Name of a map for loading." )]
		public string MapName
		{
			get { return mapName; }
			set { mapName = value; }
		}

		/// <summary>
		/// Gets or set the name of a spawn point in the destination map.
		/// </summary>
		[Description( "The name of a spawn point in the destination map." )]
		public string SpawnPointName
		{
			get { return spawnPointName; }
			set { spawnPointName = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			ObjectIn += new ObjectInOutDelegate( MapChangeRegion_ObjectIn );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			ObjectIn -= new ObjectInOutDelegate( MapChangeRegion_ObjectIn );
			base.OnDestroy();
		}

		void MapChangeRegion_ObjectIn( Entity entity, MapObject obj )
		{
			if( PlayerIntellect.Instance != null && PlayerIntellect.Instance.ControlledObject == obj )
			{
				if( EntitySystemWorld.Instance.IsServer() )
				{
					Log.Warning( "MapChangeRegion: Networking mode is not supported." );
					return;
				}

				PlayerCharacter character = (PlayerCharacter)PlayerIntellect.Instance.ControlledObject;
				PlayerCharacter.ChangeMapInformation playerCharacterInformation =
					character.GetChangeMapInformation( this );
				GameWorld.Instance.SetShouldChangeMap( mapName, spawnPointName,
					playerCharacterInformation );
			}
		}
	}
}
