﻿using Common;
using SadConsole;
using SadRogue.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public class EffectParticle : Effect {
        public int Lifetime;
        public EffectParticle(XY Position, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = new XY();
            this.Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public EffectParticle(XY Position, XY Velocity, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = Velocity;
            this.Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public XY Position { get; set; }
        public XY Velocity { get; set; }

        public bool Active => Lifetime > 0;

        public ColoredGlyph Tile { get; private set; }

        public void Update() {
            Position += Velocity / TranscendenceRL.TICKS_PER_SECOND;
            Lifetime--;
        }
    }
    public class FadingTrail : Effect {
        private int Lifetime;
        public FadingTrail(XY Position, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = new XY();
            this._Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public FadingTrail(XY Position, XY Velocity, ColoredGlyph Tile, int Lifetime) {
            this.Position = Position;
            this.Velocity = Velocity;
            this._Tile = Tile;
            this.Lifetime = Lifetime;
        }
        public XY Position { get; private set; }
        public XY Velocity { get; private set; }

        public bool Active => Lifetime > 0;

        private ColoredGlyph _Tile;
        public ColoredGlyph Tile => new ColoredGlyph(_Tile.Foreground.WithValues(alpha: (int) (255 * Math.Min(1, 1f * Lifetime / TranscendenceRL.TICKS_PER_SECOND))),
            _Tile.Background.SetAlpha((byte)(192 + (63 * Math.Min(1, 1f * Lifetime / TranscendenceRL.TICKS_PER_SECOND)))).Premultiply(),
            _Tile.GlyphCharacter);

        public void Update() {
            Position += Velocity / TranscendenceRL.TICKS_PER_SECOND;
            Lifetime--;
        }
    }
}