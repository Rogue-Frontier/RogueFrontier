﻿using SadConsole;
using SadRogue.Primitives;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using static SadConsole.Input.Keys;
using Common;
using System.IO;
using Console = SadConsole.Console;
using TranscendenceRL.Types;
using Newtonsoft.Json;
using TranscendenceRL.Screens;
using ArchConsole;

namespace TranscendenceRL {
    class TitleScreen : Console {
        World World;
        
        string[] title = File.ReadAllText("RogueFrontierContent/Title.txt").Replace("\r\n", "\n").Split('\n');
        Settings settings;

        public AIShip pov;
        public int povTimer;
        public List<InfoMessage> povDesc;

        XY screenCenter;

        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;

        public TitleScreen(int width, int height, World World) : base(width, height) {
            this.World = World;

            UseKeyboard = true;

            screenCenter = new XY(Width / 2, Height / 2);

            camera = new XY(0, 0);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            /*
            {
                var size = 20;
                var y = (Height * 3) / 4;
                var start = new Button(size, 1) {
                    //Position = new Point((Width / 2) - (size / 2), y),
                    Position = new Point(8, y),
                    Text = "NEW GAME",
                    Theme = BUTTON_THEME,
                };
                start.Click += (o, e) => StartGame();
                Add(start);
            }
            {
                var size = 20;
                //var y = (Height * 3) / 4;
                var y = (Height * 3) / 4 + 2;
                var exit = new Button(size, 1) {
                    //Position = new Point((Width * 3) / 4 - (size / 2), y),
                    Position = new Point(8, y),
                    Text = "EXIT",
                    Theme = BUTTON_THEME
                };
                exit.Click += (o, e) => Exit();
                Add(exit);
            }
.                         .
.  [Enter]      Play      .
.  [Shift + A]  Arena     .
.  [Shift + C]  Controls  .
.  [Escape]     Exit      .
.                         .
...........................
            */

            int x = 3;
            int y = 16;
            Children.Add(new LabelButton("[Enter]     Play Story Mode", StartGame) { Position = new Point(x, y++) });

            Children.Add(new LabelButton("[Shift + A] Arena Mode", StartArena) { Position = new Point(x, y++) });
            Children.Add(new LabelButton("[Shift + C] Controls", StartConfig) { Position = new Point(x, y++) });
            Children.Add(new LabelButton("[Shift + S] Survival Mode", StartSurvival) { Position = new Point(x, y++) });
            
            Children.Add(new LabelButton("[Escape]    Exit", Exit) { Position = new Point(x, y++) });

            var f = "Settings.json";
            if(File.Exists(f)) {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(f));
            } else {
                settings = new Settings();
            }
        }
        private void StartGame() {
            SadConsole.Game.Instance.Screen = new TitleSlideIn(this, new PlayerCreator(this, World, StartCrawl)) { IsFocused = true };
            
            void StartCrawl(ShipSelectorModel context) {
                var loc = AppDomain.CurrentDomain.BaseDirectory + Path.PathSeparator + context.playerName;
                string file;
                do { file = $"{loc}-{new Random().Next(9999)}"; }
                while (File.Exists(file));


                Player player = new Player() {
                    Settings = settings,
                    file = file,
                    name = context.playerName,
                    Genome = context.playerGenome
                };

                var (playable, index) = (context.playable, context.shipIndex);
                var playerClass = playable[index];

                CrawlScreen crawl = null;
                crawl = new CrawlScreen(Width, Height, StartWorld) { IsFocused = true };
                SadConsole.Game.Instance.Screen = crawl;

                void StartWorld() {

                    //Name is seed
                    var seed = player.name.GetHashCode();
                    World = new World(World.types, new Random(seed), new Backdrop(new Random(seed)));
                    World.types.Lookup<SystemType>("system_orion").Generate(World);
                    World.UpdatePresent();

                    var start = World.entities.all.OfType<Marker>().First(m => m.Name == "Start");
                    start.Active = false;
                    var playerStart = start.Position;
                    var playerSovereign = World.types.Lookup<Sovereign>("sovereign_player");
                    var playerShip = new PlayerShip(player, new BaseShip(World, playerClass, playerSovereign, playerStart));
                    playerShip.Messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));

                    World.AddEffect(new Heading(playerShip));
                    World.AddEntity(playerShip);

                    var playerMain = new PlayerMain(Width, Height, World, playerShip);
                    playerShip.OnDestroyed += (p, d, wreck) => playerMain.EndGame(d, wreck);

                    playerMain.Update(new TimeSpan());
                    playerMain.PlaceTiles();
                    playerMain.DrawWorld();
                    GameHost.Instance.Screen = new FlashTransition(Width, Height, crawl,
                        new Pause(1,
                        new SimpleCrawl(Width, Height, "Today has been a long time in the making.\n\n" + ((new Random(seed).Next(5) + new Random().Next(2)) switch
                        {
                            1 => "Maybe history will remember.",
                            2 => "Tomorrow will be forever.",
                            3 => "Life runs short; hurry along now.",
                            _ => "Maybe all of it will have been for something.",
                        }),
                        new FadeIn(new Pause(1, playerMain)
                        )))) { IsFocused = true };
                }
            }
        }

        private void StartArena() {
            SadConsole.Game.Instance.Screen = new ArenaScreen(this, settings, World) { IsFocused = true, camera = camera, pov = pov };
        }
        private void StartConfig() {
            SadConsole.Game.Instance.Screen = new ConfigScreen(this, settings, World) { IsFocused = true };
        }
        private void StartSurvival() {
            SadConsole.Game.Instance.Screen = new PlayerCreator(this, World, CreateGame) { IsFocused = true };

            void CreateGame(ShipSelectorModel context) {
                var loc = AppDomain.CurrentDomain.BaseDirectory + Path.PathSeparator + context.playerName;
                string file;
                do { file = $"{loc}-{new Random().Next(9999)}"; }
                while (File.Exists(file));


                Player player = new Player() {
                    Settings = settings,
                    file = file,
                    name = context.playerName,
                    Genome = context.playerGenome
                };

                var (playable, index) = (context.playable, context.shipIndex);
                var playerClass = playable[index];

                //Name is seed
                var seed = player.name.GetHashCode();

                var playerStart = new XY(0, 0);
                var playerSovereign = World.types.Lookup<Sovereign>("sovereign_player");
                var playerShip = new PlayerShip(player, new BaseShip(World, playerClass, playerSovereign, playerStart));
                playerShip.Messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));

                World.RemoveAll();


                World.AddEffect(new Heading(playerShip));
                World.AddEntity(playerShip);

                int waveSize = 1;
                int shipCount = 1;
                void EnemyDestroyed() {
                    shipCount--;
                    if(shipCount == 0) {
                        waveSize++;

                        World.AddEntity(new TimedEvent(240, () => {
                            playerShip.Messages.Add(new InfoMessage("Wave incoming!"));
                            CreateWave();
                        }));
                    }
                }
                void CreateWave() {
                    for (int i = 0; i < waveSize; i++) {
                        var ship = new AIShip(new BaseShip(World,
                            World.types.shipClass.Values.GetRandom(World.karma),
                            Sovereign.Gladiator,
                            XY.Polar(0, 100)), new AttackOrder(playerShip));
                        ship.Ship.OnDestroyed += (b, destroyer, wreck) => {
                            EnemyDestroyed();
                        };
                        World.AddEntity(ship);
                    }                        
                    shipCount = waveSize;
                }

                World.AddEntity(new TimedEvent(240, () => {
                    playerShip.Messages.Add(new InfoMessage("Wave incoming!"));
                    CreateWave();
                }));



                var playerMain = new PlayerMain(Width, Height, World, playerShip);
                playerShip.OnDestroyed += (p, d, wreck) => playerMain.EndGame(d, wreck);

                playerMain.Update(new TimeSpan());
                playerMain.PlaceTiles();
                playerMain.DrawWorld();
                SadConsole.Game.Instance.Screen = playerMain;
                playerMain.IsFocused = true;
            }
        }

        private void Exit() {
            Environment.Exit(0);
        }
        public override void Update(TimeSpan timeSpan) {
            tiles.Clear();

            World.UpdateAdded();
            World.UpdateActive(tiles);
            World.UpdateRemoved();

            if(World.entities.all.OfType<IShip>().Count() < 5) {
                var shipClasses = World.types.shipClass.Values;
                var shipClass = shipClasses.ElementAt(World.karma.Next(shipClasses.Count));
                var angle = World.karma.NextDouble() * Math.PI * 2;
                var distance = World.karma.Next(10, 20);
                var center = World.entities.all.FirstOrDefault()?.Position ?? new XY(0, 0);
                var ship = new BaseShip(World, shipClass, Sovereign.Gladiator, center + XY.Polar(angle, distance));
                var enemy = new AIShip(ship, new AttackAllOrder());
                World.AddEntity(enemy);
                World.AddEffect(new Heading(enemy));
                //Update now in case we need a POV
                World.UpdatePresent();
            }
            if(pov == null || povTimer < 1) {
                pov = World.entities.all.OfType<AIShip>().OrderBy(s => (s.Position - camera).Magnitude).First();
                UpdatePOVDesc();
                povTimer = 150;
            } else if(!pov.Active) {
                povTimer--;
            }

            //Smoothly move the camera to where it should be
            if ((camera - pov.Position).Magnitude < pov.Velocity.Magnitude / 15 + 1) {
                camera = pov.Position;
            } else {
                var step = (pov.Position - camera) / 15;
                if (step.Magnitude < 1) {
                    step = step.Normal;
                }
                camera += step;
            }
        }
        public void UpdatePOVDesc() {
            povDesc = new List<InfoMessage> {
                    new InfoMessage(pov.Name),
                };
            if (pov.DamageSystem is LayeredArmorSystem las) {
                povDesc.AddRange(las.GetDesc().Select(m => new InfoMessage(m)));
            } else if (pov.DamageSystem is HPSystem hp) {
                povDesc.Add(new InfoMessage($"HP: {hp}"));
            }
            foreach (var device in pov.Ship.Devices.Installed) {
                povDesc.Add(new InfoMessage(device.source.type.name));
            }
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();
            var titleY = 0;
            foreach (var line in title) {
                this.Print(0, titleY, line, Color.White, Color.Transparent);
                titleY++;
            }

            //Wait until we are focused to print the POV desc
            //This will happen when TitleSlide transition finishes
            if(IsFocused) {
                int descX = Width / 2;
                int descY = Height * 3 / 4;


                bool indent = false;
                foreach (var line in povDesc) {
                    line.Update();

                    var lineX = descX + (indent ? 8 : 0);

                    this.Print(lineX, descY, line.Draw());
                    indent = true;
                    descY++;
                }
            }
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, Height - y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x,y).A == 0) {
                        
                        
                        if(tiles.TryGetValue(location.RoundDown, out var tile)) {
                            if(tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            this.SetCellAppearance(x, y, tile);
                        } else {
                            this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                        }
                    } else {
                        this.SetBackground(x, y, World.backdrop.GetBackground(location, camera));
                    }
                    
                }
            }
            base.Render(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Keys.K)) {
                if (pov.Active) {
                    pov.Destroy(pov);
                }
            }


            if(info.IsKeyPressed(Keys.P)) {

            }
            if (info.IsKeyPressed(Enter)) {
                StartGame();
            }
            if (info.IsKeyPressed(Escape)) {
                Exit();
            }
            if (info.IsKeyDown(LeftShift)) {
                if (info.IsKeyPressed(A)) {
                    StartArena();
                }
                if (info.IsKeyPressed(C)) {
                    StartConfig();
                }
                if (info.IsKeyPressed(S)) {
                    StartSurvival();
                }
#if DEBUG
                if (info.IsKeyPressed(G)) {
                    var loc = AppDomain.CurrentDomain.BaseDirectory + Path.PathSeparator + "Debug";
                    string file;
                    do { file = $"{loc}-{new Random().Next(9999)}"; }
                    while (File.Exists(file));

                    Player player = new Player() {
                        Settings = settings,
                        file = file,
                        name = "Player",
                        Genome = World.types.genomeType.Values.First()
                    };

                    World.entities.all.Clear();
                    World.effects.all.Clear();
                    World.types.Lookup<SystemType>("system_orion").Generate(World);
                    World.UpdatePresent();

                    var playerClass = World.types.Lookup<ShipClass>("ship_amethyst");
                    var playerStart = World.entities.all.First(e => e is Marker m && m.Name == "Start").Position;
                    var playerSovereign = World.types.Lookup<Sovereign>("sovereign_player");
                    var playerShip = new PlayerShip(player, new BaseShip(World, playerClass, playerSovereign, playerStart));
                    playerShip.Powers.Add(new Power(World.types.Lookup<PowerType>("power_silence")));
                    playerShip.Messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));

                    Func<Item> item = () => new Item(new ItemType() { name = "aa" });
                    playerShip.Items.Add(item());
                    playerShip.Items.Add(item());
                    playerShip.Items.Add(item());

                    var wreck = new Wreck(playerShip);
                    wreck.Items.Add(item());
                    wreck.Items.Add(item());
                    wreck.Items.Add(item());
                    World.AddEntity(wreck);

                    World.AddEffect(new Heading(playerShip));
                    World.AddEntity(playerShip);

                    var wingmateClass = World.types.Lookup<ShipClass>("ship_beowulf");

                    var wingmate = new AIShip(new BaseShip(World, wingmateClass, playerSovereign, playerStart), new EscortOrder(playerShip, new XY(-5, 0)));
                    World.AddEntity(wingmate);
                    World.AddEffect(new Heading(wingmate));

                    var playerMain = new PlayerMain(Width, Height, World, playerShip);
                    playerShip.OnDestroyed += (p, d, wreck) => playerMain.EndGame(d, wreck);

                    playerMain.IsFocused = true;

                    File.WriteAllText(file, JsonConvert.SerializeObject(playerMain));

                    SadConsole.Game.Instance.Screen = playerMain;
                }
#endif
            }
            return base.ProcessKeyboard(info);
        }
    }



}