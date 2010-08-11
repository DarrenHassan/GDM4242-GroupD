// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.EntitySystem;

namespace GameCommon
{
	public class MeshObjectAnimationController
	{
		MeshObject meshObject;
		float blendingTime;

		List<AnimationItem> activeItems = new List<AnimationItem>();
		ReadOnlyCollection<AnimationItem> activeItemsAsReadOnly;
		List<AnimationItem> removedItemsForBlending = new List<AnimationItem>();

		//key: animation name; value: maximum index (walk, walk2, walk3)
		Dictionary<string, int> maxAnimationIndices = new Dictionary<string, int>();

		///////////////////////////////////////////

		public sealed class AnimationItem
		{
			MeshObjectAnimationController owner;
			string animationBaseName;
			bool allowRandomAnimationNumber;
			bool loop;

			internal bool removed;

			float velocity = 1;
			float weight = 1;

			internal MeshObject.AnimationState animationState;
			internal float lastTimePosition;

			internal float blendingWeightCoefficient;

			//

			internal AnimationItem( MeshObjectAnimationController owner, string animationBaseName,
				bool allowRandomAnimationNumber, bool loop )
			{
				this.owner = owner;
				this.animationBaseName = animationBaseName;
				this.allowRandomAnimationNumber = allowRandomAnimationNumber;
				this.loop = loop;
			}

			public string AnimationBaseName
			{
				get { return animationBaseName; }
			}

			public bool AllowRandomAnimationNumber
			{
				get { return allowRandomAnimationNumber; }
			}

			public bool Loop
			{
				get { return loop; }
			}

			public float Velocity
			{
				get { return velocity; }
				set { velocity = value; }
			}

			public float Weight
			{
				get { return weight; }
				set
				{
					weight = value;
					owner.UpdateAnimationStatesWeights();
				}
			}

			public float TimePosition
			{
				get
				{
					if( animationState == null )
						return 0;
					return animationState.TimePosition;
				}
				set
				{
					if( animationState != null )
						animationState.TimePosition = value;
				}
			}

			public bool Removed
			{
				get { return removed; }
			}

			public float Length
			{
				get { return animationState.Length; }
			}
		}

		///////////////////////////////////////////

		public MeshObjectAnimationController( MeshObject meshObject, float blendingTime )
		{
			activeItemsAsReadOnly = new ReadOnlyCollection<AnimationItem>( activeItems );

			this.meshObject = meshObject;
			this.blendingTime = blendingTime;
		}

		public MeshObject MeshObject
		{
			get { return meshObject; }
		}

		public AnimationItem Add( string animationBaseName, bool allowRandomAnimationNumber,
			bool loop )
		{
			//remove from removedItemsForBlending
			if( blendingTime != 0 )
			{
				for( int n = 0; n < removedItemsForBlending.Count; n++ )
				{
					AnimationItem removedItem = removedItemsForBlending[ n ];
					if( removedItem.AnimationBaseName == animationBaseName )
					{
						removedItem.animationState.Enable = false;
						removedItemsForBlending.RemoveAt( n );
						n--;
						continue;
					}
				}
			}

			string animationName = animationBaseName;
			if( allowRandomAnimationNumber )
			{
				int number = GetRandomAnimationNumber( animationBaseName, true );
				if( number != 1 )
					animationName += number.ToString();
			}

			MeshObject.AnimationState animationState = meshObject.GetAnimationState( animationName );
			if( animationState == null )
				return null;

			animationState.Loop = loop;
			animationState.TimePosition = 0;
			animationState.Enable = true;

			AnimationItem item = new AnimationItem( this, animationBaseName,
				allowRandomAnimationNumber, loop );

			if( blendingTime != 0 )
				item.blendingWeightCoefficient = .001f;
			else
				item.blendingWeightCoefficient = 1;

			item.animationState = animationState;

			activeItems.Add( item );

			UpdateAnimationStatesWeights();

			return item;
		}

		public void Remove( AnimationItem item )
		{
			if( item.removed )
				return;

			if( blendingTime == 0 )
				item.animationState.Enable = false;

			item.removed = true;
			activeItems.Remove( item );

			if( blendingTime != 0 )
				removedItemsForBlending.Add( item );

			UpdateAnimationStatesWeights();
		}

		public void RemoveAll()
		{
			while( activeItems.Count != 0 )
				Remove( activeItems[ activeItems.Count - 1 ] );
		}

		public void DoRenderFrame()
		{
			float delta = RendererWorld.Instance.FrameRenderTimeStep;

			for( int n = 0; n < activeItems.Count; n++ )
			{
				AnimationItem item = activeItems[ n ];

				MeshObject.AnimationState animationState = item.animationState;

				//time progress
				animationState.AddTime( item.Velocity * delta );

				//has ended?
				if( !item.Loop )
				{
					if( animationState.TimePosition + blendingTime * 2 + .001f >=
						item.animationState.Length )
					{
						Remove( item );
						n--;
						continue;
					}
				}

				//change random animation
				if( item.Loop && item.AllowRandomAnimationNumber )
				{
					//detect rewind
					if( animationState.TimePosition < item.lastTimePosition )
					{
						string animationName = item.AnimationBaseName;
						int number = GetRandomAnimationNumber( animationName, true );
						if( number != 1 )
							animationName += number.ToString();

						MeshObject.AnimationState newAnimationState =
							meshObject.GetAnimationState( animationName );
						if( newAnimationState == null )
						{
							Log.Fatal( "MeshObjectAnimationController: DoRenderFrame: " +
								"newAnimationState == null." );
						}

						animationState.Enable = false;

						newAnimationState.Loop = animationState.Loop;
						newAnimationState.TimePosition = animationState.TimePosition;
						newAnimationState.Weight = animationState.Weight;
						newAnimationState.Enable = true;

						item.animationState = newAnimationState;
					}
				}

				//update weight for blending
				if( blendingTime != 0 )
				{
					item.blendingWeightCoefficient += delta / blendingTime;
					if( item.blendingWeightCoefficient > 1 )
						item.blendingWeightCoefficient = 1;
				}

				item.lastTimePosition = animationState.TimePosition;
			}

			for( int n = 0; n < removedItemsForBlending.Count; n++ )
			{
				AnimationItem item = removedItemsForBlending[ n ];

				item.blendingWeightCoefficient -= delta / blendingTime;
				if( item.blendingWeightCoefficient <= 0 )
				{
					item.animationState.Enable = false;
					removedItemsForBlending.RemoveAt( n );
					n--;
					continue;
				}
			}

			UpdateAnimationStatesWeights();
		}

		public IList<AnimationItem> Items
		{
			get { return activeItemsAsReadOnly; }
		}

		public int GetRandomAnimationNumber( string animationBaseName,
			bool firstAnimationIn10TimesMoreOften )
		{
			int maxIndex;

			if( !maxAnimationIndices.TryGetValue( animationBaseName, out maxIndex ) )
			{
				//calculate max animation index
				maxIndex = 1;
				for( int n = 2; ; n++ )
				{
					if( meshObject.GetAnimationState( animationBaseName + n.ToString() ) != null )
						maxIndex++;
					else
						break;
				}
				maxAnimationIndices.Add( animationBaseName, maxIndex );
			}

			int number;

			//The first animation in 10 times more often
			if( firstAnimationIn10TimesMoreOften )
			{
				number = World.Instance.Random.Next( 10 + maxIndex ) + 1 - 10;
				if( number < 1 )
					number = 1;
			}
			else
				number = World.Instance.Random.Next( maxIndex ) + 1;

			return number;
		}

		void UpdateAnimationStatesWeights()
		{
			float totalWeight = 0;
			{
				for( int n = 0; n < activeItems.Count; n++ )
				{
					AnimationItem item = activeItems[ n ];
					totalWeight += item.Weight * item.blendingWeightCoefficient;
				}
				for( int n = 0; n < removedItemsForBlending.Count; n++ )
				{
					AnimationItem item = removedItemsForBlending[ n ];
					totalWeight += item.Weight * item.blendingWeightCoefficient;
				}
			}

			float multiplier = 1;
			if( totalWeight > 0 && totalWeight < 1 )
				multiplier = 1.0f / totalWeight;

			//update animation states
			{
				for( int n = 0; n < activeItems.Count; n++ )
				{
					AnimationItem item = activeItems[ n ];
					item.animationState.Weight = item.Weight * item.blendingWeightCoefficient *
						multiplier;
				}
				for( int n = 0; n < removedItemsForBlending.Count; n++ )
				{
					AnimationItem item = removedItemsForBlending[ n ];
					item.animationState.Weight = item.Weight * item.blendingWeightCoefficient *
						multiplier;
				}
			}
		}
	}
}
