﻿using ExtSadConsole;
using Common;
using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace RogueFrontier.Screens;

class ShowroomModel {
    public List<BaseShip> available;
    public int shipIndex;
    public BaseShip current;

    public char[,] portrait;
}
class Showroom : ControlsConsole {
    private ref List<BaseShip> available => ref context.available;
    private ref int index => ref context.shipIndex;
    private Console prev;
    private ShowroomModel context;
    private Action<ShowroomModel> next;
    private LabelButton leftArrow, rightArrow;
    double time = 0;
    public Showroom(Console prev, List<BaseShip> available, Action<ShowroomModel> next) : base(prev.Width, prev.Height) {
        this.prev = prev;
        this.next = next;
        context = new() {
            available = available,
            shipIndex = 0,
            portrait = new char[8, 8]
        };
        int x = 2;
        int y = 2;
        Children.Add(new TextPainter(context.portrait) { Position = (x, y) });
        string back = "[Escape] Cancel";
        Children.Add(new LabelButton(back, Cancel) {
            Position = new Point(Width - back.Length, 1)
        });
        string start = "[Enter] Select";
        Children.Add(new LabelButton(start, Select) {
            Position = new Point(Width - start.Length, Height - 1)
        });
        UpdateArrows();
    }
    public override void Update(TimeSpan delta) {
        time += delta.TotalSeconds;
        base.Update(delta);
    }
    public override void Render(TimeSpan drawTime) {
        this.Clear();
        var current = available[index];
        int shipDescY = 12;
        shipDescY++;
        shipDescY++;
        var nameX = Width / 4 - current.name.Length / 2;
        var y = shipDescY;
        this.Print(nameX, y, current.name);
        var ps = current.shipClass.playerSettings;
        
        //Print label below image
        string s = "[Image is for promotional use only]";
        var strX = Width / 4 - s.Length / 2;
        this.Print(strX, y, s);
        //Print desc on right side
        var descX = Width * 2 / 4;
        y = shipDescY;
        foreach (var line in current.shipClass.playerSettings.description.Wrap(Width / 3)) {
            this.Print(descX, y, line);
            y++;
        }
        y++;
        //Show installed devices on the right pane
        this.Print(descX, y, "[Devices]");
        y++;
        foreach (var device in current.devices.Installed) {
            this.Print(descX + 4, y, device.source.type.name);
            y++;
        }
        for (y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                var g = this.GetGlyph(x, y);
                if (g == 0 || g == ' ') {
                    this.SetCellAppearance(x, y, new ColoredGlyph(
                        new Color(255, 255, 255, (int)(51 * Math.Sin(time * Math.Sin(x - y) + Math.Sin(x) * 5 + Math.Sin(y) * 5))),
                        Color.Black,
                        '='));
                }
            }
        }
        base.Render(drawTime);
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (info.IsKeyPressed(Keys.Right) && rightArrow.IsVisible) {
            SelectRight();
        }
        if (info.IsKeyPressed(Keys.Left) && leftArrow.IsVisible) {
            SelectLeft();
        }
        if (info.IsKeyPressed(Keys.Escape)) {
            Cancel();
        }
        if (info.IsKeyPressed(Keys.Enter)) {
            Select();
        }
        return base.ProcessKeyboard(info);
    }
    public void UpdateArrows() {
        leftArrow.IsVisible = index > 0;
        rightArrow.IsVisible = index < available.Count - 1;
    }
    public void SelectLeft() {
        index = (available.Count + index - 1) % available.Count;
        UpdateArrows();
    }
    public void SelectRight() {
        index = (index + 1) % available.Count;
        UpdateArrows();
    }
    public void Cancel() {
        IsFocused = false;
        Game.Instance.Screen = prev;
    }
    public void Select() {
        next(context);
    }
}