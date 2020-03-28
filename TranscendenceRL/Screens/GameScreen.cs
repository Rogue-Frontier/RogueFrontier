﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.Xna.Framework;
using static Microsoft.Xna.Framework.Input.Keys;
using SadConsole;
using SadConsole.Input;
using SadConsole.Themes;

namespace TranscendenceRL {
	static class Themes {
		public static WindowTheme Main = new SadConsole.Themes.WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.White, Color.Black),
		};
		public static WindowTheme Sub = new WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.Transparent, Color.Transparent)
		};
	}
	class GameConsole : Window {
		private PlayerMain main;
		public GameConsole(int Width, int Height, TypeCollection types, ShipClass playerClass) : base(Width, Height) {
			Theme = new WindowTheme {
				ModalTint = Color.Transparent,
				FillStyle = new Cell(Color.White, Color.Black),
			};
			UseKeyboard = true;
			UseMouse = true;
			this.DebugInfo($"Width: {Width}", $"Height: {Height}");
			main = new PlayerMain(Width, Height, types, playerClass);
		}
		public override void Show(bool modal) {
			base.Show(modal);
			main.Show(true);
		}
		public override void Update(TimeSpan delta) {
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
			this.DebugInfo($"Draw({delta})");
			Clear();

			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;
			//var i = 0;
			for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XY location = main.camera + new XY(x, y);

					var xScreen = x + HalfViewWidth;
					//var xScreen = x;
					var yScreen = ViewHeight - (y + HalfViewHeight);
					Print(xScreen, yScreen, main.GetTile(location));
				}
				//i++;
				//Print(main.camera.xi + x + HalfViewWidth, 0, new ColoredGlyph('0' + i/10, Color.White, Color.Transparent));
				//Print(0, 0, new ColoredString(main.camera.xi.ToString(), new Cell(Color.White, Color.Black)));
			}
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			return base.ProcessKeyboard(info);
		}
	}
	class PlayerMain : Window {
		public XY camera;
		public World world;
		public GeneratedGrid<Color> backSpace;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerShip player;
		public PlayerMain(int Width, int Height, TypeCollection types, ShipClass playerClass) : base(Width, Height) {
			camera = new XY();
			world = new World() {
				types = types
			};
			backSpace = new GeneratedGrid<Color>(p => {
				(var x, var y) = p;
				var value = world.karma.Next(28);
				var c = new Color(value, value, value + world.karma.Next(12));

				var init = new XY[] { new XY(1, 0), new XY(0, 1), new XY(0, -1), new XY(-1, 0) }.Select(xy => new XY(xy.xi + x, xy.yi + y)).Where(xy => backSpace.IsInit(xy.xi, xy.yi));

				var count = init.Count() + 1;
				foreach (var xy in init) {
					c = c.Add(backSpace.Get(xy.xi, xy.yi));
				}
				c = c.Divide(count);
				return c;
			});
			tiles = new Dictionary<(int, int), ColoredGlyph>();
			/*
			var shipClass =  new ShipClass() { thrust = 0.5, maxSpeed = 20, rotationAccel = 4, rotationDecel = 2, rotationMaxSpeed = 3 };
			world.AddEntity(player = new PlayerShip(new Ship(world, shipClass, new XY(0, 0))));
			world.AddEffect(new Heading(player));

			
			
			StationType stDaughters = new StationType() {
				name = "Daughters of the Orator",
				segments = new List<StationType.SegmentDesc> {
					new StationType.SegmentDesc(new XY(-1, 0), new StaticTile('<')),
					new StationType.SegmentDesc(new XY(1, 0), new StaticTile('>')),
				},
				tile = new StaticTile(new ColoredGlyph('S', Color.Pink, Color.Transparent))
			};
			var daughters = new Station(world, stDaughters, new XY(5, 5));
			world.AddEntity(daughters);
			*/
			/*
			player = new PlayerShip(new Ship(world, world.types.Lookup<ShipClass>("scAmethyst"), new XY(0, 0)));
			world.AddEntity(player);
			*/
			var playerSovereign = world.types.Lookup<Sovereign>("svPlayer");
			player = new PlayerShip(new Ship(world, playerClass, playerSovereign, new XY(0, 0)));
			world.AddEntity(player);
			var daughters = new Station(world, world.types.Lookup<StationType>("stDaughtersOutpost"), new XY(5, 5));
			world.AddEntity(daughters);

			player.messages.Add(new PlayerMessage("Welcome to Transcendence: Rogue Frontier!"));
		}
		public override void Update(TimeSpan delta) {
			tiles.Clear();
			foreach (var e in world.entities.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
					tiles[e.Position] = e.Tile;
				}
			}
			foreach (var e in world.effects.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
					tiles[e.Position] = e.Tile;
				}
			}

			camera = player.Position;

			world.entities.all.UnionWith(world.entitiesAdded);
			world.effects.all.UnionWith(world.effectsAdded);
			world.entitiesAdded.Clear();
			world.effectsAdded.Clear();
			world.entities.all.RemoveWhere(e => !e.Active);
			world.effects.all.RemoveWhere(e => !e.Active);
		}
		public override void Draw(TimeSpan drawTime) {
			var y = Height * 3 / 5;
			Clear();
			foreach (var message in player.messages) {
				var line = message.Draw();
				var x = Width * 3 / 4 - line.Count;
				Print(x, y, line);
				y++;
			}
			base.Draw(drawTime);
		}
		public ColoredGlyph GetTile(XY xy) {
			var back = GetBackTile(xy - (camera * 3) / 5);
			if (tiles.TryGetValue(xy, out ColoredGlyph g)) {
				if(g.Background == Color.Transparent) {
					g.Background = back.Background;
				}
				return g;
			} else {
				return back;
				//return GetBackTile(xy);
			}
		}
		public ColoredGlyph GetBackTile(XY xy) {
			//var value = backSpace.Get(xy - (camera * 3) / 4);
			var back = backSpace.Get(xy);
			return new ColoredGlyph(' ', Color.Transparent, back);
		}
		public override bool ProcessKeyboard(Keyboard info) {
			if(info.IsKeyDown(Up)) {
				player.SetThrusting();
			}
			if (info.IsKeyDown(Left)) {
				player.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(Right)) {
				player.SetRotating(Rotating.CW);
			}
			if(info.IsKeyDown(Down)) {
				player.SetDecelerating();
			}
			if(info.IsKeyDown(X)) {
				player.SetFiringPrimary();
			}
			return base.ProcessKeyboard(info);
		}
	}
}