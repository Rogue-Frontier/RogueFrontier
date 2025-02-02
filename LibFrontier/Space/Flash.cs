﻿using Common;
using LibGamer;
using RogueFrontier;
using System;
using System.Linq;
using System.Xml.Linq;
using Sys = RogueFrontier.World;
public record FlashDesc(){
    [Req] public int intensity;
    [Opt] public int lifetime = 60;
    public FlashDesc(XElement e) : this() {
        e.Initialize(this);
    }
    public void Create(Sys world, XY position) {
        var center = new Center(position, (int)(255 * Sqrt(intensity)), lifetime);
        world.AddEffect(center);
        int radius = (int)(Sqrt(intensity) * 1.5);
        var rng = Enumerable.Range(-radius * 2, radius * 2 * 2);
		var particles = rng
            .SelectMany(x => rng.Select(y => new XY(x, y)))
            .Where(p => (p - position).magnitude > radius)
            .Select(p => new Particle(center, center.position + p))
            .Where(p => p.active)
            .ToList();
        particles.ForEach(world.AddEffect);
    }
    public class Center : Effect {
        public XY position { get; set; }
        public int maxBrightness;
        public int maxLifetime;
        public double lifetime;
        public int brightness => (int) (maxBrightness * Sqrt(lifetime / maxLifetime));
        public bool active => brightness>128;
        public Tile tile => (ABGR.Transparent, ABGR.RGBA(255, 255, 255, (byte)brightness), ' ');
        public Center(XY position, int brightness, int lifetime) {
            this.position = position;
            this.maxBrightness = brightness;
            this.maxLifetime = lifetime;
            this.lifetime = lifetime;
        }
        public void Update(double delta) {
            lifetime -= delta * Constants.TICKS_PER_SECOND;
        }
    }
    public class Particle : Effect {
        public Center parent;
        public XY position { get; set; }
        public double distance;
        public byte brightness => (byte)Min(255, parent.brightness / distance);
        public bool active => brightness > 128 && parent.active;
        public Tile tile => delay > 0 ? null : (ABGR.Transparent, ABGR.RGBA(255, 255, 255, brightness), ' ');

        public double delay;
        public Particle(Center parent, XY position) {
            this.parent = parent;
            this.position = position;
            distance = Max(1, (parent.position - position).magnitude);
            delay = distance / 64;
        }
        public void Update(double delta) {
            delay -= delta;
        }
    }
}
