using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;
using Engine.MathEx;

namespace GameEntities
{
    /// <summary>
    /// Defines the <see cref="Darkness"/> entity type.
    /// </summary>
    public class DarknessType : MapGeneralObjectType
    {
		public DarknessType()
		{
			UniqueEntityInstance = true;
			AllowEmptyName = true;
		}
    }

    public class Darkness : MapGeneralObject
    {
        [FieldSerialize]
        Vec2i mapDimension = new Vec2i(300, 300);

        [DefaultValue(typeof(Vec2i), "300 300")]
        public Vec2i MapDimension
        {
            get { return mapDimension; }
            set { mapDimension = value; }
        }

        [FieldSerialize]
        Vec3i tileSize = new Vec3i(10, 10, 9);

        [DefaultValue(typeof(Vec3i), "10 10 9")]
        public Vec3i TileSize
        {
            get { return tileSize; }
            set { tileSize = value; }
        }

        [FieldSerialize]
        int tileElevation = 8;

        [DefaultValue(typeof(int), "8")]
        public int TileElevation
        {
            get { return tileElevation; }
            set { tileElevation = value; }
        }

        List<MapObject> tiles = new List<MapObject>();

        // A field needed by the resource editor
        DarknessType _type = null; public new DarknessType Type { get { return _type; } }

        static Darkness instance;

		public static Darkness Instance
		{
			get { return instance; }
		}

		public Darkness()
		{
			if( instance != null )
				Log.Fatal( "Darkness: instance != null" );
			instance = this;
		}

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            if (instance == this)//for undo support
                instance = this;

            base.OnPostCreate(loaded);
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (instance == this)//for undo support
                instance = null;
        }

        public void cover()
        {
            for (int i = 0; i < MapDimension.X; i += TileSize.X)
            {
                for (int j = 0; j < MapDimension.Y; j += TileSize.Y)
                {
                    MapObject obj = (MapObject)Entities.Instance.Create("darkness_tile", Map.Instance);
                    //obj.Position = new Vec3(i, j, 5);
                    int x = i - (MapDimension.X/2);
                    int y = j - (MapDimension.Y/2);
                    obj.Scale = new Vec3( TileSize.X + 5, TileSize.Y + 5, TileSize.Z);
                    obj.Position = new Vec3(x, y, tileElevation);
                    obj.PostCreate();

                    MapObject obj2 = new MapObject();
                    obj2 = obj;

                    tiles.Add(obj2);
                }
            }
        }

        public void ClearMapPosition(float x, float y, int type)
        {            
            // Validate the tiles vector
            if (tiles.Count != 0)
            {
                float xHalf = ((float)MapDimension.X / (float)2.0 );
                float yHalf = ((float)MapDimension.Y / (float)2.0 );

                // Check x and y are in the right range
                if ( (x > -xHalf) && (x < xHalf) &&
                     (y > -yHalf) && (y < yHalf) )
                {
                    float x1 = (float)(x + xHalf) / (float)TileSize.X;
                    float y1 = (float)(y + yHalf) / (float)TileSize.Y;

                    int x1_round = (int)Math.Round(x1, 0, MidpointRounding.ToEven);
                    int y1_round = (int)Math.Round(y1, 0, MidpointRounding.ToEven);

                    float size = (float)MapDimension.X / (float)TileSize.X;
                    int size_round = (int)Math.Round(size, 0, MidpointRounding.ToEven);

                    int pos = size_round * x1_round + y1_round;
                    if ((pos >= 0) && (pos < tiles.Count))
                    {
                        MapObject obj = tiles[pos];
                        obj.Visible = false;
                    }

                    if (type == 1)// Buildings
                    {
                        // Top
                        int pos_left_top = size_round * (x1_round - 1) + (y1_round + 1);
                        if ((pos_left_top >= 0) && (pos_left_top < tiles.Count))
                        {
                            MapObject obj = tiles[pos_left_top];
                            obj.Visible = false;
                        }

                        int pos_top = size_round * x1_round + (y1_round + 1);
                        if ((pos_top >= 0) && (pos_top < tiles.Count))
                        {
                            MapObject obj = tiles[pos_top];
                            obj.Visible = false;
                        }

                        int pos_right_top = size_round * (x1_round + 1) + (y1_round + 1);
                        if ((pos_right_top >= 0) && (pos_right_top < tiles.Count))
                        {
                            MapObject obj = tiles[pos_right_top];
                            obj.Visible = false;
                        }

                        // Middle
                        int pos_left = size_round * (x1_round - 1) + y1_round;
                        if ((pos_left >= 0) && (pos_left < tiles.Count))
                        {
                            MapObject obj = tiles[pos_left];
                            obj.Visible = false;
                        }

                        int pos_right = size_round * (x1_round + 1) + y1_round;
                        if ((pos_right >= 0) && (pos_right < tiles.Count))
                        {
                            MapObject obj = tiles[pos_right];
                            obj.Visible = false;
                        }

                        // Bottom
                        int pos_left_bottom = size_round * (x1_round - 1) + (y1_round - 1);
                        if ((pos_left_bottom >= 0) && (pos_left_bottom < tiles.Count))
                        {
                            MapObject obj = tiles[pos_left_bottom];
                            obj.Visible = false;
                        }

                        int pos_bottom = size_round * x1_round + (y1_round - 1);
                        if ((pos_bottom >= 0) && (pos_bottom < tiles.Count))
                        {
                            MapObject obj = tiles[pos_bottom];
                            obj.Visible = false;
                        }

                        int pos_right_bottom = size_round * (x1_round + 1) + (y1_round - 1);
                        if ((pos_right_bottom >= 0) && (pos_right_bottom < tiles.Count))
                        {
                            MapObject obj = tiles[pos_right_bottom];
                            obj.Visible = false;
                        }
                    }
                }
            }
        }
    }
}