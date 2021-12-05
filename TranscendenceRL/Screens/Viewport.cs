﻿using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Console = SadConsole.Console;
namespace TranscendenceRL
{
    public class Viewport : Console {
		public Camera camera;
		public System world;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public Viewport(Console prev, Camera camera, System world) : base(prev.Width, prev.Height) {
			this.camera = camera;
			this.world = world;
			this.tiles = new();

        }
        public override void Update(TimeSpan delta) {
			tiles.Clear();
			world.PlaceTiles(tiles);
            base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
			this.Clear();
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			for (int x = -HalfViewWidth; x < HalfViewWidth; x++)
			{
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++)
				{
					XY location = camera.position + new XY(x, y).Rotate(camera.rotation);

					if (tiles.TryGetValue(location.roundDown, out var tile))
					{
						var xScreen = x + HalfViewWidth;
						var yScreen = HalfViewHeight - y;
						this.SetCellAppearance(xScreen, yScreen, tile);
					}
				}
			}
			base.Render(delta);
        }
		public ColoredGlyph GetTile(int x, int y) {
			XY location = camera.position + new XY(x-Width/2,y-Height/2).Rotate(camera.rotation);
			return tiles.TryGetValue(location.roundDown, out var tile) ? tile  :new ColoredGlyph(Color.Transparent, Color.Transparent);
        }
    }
}
