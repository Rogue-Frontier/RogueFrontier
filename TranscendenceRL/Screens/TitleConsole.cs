﻿using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Controls;
using SadConsole.Input;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Xna.Framework.Input.Keys;
using static Common.Helper;
using Common;
using System.IO;

namespace TranscendenceRL {
    class TitleConsole : Window {
        string[] title = Properties.Resources.Title.Replace("\r\n", "\n").Split('\n');
        int titleStart;
        World World = new World();

        public AIShip pov;
        public int povTimer;
        public List<InfoMessage> povDesc;

        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles;

        public TitleConsole(int width, int height) : base(width, height) {
            UseKeyboard = true;
            ButtonTheme BUTTON_THEME = new SadConsole.Themes.ButtonTheme() {
                Normal = new SadConsole.Cell(Color.Blue, Color.Transparent),
                Disabled = new Cell(Color.Gray, Color.Transparent),
                Focused = new Cell(Color.Cyan, Color.Transparent),
                MouseDown = new Cell(Color.White, Color.Transparent),
                MouseOver = new Cell(Color.Cyan, Color.Transparent),
            };
            camera = new XY(0, 0);
            tiles = new Dictionary<(int, int), ColoredGlyph>();
            titleStart = Width;
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
            */

            World.types.Load(Directory.GetFiles("Content", "Main.xml"));
        }
        private void StartGame() {
            Hide();
            //new GameConsole(Width/2, Height/2).Show(true);
            new ShipSelector(Width, Height, World).Show(true);
        }
        private void Exit() {
            Environment.Exit(0);
        }
        public override void Update(TimeSpan timeSpan) {
            World.entities.UpdateSpace();
            World.effects.UpdateSpace();

            tiles.Clear();
            //Update everything
            foreach (var e in World.entities.all) {
                e.Update();
                if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
                    tiles[e.Position] = e.Tile;
                }
            }
            foreach (var e in World.effects.all) {
                e.Update();
                if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
                    tiles[e.Position] = e.Tile;
                }
            }

            World.entitiesAdded.ForEach(e => World.entities.all.Add(e));
            World.effectsAdded.ForEach(e => World.effects.all.Add(e));
            World.entitiesAdded.Clear();
            World.effectsAdded.Clear();
            World.entities.all.RemoveWhere(e => !e.Active);
            World.effects.all.RemoveWhere(e => !e.Active);

            if(World.entities.all.OfType<IShip>().Count() < 5) {
                var shipClasses = World.types.shipClass.Values;
                var shipClass = shipClasses.ElementAt(World.karma.Next(shipClasses.Count));
                var angle = World.karma.NextDouble() * Math.PI * 2;
                var distance = World.karma.Next(10, 20);
                var center = World.entities.all.FirstOrDefault()?.Position ?? new XY(0, 0);
                var ship = new BaseShip(World, shipClass, Sovereign.Gladiator, center + XY.Polar(angle, distance));
                var enemy = new AIShip(ship, new AttackAllOrder());
                World.entities.all.Add(enemy);
            }
            if(pov == null || povTimer < 1) {
                pov = World.entities.all.OfType<AIShip>().First();
                UpdatePOVDesc();
                povTimer = 150;
            } else if(!pov.Active) {
                povTimer--;
            }
            /*
            //The buffer is updated every update rather than every draw, so that the camera stays centered on the ship
            tiles.Clear();
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var offset = new XY(x, y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    var e = World.entities[location].FirstOrDefault() ?? World.effects[location].FirstOrDefault();
                    if (e != null) {
                        var tile = e.Tile;
                        if (tile.Background == Color.Transparent) {
                            tile.Background = World.backdrop.GetBackground(location, camera);
                        }
                        tiles[(x, y)] = tile;
                    } else {
                        tiles[(x, y)] = World.backdrop.GetTile(new XY(x, y), camera);
                    }
                }
            }
            */
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
        public override void Draw(TimeSpan drawTime) {
            Clear();

            var titleY = 0;
            
            foreach(var line in title) {
                if(titleStart < line.Length) {
                    Print(titleStart, titleY, line.Substring(titleStart), Color.White, Color.Transparent);
                }
                titleY++;
            }
            if (titleStart > 0) {
                titleStart--;
            } else {
                int descX = Width / 2;
                int descY = Height * 3 / 4;


                bool indent = false;
                foreach (var line in povDesc) {
                    line.Update();

                    var lineX = descX + (indent ? 8 : 0);

                    Print(lineX, descY, line.Draw());
                    indent = true;
                    descY++;
                }
            }
            camera = pov.Position;
            for (int x = titleStart; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = GetGlyph(x, y);

                    var offset = new XY(x, y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    if (g == 0 || g == ' ') {
                        
                        
                        if(tiles.TryGetValue(location, out var tile)) {
                            if(tile.Background == Color.Transparent) {
                                tile.Background = World.backdrop.GetBackground(location, camera);
                            }
                            this.SetCellAppearance(x, y, tile);
                        } else {
                            this.SetCellAppearance(x, y, World.backdrop.GetTile(location, camera));
                        }
                    } else {
                        SetBackground(x, y, World.backdrop.GetBackground(location, camera));
                    }
                    
                }
            }

            Base:
            base.Draw(drawTime);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            if(info.IsKeyPressed(Enter)) {
                StartGame();
            }
            if(info.IsKeyPressed(Escape)) {
                Exit();
            }
#if DEBUG
            if (info.IsKeyDown(LeftShift) && info.IsKeyPressed(G)) {
                Hide();
                new PlayerMain(Width, Height, World, World.types.Lookup<ShipClass>("scAmethyst")).Show(true);
            }
#endif
            return base.ProcessKeyboard(info);
        }
    }
}
