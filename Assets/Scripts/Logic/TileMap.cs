using SafeCoroutine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicTower.Logic
{
    /// <summary>
    /// Tile map.
    /// </summary>
    public class TileMap
    {
        public Display.ITileMapDisplay Display { get; private set; }

        public Data.TileMapData Data { get; private set; }

        /// <summary>
        /// 地图宽度
        /// </summary>
        public uint Width
        {
            get { return Data.Width; }
        }

        /// <summary>
        /// 地图高度
        /// </summary>
        public uint Height
        {
            get { return Data.Height; }
        }

        /// <summary>
        /// 地板层
        /// </summary>
        public TileLayer LayerFloor { get; private set; }

        /// <summary>
        /// 碰撞层
        /// </summary>
        public TileLayer LayerCollide { get; private set; }

        public void Init(Data.TileMapData data)
        {
            Data = data;

            LayerFloor = new TileLayer();
            LayerFloor.Init(this, Width, Height);

            LayerCollide = new TileLayer();
            LayerCollide.Init(this, Width, Height);
        }

        public IEnumerator LoadFromData()
        {
            var floor_layer_data = Data.FloorLayer;
            for (int row = 0; row < floor_layer_data.GetLength(0); ++row)
            {
                for (int col = 0; col < floor_layer_data.GetLength(1); ++col)
                {
                    var tile_type = floor_layer_data[row, col];
                    var tile = TileFactory.Instance.CreateTile(tile_type);
                    if (tile != null)
                    {
                        LayerFloor[(uint)(row), (uint)col] = tile;
                    }
                }
            }

            var portals = Data.PortalDatas;
            if (portals != null)
            {
                foreach (var portal in portals)
                {
                    var portal_tile = TileFactory.Instance.CreatePortal(portal);
                    LayerCollide[portal.Pos] = portal_tile;
                }
            }

            var monsters = Data.MonsterDatas;
            if (monsters != null)
            {
                foreach (var monster in monsters)
                {
                    var monster_tile = TileFactory.Instance.CreateMonster(monster);
                    LayerCollide[monster.Pos] = monster_tile;
                }
            }

            yield return null;
        }

        public IEnumerator SaveToData()
        {
            foreach (Tile tile in LayerFloor)
            {
                if (tile != null)
                {
                    Data.FloorLayer[tile.Position.Row, tile.Position.Col] = tile.Type;
                }
            }

            var monsters = new List<Data.MonsterData>();
            var portals = new List<Data.PortalData>();
            foreach (Tile tile in LayerCollide)
            {
                if (tile != null)
                {
                    switch (tile.Type)
                    {
                        case Tile.EType.Monster:
                            {
                                var monster_data = new Data.MonsterData();
                                monster_data.Pos = tile.Position;
                                monster_data.Id = (tile as Tile_Monster).Id;

                                monsters.Add(monster_data);
                                break;
                            }
                        case Tile.EType.Portal:
                            {
                                var portal_data = new Data.PortalData();
                                portal_data.Pos = tile.Position;
								portal_data.DestinationLevel = (tile as Tile_Portal).DestinationLevel;
                                portal_data.DestinationPosition = (tile as Tile_Portal).DestinationPosition;

								portals.Add(portal_data);
                                break;
                            }
                    }
                }
            }

            Data.MonsterDatas = monsters;
			Data.PortalDatas = portals;

            yield return null;
        }

        public IEnumerator Enter()
        {
            if (Display == null)
            {
                Display = Game.Instance.DisplayFactory.GetTileMapDisplay(this);
            }

            yield return Display.BeginEnter();

            foreach (Tile tile in LayerFloor)
            {
                if (tile != null)
                {
                    tile.Parent = this;
                    tile.Layer = LayerFloor;
                    yield return tile.Enter();
                }
            }

            foreach (Tile tile in LayerCollide)
            {
                if (tile != null)
                {
                    tile.Parent = this;
                    tile.Layer = LayerCollide;
                    yield return tile.Enter();
                }
            }

            yield return Display.EndEnter();
        }

        public IEnumerator Exit()
        {
            yield return Display.BeginExit();

            foreach (Tile tile in LayerFloor)
            {
                if (tile != null)
                {
                    yield return tile.Exit();
                }
            }

            foreach (Tile tile in LayerCollide)
            {
                if (tile != null)
                {
                    yield return tile.Exit();
                }
            }

            yield return Display.EndExit();
        }
    }
}