// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Door"/> entity type.
	/// </summary>
	public class DoorType : DynamicType
	{
		[FieldSerialize]
		Vec3 openDoorBodyOffset = new Vec3( 0, 0, 1 );

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float openTime = 1.0f;

		[FieldSerialize]
		string soundOpen;
		[FieldSerialize]
		string soundClose;

		//

		/// <summary>
		/// Gets or sets the displacement a position of a body "door" when the door is open.
		/// </summary>
		[Description( "The displacement a position of a body \"door\" when the door is open." )]
		[DefaultValue( typeof( Vec3 ), "0 0 1" )]
		public Vec3 OpenDoorBodyOffset
		{
			get { return openDoorBodyOffset; }
			set { openDoorBodyOffset = value; }
		}

		/// <summary>
		/// Gets or set the time of opening/closing of a door.
		/// </summary>
		[Description( "The time of opening/closing of a door." )]
		[DefaultValue( 1.0f )]
		public float OpenTime
		{
			get { return openTime; }
			set { openTime = value; }
		}

		/// <summary>
		/// Gets or sets the sound at opening a door.
		/// </summary>
		[Description( "The sound at opening a door." )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundOpen
		{
			get { return soundOpen; }
			set { soundOpen = value; }
		}

		/// <summary>
		/// Gets or sets the sound at closing a door.
		/// </summary>
		[Description( "The sound at closing a door." )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundClose
		{
			get { return soundClose; }
			set { soundClose = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			if( !string.IsNullOrEmpty( SoundOpen ) )
				SoundWorld.Instance.SoundCreate( SoundOpen, SoundMode.Mode3D );
			if( !string.IsNullOrEmpty( SoundClose ) )
				SoundWorld.Instance.SoundCreate( SoundClose, SoundMode.Mode3D );
		}
	}

	/// <summary>
	/// Defines the doors. That doors worked, it is necessary that the physical 
	/// model had a body with a name "door". This body will move at change of a status of a door.
	/// </summary>
	public class Door : Dynamic
	{
		[FieldSerialize]
		bool opened;

		[FieldSerialize]
		bool needOpen;

		[FieldSerialize]
		float openDoorOffsetCoefficient;

		Vec3 doorBodyInitPosition;
		Body doorBody;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			OpenedToClient,
			SoundOpenToClient,
			SoundCloseToClient,
		}

		///////////////////////////////////////////

		DoorType _type = null; public new DoorType Type { get { return _type; } }

		protected override void OnCreate()
		{
			base.OnCreate();
			needOpen = opened;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();

			//init doorBodyInitPosition and doorBody
			if( PhysicsModel != null )
			{
				for( int n = 0; n < PhysicsModel.Bodies.Length; n++ )
				{
					if( PhysicsModel.Bodies[ n ].Name == "door" )
					{
						Mat4 transform = PhysicsModel.ModelDeclaration.Bodies[ n ].GetTransform();
						doorBodyInitPosition = transform.Item3.ToVec3();
						break;
					}
				}

				doorBody = PhysicsModel.GetBody( "door" );
			}

			UpdateDoorBody();
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			UpdateDoorBody();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( needOpen != opened || openDoorOffsetCoefficient != 0 )
			{
				float offset = TickDelta / Type.OpenTime;

				bool oldOpened = opened;

				if( needOpen )
				{
					openDoorOffsetCoefficient += offset;
					if( openDoorOffsetCoefficient >= 1 )
					{
						openDoorOffsetCoefficient = 1;
						opened = needOpen;
					}
				}
				else
				{
					openDoorOffsetCoefficient -= offset;
					if( openDoorOffsetCoefficient <= 0 )
					{
						openDoorOffsetCoefficient = 0;
						opened = needOpen;
					}
				}

				if( oldOpened != opened )
				{
					if( EntitySystemWorld.Instance.IsServer() )
					{
						if( Type.NetworkType == EntityNetworkTypes.Synchronized )
							Server_SendOpenedToClients( EntitySystemWorld.Instance.RemoteEntityWorlds );
					}
				}
			}

			UpdateDoorBody();
		}

		void UpdateDoorBody()
		{
			if( doorBody == null )
				return;

			//update body
			Vec3 pos = Position + doorBodyInitPosition +
				Type.OpenDoorBodyOffset * openDoorOffsetCoefficient;
			Vec3 oldPosition = doorBody.Position;
			doorBody.Position = pos;
			doorBody.OldPosition = oldPosition;

			if( EntitySystemWorld.Instance.IsServer() &&
				Type.NetworkType == EntityNetworkTypes.Synchronized )
			{
				Server_SendBodiesPositionsToAllClients( false );
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the door is currently open.
		/// </summary>
		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		virtual public bool Opened
		{
			get { return opened; }
			set
			{
				if( needOpen == value )
					return;

				needOpen = value;

				if( EntitySystemWorld.Instance.IsEditor() )
				{
					opened = value;
					openDoorOffsetCoefficient = opened ? 1 : 0;
					UpdateDoorBody();
				}
				else
				{
					if( needOpen )
					{
						SoundPlay3D( Type.SoundOpen, .5f, false );

						//send message to client
						if( EntitySystemWorld.Instance.IsServer() )
						{
							if( Type.NetworkType == EntityNetworkTypes.Synchronized )
								Server_SendSoundOpenToAllClients();
						}
					}
					else
					{
						SoundPlay3D( Type.SoundClose, .5f, false );

						//send message to client
						if( EntitySystemWorld.Instance.IsServer() )
						{
							if( Type.NetworkType == EntityNetworkTypes.Synchronized )
								Server_SendSoundCloseToAllClients();
						}
					}
				}
			}
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendOpenedToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendOpenedToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Door ),
				(ushort)NetworkMessages.OpenedToClient );
			writer.Write( opened );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.OpenedToClient )]
		void Client_ReceiveOpened( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			bool value = reader.ReadBoolean();
			if( !reader.Complete() )
				return;
			opened = value;
		}

		void Server_SendSoundOpenToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Door ),
				(ushort)NetworkMessages.SoundOpenToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SoundOpenToClient )]
		void Client_ReceiveSoundOpen( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			SoundPlay3D( Type.SoundOpen, .5f, false );
		}

		void Server_SendSoundCloseToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Door ),
				(ushort)NetworkMessages.SoundCloseToClient );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.SoundCloseToClient )]
		void Client_ReceiveSoundClose( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			if( !reader.Complete() )
				return;
			SoundPlay3D( Type.SoundClose, .5f, false );
		}

	}
}
