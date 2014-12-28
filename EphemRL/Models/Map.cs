using EphemRL.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtiliCS;

namespace EphemRL.Models
{
    public class Map : ViewModel
    {
        /// <summary>
        /// Collection of tiles for display purposes, and as a backing store. DO NOT touch this directly from
        /// outside of Map. Use Get...() and Set...() and Chage...() methods to interact with map contents!
        /// </summary>
        public ObservableCollection<MapTile> Tiles { get; private set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int PixelWidth { get { return Width * MapTile.TileSize; } }
        public int PixelHeight { get { return Height * MapTile.TileSize; } }

        public List<TerrainProto> TerrainTypes { get; set; }
        private Dictionary<string, TerrainProto> TerrainTypesByName { get; set; }

        private Dictionary<Actor, MapTile> ActorPlaces { get; set; }

        public IEnumerable<Actor> Actors { get { return ActorPlaces.Keys; } }

        public Map(List<TerrainProto> terrainTypes) 
        {
            TerrainTypes = terrainTypes;
            TerrainTypesByName = terrainTypes.ToDictionary(tt => tt.Name);

            Width = Height = 20;
            Tiles = new ObservableCollection<MapTile>();
            Tiles.AddRange(0.To(Width * Height).Select(i => new MapTile(i % Width, i / Width, TerrainTypes.Choose())));

            ActorPlaces = new Dictionary<Actor, MapTile>();
        }

        public IEnumerable<MapTile> WalkTilesInRangeOf(MapTile origin, int range)
        {
            var frontier = new Queue<MapTile>();
            frontier.Enqueue(origin);
            var visitedTiles = new HashSet<MapTile>();

            while (frontier.Any())
            {
                var cur = frontier.Dequeue();

                if (visitedTiles.Contains(cur)) continue;

                visitedTiles.Add(cur);

                yield return cur;

                GetAdjacentTiles(cur).Where(t => !visitedTiles.Contains(t) 
                                              && range >= t.DistanceTo(origin))
                                     .Do(frontier.Enqueue);
            }
            
        }

        public void SpawnActor(Actor a)
        {
            var tile = Tiles.Where(t => t.Proto.IsPassable).Choose();

            tile.Actor = a;
            ActorPlaces.Add(a, tile);
        }

        public MapTile GetActorTile(Actor actor)
        {
            return ActorPlaces[actor];
        }

        public void MoveActorBy(Actor actor, int dx, int dy)
        {
            var t = ActorPlaces[actor];

            var x = t.X + dx;
            var y = t.Y + dy;

            MoveActorTo(actor, x, y);
        }

        public void MoveActorTo(Actor actor, MapTile t)
        {
            MoveActorTo(actor, t.X, t.Y);
        }

        public void MoveActorTo(Actor actor, int x, int y)
        {
            x = x.Clamp(0, Width - 1);
            y = y.Clamp(0, Height - 1);

            var targetTile = GetTileAt(x, y);
            if (targetTile.Proto.IsPassable)
            {
                ActorPlaces[actor].Actor = null;
                ActorPlaces[actor] = targetTile;

                targetTile.Actor = actor;
                if (targetTile.Proto.Name == "Lava") actor.Health--;
                Debug.WriteLine("{0} moved to {1}, {2}", actor.Proto.Name, targetTile.X, targetTile.Y);
            }
            else
            {
                Debug.WriteLine("{0} move to {1}, {2} cancelled due to impassability.", actor.Proto.Name, targetTile.X, targetTile.Y);
            }
        }

        public MapTile GetTileAt(int x, int y)
        {
            return Tiles[Linearize(x, y)];
        }

        /// <summary>
        /// Replaces the existing tile at x, y with newTile, transferring no state aside from position.
        /// </summary>
        public void SetTileAt(int x, int y, MapTile newTile)
        {
            var tileIndex = Linearize(x, y);

            Tiles.RemoveAt(tileIndex);
            Tiles.Insert(tileIndex, newTile);
        }

        private int Linearize(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return y * Width + x;
            } 
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Coordinates were outside of map: ({0}, {1})", x, y));
            }
        }

        public void ChangeTerrainAt(int x, int y, string terrainTypeName)
        {
            ReplaceTileAt(x, y, new MapTile(x, y, TerrainTypesByName[terrainTypeName]));
        }

        public void ChangeTerrainOf(MapTile tile, string terrainTypeName)
        {
            ReplaceTileAt(tile.X, tile.Y, new MapTile(tile.X, tile.Y, TerrainTypesByName[terrainTypeName]));
        }

        /// <summary>
        /// Transfers any relevant state (like items and actors) from the old tile to the newTile at x, y.
        /// </summary>
        public void ReplaceTileAt(int x, int y, MapTile newTile)
        {
            var oldTile = GetTileAt(x, y);

            SetTileAt(x, y, newTile);

            if (oldTile.Actor != null) 
            {
                MoveActorTo(oldTile.Actor, x, y);
            }
        }

        public void SetTileVisibilityForLineOfSight(Actor a)
        {
            Tiles.Do(t => t.IsHidden = true);
            a.VisibleTiles.Do(t => t.IsHidden = false);
        }

        public static List<Tuple<int, int>> AdjacencyDeltas =  new List<Tuple<int, int>>
        {
            Tuple.Create(1, 0), Tuple.Create(1, 1), Tuple.Create(0, 1), 
            Tuple.Create(-1, 0), Tuple.Create(-1, -1), Tuple.Create(0, -1),
            Tuple.Create(-1, 1), Tuple.Create(1, -1)
        };

        public IEnumerable<MapTile> GetAdjacentTiles(Actor a)
        {
            return GetAdjacentTiles(GetActorTile(a));
        }

        public IEnumerable<MapTile> GetAdjacentTiles(MapTile t)
        {
            foreach (var delta in AdjacencyDeltas)
            {
                int newX = t.X + delta.Item1, newY = t.Y + delta.Item2;

                if (newX >= 0 && newX < Width && newY >= 0 && newY < Height)
                {
                    yield return GetTileAt(newX, newY);
                }
            }
        }

        public void RegenerateMana(ManaElement e)
        {
            Tiles.Do(t => t.Mana.Regenerate(e));
        }

        public void ExpendManaForSpell(SpellCastDelta delta, MapTile targetTile)
        {
            delta.ValidTargets[targetTile].Delta.Do((tile, mana) => tile.Mana.Expend(mana));
        }
    }
}