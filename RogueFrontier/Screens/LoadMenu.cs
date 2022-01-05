﻿using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using System;
using System.IO;
using System.Linq;
using Console = SadConsole.Console;

namespace RogueFrontier;

class LoadMenu : Console {
    Profile profile;
    public LoadMenu(int Width, int Height, Profile profile) : base(Width, Height) {
        this.profile = profile;

        UseKeyboard = true;
        FocusOnMouseClick = true;

        Init();
    }
    public void Reset() {
        Children.Clear();
        Init();
    }
    public void Init() {
        int x = 2;
        int y = 0;

        var files = Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}save", "*.trl");
        if (files.Any()) {
            var dir = Path.GetFullPath(".");
            foreach (var file in files) {

                var b = new LabelButton(file.Replace(dir, null), () => {
                    var loaded = SaveGame.Deserialize(File.ReadAllText(file));

                    var s = (Console)GameHost.Instance.Screen;
                    int Width = s.Width, Height = s.Height;

                    switch (loaded) {
                        case LiveGame live: {
                                var playerMain = new PlayerMain(Width, Height, profile, live.playerShip) { IsFocused = true };
                                //live.playerShip.player.Settings;

                                live.playerShip.onDestroyed += new EndGamePlayerDestroyed(playerMain);
                                GameHost.Instance.Screen = playerMain;
                                //If we have any load hooks, trigger them now
                                live.hook?.Value(playerMain);
                                break;
                            }
                        case DeadGame dead: {
                                var playerMain = new PlayerMain(Width, Height, profile, dead.playerShip);
                                playerMain.camera.position = dead.playerShip.position;
                                playerMain.PlaceTiles(new());
                                var deathScreen = new DeathScreen(playerMain, dead.epitaph);
                                dead.playerShip.onDestroyed += new EndGamePlayerDestroyed(playerMain);
                                GameHost.Instance.Screen = deathScreen;
                                deathScreen.IsFocused = true;
                                break;
                            }
                    }
                }) { Position = new Point(x, y++), FontSize = FontSize };
                Children.Add(b);
            }
        } else {
            Children.Add(new Label("No save files found") { Position = new Point(x, y++), FontSize = FontSize });
        }
    }

    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(Keys.Escape)) {
            Parent.Children.Remove(this);
        }
        return base.ProcessKeyboard(info);
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        return base.ProcessMouse(state);
    }
}
