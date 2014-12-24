using EphemRL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtiliCS;

namespace EphemRL.Helpers
{
    public static class LineOfSight
    {
        public static IEnumerable<MapTile> Calculate(Map map, MapTile sourceTile) 
        {
            var tilesProcessed = new HashSet<MapTile>();

            var frontier = new UniqueQueue<MapTile>();

            var visibleTiles = new HashSet<MapTile>();

            int sightRange = sourceTile.Actor != null ? sourceTile.Actor.Proto.SightRange : Int32.MaxValue;

            visibleTiles.Add(sourceTile);
            yield return sourceTile;

            frontier.EnqueueAll(map.GetAdjacentTiles(sourceTile));

            while (!frontier.IsEmpty)
            {
                var cur = frontier.Dequeue();

                // Don't reprocess tiles we've already decided on.
                if (tilesProcessed.Contains(cur)) continue;

                tilesProcessed.Add(cur);

                var curDistanceToSource = cur.DistanceTo(sourceTile);

                List<MapTile> nextTiles = new List<MapTile>();

                // Loop through adjacent tiles to
                //  * Find the tile nearest to the actor for whom we're determining LoS. This is the tile from which visibility is traced.
                //  * Find all tiles further away from the actor, so we can enqueue them. LoS is potentially traced through me to these 
                //    adjacent tiles.
                MapTile tileNearestToSource = null;
                var minDistanceToSource = double.MaxValue;
                foreach (var tile in map.GetAdjacentTiles(cur))
                {
                    var distanceToSource = tile.DistanceTo(sourceTile);

                    if (distanceToSource > sightRange) continue;

                    distanceToSource += tile.DistanceTo(cur);

                    if (tileNearestToSource == null || distanceToSource < minDistanceToSource
                      || (!tileNearestToSource.Proto.BlocksSight && tile.Proto.BlocksSight && distanceToSource == minDistanceToSource))
                    {
                        tileNearestToSource = tile;
                        minDistanceToSource = distanceToSource;
                    } 
                    
                    if (curDistanceToSource < distanceToSource)
                    {
                        nextTiles.Add(tile);
                    }
                }

                // If the adjacent tile to me nearest to the source is visible, I am also visible.
                if (visibleTiles.Contains(tileNearestToSource) && !tileNearestToSource.Proto.BlocksSight)
                {
                    yield return cur;
                    visibleTiles.Add(cur);

                    // Enqueue all adjacent tiles further away from the source than I am, unless I block sight.
                    // Tiles behind a tile that blocks sight could potentially only be seen if another tile can have visibility traced
                    // through it to that tile.
                    if (!cur.Proto.BlocksSight) 
                    {
                        frontier.EnqueueAll(nextTiles);
                    }
                }
            }
        }
    }
}
