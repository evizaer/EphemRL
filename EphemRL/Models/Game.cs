using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Conflight;
using UtiliCS;

namespace EphemRL.Models
{
    public enum ManaElement { Fire, Earth, Water, Void, Life }

    public enum InputMode { Normal, Selecting }

    public class Game : ViewModel
    {
        public List<TerrainProto> TerrainTypes { get; set; }
        public List<SpellProto> Spells { get; set; }
        public List<ActorProto> ActorProtos { get; set; } 

        public ObservableCollection<MapTile> Tiles { get; set; }
        public Actor PlayerActor { get; set; }

        public Dictionary<Actor, MapTile> ActorPlaces { get; set; }

        public ObservableCollection<SpellCastDelta> SpellDeltas { get; set; } 

        public int MapWidth { get; set; }
        public int MapHeight { get; set; }

        public int MapPixelWidth { get { return MapWidth*MapTile.TileSize; } }
        public int MapPixelHeight { get { return MapHeight*MapTile.TileSize; } }

        public MapTile SelectedTile { get; set; }

        public InputMode Mode { get; set; }

        public Game()
        {
            Mode = InputMode.Normal;
           
            // 832 is start index of terrain tile graphics.
            // spritesheet is 64 tiles wide. tiles are 32 pixels wide.
            Spritesheet.Build("Content\\tiles.png", "Content\\sprites.txt", 32);

            TerrainTypes = ObjectLoader.LoadInstanceList<TerrainProto>(File.ReadAllText("Content\\terrain.cflt"));
            Spells = ObjectLoader.LoadInstanceList<SpellProto>(File.ReadAllText("Content\\spells.cflt"));
            ActorProtos = ObjectLoader.LoadInstanceList<ActorProto>(File.ReadAllText("Content\\actors.cflt"));

            MapWidth = MapHeight = 20;

            SpellDeltas = new ObservableCollection<SpellCastDelta>();

            Tiles = new ObservableCollection<MapTile>();
            0.To(MapWidth * MapHeight).Select(i => new MapTile(i % MapWidth, i / MapWidth, TerrainTypes.Choose()))
                                      .Do(t => Tiles.Add(t));

            var actorStartTile = Tiles.Where(t => t.Proto.IsPassable).Choose();
            actorStartTile.Actor = PlayerActor = new Actor(ActorProtos.Single(ap => ap.Name == "You"), Spells);
            PlayerActor.Spells = Spells;

            ActorPlaces = new Dictionary<Actor, MapTile>
            {
                {PlayerActor, actorStartTile}
            };

            InputActions = new Dictionary<string, Action>
            {
                {"MoveNorth", () => { MoveActorBy(PlayerActor, 0, -1); EndTurn(); }},
                {"MoveWest", () => { MoveActorBy(PlayerActor, -1, 0); EndTurn(); }},
                {"MoveEast", () => { MoveActorBy(PlayerActor, 1, 0); EndTurn(); }},
                {"MoveSouth", () => { MoveActorBy(PlayerActor, 0, 1); EndTurn(); }},
            };

            SelectableTiles = new List<MapTile>();

            PopulateSpellDeltas();
        }
        
        public void ResolveSpell(SpellCastDelta delta)
        {
            ExpendManaForSpell(delta);

            //TODO: Further refactor this into some kind of static spell resolution class, provide calling details in spells.cflt.
            if (delta.Spell.Name == "Create Water")
            {
                var casterPosition = ActorPlaces[delta.Caster];
                Tiles.Remove(casterPosition);
                var newTile = new MapTile(casterPosition.X, casterPosition.Y,
                    TerrainTypes.Single(tt => tt.Name == "Water"));
                Tiles.Insert(newTile.Y * MapWidth + newTile.X, newTile);


                //TODO: Move all present actors and items to new tile.
                newTile.Actor = delta.Caster;

                ActorPlaces[delta.Caster] = newTile;
            } 

            EndTurn();
        }

        private void PopulateSpellDeltas()
        {
            SpellDeltas.Clear();

            PlayerActor.Spells.Select(s => BuildSpellDelta(s, PlayerActor))
                              .Do(sd => SpellDeltas.Add(sd));
        }

        private void EndTurn()
        {
            UnselectAllTiles();
            Mode = InputMode.Normal;
            PopulateSpellDeltas();
        }

        private SpellCastDelta BuildSpellDelta(SpellProto spell, Actor caster)
        {
            var result = new SpellCastDelta
            {
                Caster = caster,
                Spell = spell,
                // TODO: Allow for other target tiles (like a target selected by the user or AI)
                TargetTile = ActorPlaces[caster],
                IsCastable = false
            };

            // This dictionary stores our progress towards expending all mana required to cast the spell.
            // It will be modified in the below loop: as we expend mana, we decrease the values in this dict.
            Dictionary<ManaElement, int> elementToManaNeeded;
            if (spell.AggregateManaRequired != null)
            {
                elementToManaNeeded = spell.AggregateManaRequired.Where(e => e.Value > 0)
                                                                 .ToDictionary(e => e.Key, e => e.Value);
            }
            else
            {
                elementToManaNeeded = new Dictionary<ManaElement, int>();
            }
            
            // All required mana must be expended from tiles listed in the TileManaRequirements, and all
            // possible mana to fulfill requirements is drawn from them in order of appearance in the config.
            foreach (var requirement in spell.TileManaRequired)
            {
                int x = result.TargetTile.X + requirement.DX, y = result.TargetTile.Y + requirement.DY;

                if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight)
                {
                    var tile = GetTileAt(x, y);

                    // First, expend mana on this tile for any tile-specific requirements.
                    if (requirement.Mana != null)
                    {
                        foreach (var pair in requirement.Mana)
                        {
                            var element = pair.Key;
                            var neededMana = pair.Value;
                            var presentMana = tile.Mana.GetManaCount(element);

                            if (neededMana <= presentMana)
                            {
                                result.AddToManaDelta(tile, element, neededMana);
                            }
                            else
                            {
                                // We didn't have required mana, so bail. Current state of result has IsCastable=false, so we're fine.
                                return result;
                            }
                        }
                    }

                    // Next, fulfill the aggregate mana requirements by greedily taking relevant mana from this tile.
                    var satisfiedRequirements = new List<ManaElement>();
                    foreach (var element in elementToManaNeeded.Keys.Where(e => tile.Mana.HasAny(e)).ToList())
                    {
                        var neededMana = elementToManaNeeded[element];
                        var presentMana = tile.Mana.GetManaCount(element);

                        int manaUsed = Math.Min(presentMana, neededMana);

                        result.AddToManaDelta(tile, element, manaUsed);

                        int remainingManaNeeded = neededMana - manaUsed;
                        if (remainingManaNeeded <= 0)
                        {
                            satisfiedRequirements.Add(element);
                        }
                        else
                        {
                            elementToManaNeeded[element] = remainingManaNeeded;
                        }
                    }

                    satisfiedRequirements.Do(element => elementToManaNeeded.Remove(element));
                }
            }

            // If there's any mana needed after expending mana wherever possible, this spell cannot be cast.
            if (!elementToManaNeeded.Any())
            {
                result.IsCastable = true;
            }

            return result;
        }

        private void ExpendManaForSpell(SpellCastDelta delta)
        {
            delta.ManaDelta.Do((tile, mana) => tile.Mana.Expend(mana));
        }

        public void MoveActorBy(Actor actor, int dx, int dy)
        {
            var t = ActorPlaces[actor];

            var x = t.X + dx;
            var y = t.Y + dy;

            MoveActorTo(actor, x, y);
        }

        public void MoveActorTo(Actor actor, int x, int y)
        {
            x = x.Clamp(0, MapWidth - 1);
            y = y.Clamp(0, MapHeight - 1);

            var targetTile = GetTileAt(x, y);
            if (targetTile.Proto.IsPassable)
            {
                ActorPlaces[actor].Actor = null;
                ActorPlaces[actor] = targetTile;

                targetTile.Actor = actor;
                if (targetTile.Proto.Name == "Lava") actor.Health--;
                Debug.WriteLine("Player moved to {0}, {1}", targetTile.X, targetTile.Y);
            }
            else
            {
                Debug.WriteLine("Player move to {0}, {1} cancelled due to impassability.", targetTile.X, targetTile.Y);
            }
        }

        public MapTile GetTileAt(int x, int y)
        {
            return Tiles.GetAt(x, y, MapWidth);
        }


        public List<MapTile> SelectableTiles { get; set; } 
        
        private RelayCommand _NextSpellStateCommand;
        public RelayCommand NextSpellStateCommand
        {
            get
            {
                return _NextSpellStateCommand ?? (_NextSpellStateCommand = new RelayCommand(hotkey =>
                {
                    var spell = Spells[Int32.Parse((string)hotkey)];
                    var delta = SpellDeltas.SingleOrDefault(sd => sd.IsCastable && sd.Caster == PlayerActor && sd.Spell == spell);

                    if (delta == null) return;

                    if (Mode == InputMode.Normal)
                    {
                        if (spell.Target == SpellTarget.Self)
                        {
                            ResolveSpell(delta);
                        }
                        else
                        {
                            MarkSelectableTilesFor(delta);
                            Mode = InputMode.Selecting;
                        }
                    }
                    else if (Mode == InputMode.Selecting)
                    {
                        // If no tile is selected, cancel selection mode.
                        if (SelectedTile != null)
                        {
                            delta.TargetTile = SelectedTile;
                            ResolveSpell(delta);

                            UnselectAllTiles();
                            Mode = InputMode.Normal;
                        }
                        else
                        {
                            UnselectAllTiles();
                            MarkSelectableTilesFor(delta);
                        }

                    }
                }));
            }
        }

        public void MarkSelectableTilesFor(SpellCastDelta delta)
        {
            var origin = ActorPlaces[delta.Caster];

            var frontier = new Queue<MapTile>();
            frontier.Enqueue(origin);

            while (frontier.Any())
            {
                var cur = frontier.Dequeue();

                MarkAsSelectable(cur);

                GetAdjacentTiles(cur).Where(t => !SelectableTiles.Contains(t) 
                                         && delta.Spell.Range >= Math.Sqrt(Math.Pow(t.X - origin.X, 2) + Math.Pow(t.Y - origin.Y, 2)))
                                     .Do(frontier.Enqueue);
            }
        }

        public static List<Tuple<int, int>> AdjacencyDeltas =  new List<Tuple<int, int>>
        {
            Tuple.Create(1, 0), Tuple.Create(1, 1), Tuple.Create(0, 1), 
            Tuple.Create(-1, 0), Tuple.Create(-1, -1), Tuple.Create(0, -1),
            Tuple.Create(-1, 1), Tuple.Create(1, -1)
        };

        public IEnumerable<MapTile> GetAdjacentTiles(MapTile t)
        {
            foreach (var delta in AdjacencyDeltas)
            {
                int newX = t.X + delta.Item1, newY = t.Y + delta.Item2;

                if (newX >= 0 && newX < MapWidth && newY >= 0 && newY < MapHeight)
                {
                    yield return GetTileAt(newX, newY);
                }
            }
        }  

        public void MarkAsSelectable(MapTile tile)
        {
            SelectableTiles.Add(tile);
            tile.IsSelectable = true;
        }

        public void UnselectAllTiles()
        {
            SelectedTile = null;
            SelectableTiles.Do(t => { t.IsSelectable = t.IsSelected = false; });
            SelectableTiles.Clear();
        }

        private RelayCommand _MovePlayerCommand;
        public RelayCommand MovePlayerCommand
        {
            get { return _MovePlayerCommand ?? (_MovePlayerCommand = new RelayCommand(o => InputActions[(string)o]())); }
        }

        private RelayCommand _SelectTileCommand;
        public RelayCommand SelectTileCommand
        {
            get 
            {
                return _SelectTileCommand ?? (_SelectTileCommand = new RelayCommand(o =>
                {
                    var tile = (MapTile)o;
                    if (SelectedTile != null) SelectedTile.IsSelected = false;
                    SelectedTile = tile;
                    tile.IsSelected = true;
                }));
            }
        }

        private Dictionary<string, Action> InputActions { get; set; }

        private Dictionary<string, Action<Actor>> SpellActions { get; set; }

        public static IEnumerable<ManaElement> GetElements()
        {
            return Enum.GetValues(typeof(ManaElement)).Cast<ManaElement>();
        }
    }
}
