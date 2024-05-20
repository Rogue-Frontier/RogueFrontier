﻿using LibGamer;
using RogueFrontier;
using SadConsole.Input;
using SadRogue.Primitives;
namespace SfmlFrontier;

public class GateTransition : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }

	Viewport back, front;
    double amount;
    Rectangle rect;
    public Action next;

    Sf sf;int Width => sf.Width;int Height => sf.Height;

    class Particle {
        public int lifetime;
        public Point pos;
        public Particle(int lifetime, Point pos) {
            this.lifetime = lifetime;
            this.pos = pos;
        }
    }
    private List<Particle> particles = new();

	public GateTransition(Viewport back, Viewport front, Action next) {
        this.back = back;
        this.front = front;
        rect = new(new(Width / 2, Height / 2), 0, 0);
        this.next = next;
    }
    public void HandleKey(Keyboard keyboard) {
        if (keyboard.IsKeyPressed(Keys.Enter)) {
            next();
        }
    }
    public void Update(TimeSpan delta) {
        back.Update(delta);
        amount += delta.TotalSeconds * 1;

        if (amount < 1) {
            rect = new Rectangle(new(Width / 2, Height / 2), (int)(amount * Width / 2), (int)(amount * Height / 2));
            particles.AddRange(rect.PerimeterPositions().Select(p => new Particle(15, p)));
            particles.ForEach(p => p.lifetime--);
            particles.RemoveAll(p => p.lifetime < 1);
        } else if(particles.Any()) {
            particles.ForEach(p => p.lifetime--);
            particles.RemoveAll(p => p.lifetime < 1);
        } else {
            next();
        }
    }
    public void Render(TimeSpan delta) {
        sf.Clear();
        var particleLayer = new Sf(Width, Height);
        particles.ForEach(p => {
            var pos = p.pos;
            particleLayer.SetBack(pos.X, pos.Y, ABGR.RGBA(255, 255, 255, (byte)(p.lifetime * 255 / 15)));
        });
        var _back = new Sf(Width, Height);
        if (front != null) {
            BackdropConsole prevBack = new(back);
            BackdropConsole nextBack = new(front);
            foreach (var y in Enumerable.Range(0, Height)) {
                foreach (var x in Enumerable.Range(0, Width)) {
                    Point p = new(x, y);
                    (var v, var b) = rect.Contains(p) ? (front, nextBack) : (back, prevBack);
                    _back.SetTile(x, y, b.GetTile(x, y));
                    var g = v.GetTile(x, y);
                    //var g = (rect.Contains(p) ? next : prev).GetCellAppearance(x, y);
                    sf.SetTile(x, Height - y, g);
                }
            }
        } else {
            var prevBack = new BackdropConsole(back);
            foreach (var y in Enumerable.Range(0, Height)) {
                foreach (var x in Enumerable.Range(0, Width)) {
                    var p = new Point(x, y);
                    if (rect.Contains(p)) {
                        sf.SetTile(x, Height - y, new Tile(ABGR.Black, ABGR.Black, 0));
                    } else {
                        (var v, var b) = (back, prevBack);
                        _back.SetTile(x, y, b.GetTile(x, y));
                        var g = v.GetTile(x, y);
                        sf.SetTile(x, Height - y, g);
                    }
                }
            }
        }
        Draw(_back);
        Draw(sf);
        Draw(particleLayer);
    }
}
