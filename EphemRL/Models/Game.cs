using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Conflight;
using UtiliCS;
using EphemRL.Helpers;

namespace EphemRL.Models
{
    public enum ManaElement { Fire, Earth, Water, Void, Life }

    public enum InputMode { Normal, Selecting }

    public class Game : ViewModel
    {
        public List<SpellProto> Spells { get; set; }
        public List<ActorProto> ActorProtos { get; set; }

        public Dictionary<SpellProto, string> SpellHotkeys { get; set; }

        public static List<string> PossibleSpellHotkeys = new List<string> { "1", "2", "3", "4", "Q", "E" }; 

        public Actor PlayerActor { get; set; }

        public ManaClock Clock { get; set; }

        public ObservableCollection<SpellCastDelta> SpellDeltas { get; set; } 

        public MapTile SelectedTile { get; set; }

        public InputMode Mode { get; set; }

        public Map Map { get; set; }

        public ManaElement RegenPhase { get; set; }

        public Game()
        {
            Mode = InputMode.Normal;

            Clock = new ManaClock(GetElements());

            // 832 is start index of terrain tile graphics.
            // spritesheet is 64 tiles wide. tiles are 32 pixels wide.
            Spritesheet.Build("Content\\tiles.png", "Content\\sprites.txt", 32);

            Spells = ObjectLoader.LoadInstanceList<SpellProto>(File.ReadAllText("Content\\spells.cflt"));
            ActorProtos = ObjectLoader.LoadInstanceList<ActorProto>(File.ReadAllText("Content\\actors.cflt"));

            SpellDeltas = new ObservableCollection<SpellCastDelta>();

            var terrainTypes = ObjectLoader.LoadInstanceList<TerrainProto>(File.ReadAllText("Content\\terrain.cflt"));
            Map = new Map(terrainTypes);

            Map.SpawnActor(PlayerActor = new Actor(ActorProtos.Single(ap => ap.Name == "You"), Spells));
            SpellHotkeys = PlayerActor.Spells.Zip(PossibleSpellHotkeys, (spell, hotkey) => new { Hotkey = hotkey, Spell = spell })
                                               .ToDictionary(o => o.Spell, o => o.Hotkey);

            Map.SpawnActor(new Actor(ActorProtos.Single(ap => ap.Name == "Orc"), Enumerable.Empty<SpellProto>()));

            InputActions = new Dictionary<string, Action>
            {
                {"MoveNorth", () => { Map.MoveActorBy(PlayerActor, 0, -1); EndTurn(); }},
                {"MoveWest", () => { Map.MoveActorBy(PlayerActor, -1, 0); EndTurn(); }},
                {"MoveEast", () => { Map.MoveActorBy(PlayerActor, 1, 0); EndTurn(); }},
                {"MoveSouth", () => { Map.MoveActorBy(PlayerActor, 0, 1); EndTurn(); }},
                {"EndTurn", () => { EndTurn(); }},
                {"CancelCast", () => { UnselectAllTiles(); Mode = InputMode.Normal; }}
            };

            SelectableTiles = new List<MapTile>();

            EndTurn();
        }
        
        public void ResolveSpell(SpellCastDelta delta)
        {
            Map.ExpendManaForSpell(delta, delta.ValidTargets.Count == 1 ? delta.ValidTargets.First().Key : SelectedTile);

            //TODO: Further refactor this into some kind of static spell resolution class, provide calling details in spells.cflt.
            if (delta.Spell.Name == "Create Water")
            {
                Map.ChangeTerrainOf(Map.GetActorTile(delta.Caster), "Water");
            }
            else if (delta.Spell.Name == "Firebolt")
            {
                if (SelectedTile.Actor != null)
                {
                    SelectedTile.Actor.Health -= 3;
                }

                // TODO: Implement "kindle strength" so not all spells light everything on fire uniformly.
                if (SelectedTile.Proto.FireResistance < 1)
                {
                    SelectedTile.IsBurning = true;
                }
            }
            else if (delta.Spell.Name == "Blink")
            {
                if (SelectedTile.Proto.IsPassable && SelectedTile.Actor == null)
                {
                    Map.MoveActorTo(delta.Caster, SelectedTile);                    
                }
            }

            EndTurn();
        }

        private void PopulateSpellDeltas()
        {
            SpellDeltas.Clear();

            SpellDeltas.AddRange(PlayerActor.Spells.Select(s => BuildSpellDelta(s, PlayerActor, 
                                                                SpellHotkeys.ContainsKey(s) ? SpellHotkeys[s] : null)));
        }

        private void EndTurn()
        {
            UnselectAllTiles();
            Mode = InputMode.Normal;

            FireSimulator.Tick(Map);

            Map.Actors.AsParallel().Do(a => a.VisibleTiles = LineOfSight.Calculate(Map, Map.GetActorTile(a)));

            Map.SetTileVisibilityForLineOfSight(PlayerActor);

            Map.RegenerateMana(Clock.Tick());

            PopulateSpellDeltas();
        }

        private SpellCastDelta BuildSpellDelta(SpellProto spell, Actor caster, string hotkey)
        {
            // Build mana delta for mana requirements that are relative to the caster's position.
            ManaDelta manaNearSelf = GetManaDelta(Map.GetActorTile(caster), spell.TileManaRequired.Where(tmr => tmr.RelativeTo == SpellTarget.Self), spell.ManaRequiredRelativeToCaster);

            var delta = new SpellCastDelta
            {
                Caster = caster,
                Spell = spell,
                Hotkey = hotkey,
                IsCastable = false
            };

            if (manaNearSelf == null) return delta;

            if (spell.TargetCriteria.Target == SpellTarget.Self)
            {
                delta.IsCastable = true;
                delta.ValidTargets = new Dictionary<MapTile,ManaDelta> { {Map.GetActorTile(caster), manaNearSelf} };
            }
            else
            {
                //TODO: Filter based on if in line of sight or passable, if the spell requires those things.
                var validTargets = Map.WalkTilesInRangeOf(Map.GetActorTile(caster), spell.Range)
                                      .Where(t => spell.TargetCriteria.AreSatisfiedBy(t, caster))
                                      .Select(t => new
                {
                    Tile = t,
                    Delta = manaNearSelf.Merge(GetManaDelta(t, spell.TileManaRequired.Where(tmr => tmr.RelativeTo == SpellTarget.Terrain), spell.ManaRequiredRelativeToTarget))
                }).Where(td => td.Delta != null).ToDictionary(td => td.Tile, td => td.Delta);

                delta.IsCastable = validTargets.Any();
                delta.ValidTargets = validTargets;
            }

            return delta;
        }

        private ManaDelta GetManaDelta(MapTile srcTile, IEnumerable<TileManaRequirement> tileRequirements, Dictionary<ManaElement, int> aggregateRequirements)
        {
            var result = new ManaDelta();

            // This dictionary stores our progress towards expending all mana required to cast the spell.
            // It will be modified in the below loop: as we expend mana, we decrease the values in this dict.
            Dictionary<ManaElement, int> elementToManaNeeded;
            if (aggregateRequirements != null)
            {
                elementToManaNeeded = aggregateRequirements.Where(e => e.Value > 0)
                                                                 .ToDictionary(e => e.Key, e => e.Value);
            }
            else
            {
                elementToManaNeeded = new Dictionary<ManaElement, int>();
            }
            
            // All required mana must be expended from tiles listed in the TileManaRequirements, and all
            // possible mana to fulfill requirements is drawn from them in order of appearance in the config.
            foreach (var requirement in tileRequirements)
            {
                int x, y;
                x = srcTile.X + requirement.DX;
                y = srcTile.Y + requirement.DY;

                if (x >= 0 && x < Map.Width && y >= 0 && y < Map.Height)
                {
                    var tile = Map.GetTileAt(x, y);

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
                                result.Add(tile, element, neededMana);
                            }
                            else
                            {
                                // We didn't have required mana, so bail. 
                                return null;
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

                        result.Add(tile, element, manaUsed);

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
            if (elementToManaNeeded.Any())
            {
                return null;
            }

            return result;
        }

        public List<MapTile> SelectableTiles { get; set; } 
        
        private RelayCommand _NextSpellStateCommand;
        public RelayCommand NextSpellStateCommand
        {
            get
            {
                return _NextSpellStateCommand ?? (_NextSpellStateCommand = new RelayCommand(hotkey =>
                {
                    var delta = SpellDeltas.SingleOrDefault(sd => sd.Hotkey == (string)hotkey && sd.IsCastable);

                    if (delta == null) return;

                    if (Mode == InputMode.Normal)
                    {
                        if (delta.Spell.TargetCriteria.Target == SpellTarget.Self)
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
            delta.ValidTargets.Keys.Do(MarkAsSelectable);
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

        private RelayCommand _HotkeyCommand;
        public RelayCommand HotkeyCommand
        {
            get { return _HotkeyCommand ?? (_HotkeyCommand = new RelayCommand(o => InputActions[(string)o]())); }
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