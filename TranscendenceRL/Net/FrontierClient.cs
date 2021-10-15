﻿
using Common;
using System;
using System.Net.Sockets;
using Debug = System.Diagnostics.Debug;
using TcpClient = NetCoreServer.TcpClient;
using ArchConsole;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace TranscendenceRL {
    class GameClient : TcpClient {
        private ClientScreen game;
        private MemoryStream received;
        private string command;
        private int length;
        public GameClient(string address, int port, ClientScreen game) : base(address, port) {
            this.game = game;
        }
        protected override void OnConnected() { }
        protected override void OnDisconnected() { }
        protected override void OnReceived(byte[] buffer, long offset, long size) {
            var s = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            var m = Regex.Match(s, "([A-Z]+)([0-9]+)");
            if (m.Success) {
                received = new MemoryStream();
                command = m.Groups[1].Captures[0].Value;
                length = int.Parse(m.Groups[2].Captures[0].Value);
            } else {
                received.Write(buffer, (int)offset, (int)size);
                if (received.Length >= length) {
                    //var str = Space.Unzip(received);
                    var str = Encoding.UTF8.GetString(received.ToArray());
                    var d = SaveGame.Deserialize(str);
                    switch (command) {
                        case "WORLD":
                            game.World = (World)d;
                            break;
                        case "CAMERA":
                            game.camera = (XY)d;
                            break;
                    }
                    received.Close();
                }
            }
        }
        protected override void OnError(SocketError error) =>
            Debug.WriteLine($"Chat TCP client caught an error with code {error}");
    }

    class ClientScreen : Console {
        public TitleScreen prev;
        private GameClient client;
        public World World;
        public SpaceObject pov;
        public int povTimer;
        public List<Message> povDesc;
        XY screenCenter;
        public XY camera;
        public Dictionary<(int, int), ColoredGlyph> tiles = new Dictionary<(int, int), ColoredGlyph>();
        MouseWatch mouse = new MouseWatch();
        public ClientScreen(int width, int height, TitleScreen prev) : base(width, height) {
            this.prev = prev;
            this.World = prev.World;
            UseKeyboard = true;
            screenCenter = new XY(Width / 2, Height / 2);
            camera = prev.camera;
            client = new GameClient("127.0.0.1", 1111, this);
            client.ConnectAsync();
        }
        public override void Update(TimeSpan timeSpan) {
            World.UpdateAdded();
            World.UpdateActive();
            World.UpdateRemoved();
            tiles.Clear();
            World.PlaceTiles(tiles);

            if (pov == null) {
                return;
            }
            //Smoothly move the camera to where it should be
            if ((camera - pov.position).magnitude < pov.velocity.magnitude / 15 + 1) {
                camera = pov.position;
            } else {
                var step = (pov.position - camera) / 15;
                if (step.magnitude < 1) {
                    step = step.normal;
                }
                camera += step;
            }
        }
        public override void Render(TimeSpan drawTime) {
            this.Clear();

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var g = this.GetGlyph(x, y);

                    var offset = new XY(x, Height - y) - new XY(Width / 2, Height / 2);
                    var location = camera + offset;
                    if (g == 0 || g == ' ' || this.GetForeground(x, y).A == 0) {


                        if (tiles.TryGetValue(location.roundDown, out var tile)) {
                            if (tile.Background == Color.Transparent) {
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
        public override bool ProcessMouse(MouseScreenObjectState state) {
            mouse.Update(state, IsMouseOver);
            mouse.nowPos = new Point(mouse.nowPos.X, Height - mouse.nowPos.Y);
            if (mouse.left == ClickState.Held) {
                camera += new XY(mouse.prevPos - mouse.nowPos);
            }

            return base.ProcessMouse(state);

        }
        public override bool ProcessKeyboard(Keyboard info) {
            if (info.IsKeyPressed(Keys.K)) {
                if (pov.active) {
                    pov.Destroy(pov);
                }
            }

            if (info.IsKeyPressed(Keys.Escape)) {
                client.Disconnect();
                prev.pov = null;
                prev.camera = camera;
                SadConsole.Game.Instance.Screen = prev;
                prev.IsFocused = true;
            }
            return base.ProcessKeyboard(info);
        }
    }
}