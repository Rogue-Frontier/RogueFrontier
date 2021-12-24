﻿using SadRogue.Primitives;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Console = SadConsole.Console;

using static UI;
using Common;
using ArchConsole;
namespace RogueFrontier;

class ShipScreen : Console {
    public Console prev;
    public PlayerShip playerShip;
    public PlayerStory story;
    //Idea: Show an ASCII-art map of the ship where the player can walk around
    public ShipScreen(Console prev, PlayerShip playerShip, PlayerStory story) : base(prev.Width, prev.Height) {
        this.prev = prev;

        this.playerShip = playerShip;
        this.story = story;

        int x = 1, y = Height - 9;
        Children.Add(new LabelButton("[A] Active Devices", ShowPower) { Position = (x, y++) });
        Children.Add(new LabelButton("[C] Cargo", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[D] Devices", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[I] Invoke Items", ShowInvokable) { Position = (x, y++) });
        Children.Add(new LabelButton("[M] Missions", ShowMissions) { Position = (x, y++) });
        Children.Add(new LabelButton("[R] Refuel", ShowRefuel) { Position = (x, y++) });
    }
    public override void Render(TimeSpan delta) {

        //back.Render(delta);



        this.Clear();
        this.RenderBackground();
        var name = playerShip.shipClass.name;
        var x = Width / 4 - name.Length / 2;
        var y = 4;

        void Print(int x, int y, string s) =>
            this.Print(x, y, s, Color.White, Color.Black);
        void Print2(int x, int y, string s) =>
            this.Print(x, y, s, Color.White, Color.Black.SetAlpha(102));

        Print(x, y, name);


        var map = playerShip.shipClass.playerSettings?.map ?? new string[] { "" };
        x = Math.Max(0, Width / 4 - map.Select(line => line.Length).Max() / 2);
        y = 2;

        int width = map.Max(l => l.Length);
        foreach (var line in map) {
            var l = line.PadRight(width);
            Print2(x, y++, l);
            Print2(x, y++, l);
        }
        y++;

        x = 1;
        Print(x, y, $"{$"Thrust:    {playerShip.shipClass.thrust}",-16}{$"Rotate acceleration: {playerShip.shipClass.rotationAccel,3} deg/s^2"}");
        y++;
        Print(x, y, $"{$"Max Speed: {playerShip.shipClass.maxSpeed}",-16}{$"Rotate deceleration: {playerShip.shipClass.rotationDecel,3} deg/s^2"}");
        y++;
        Print(x, y, $"{"",-16}{$"Rotate max speed:    {playerShip.shipClass.rotationMaxSpeed * 30,3} deg/s^2"}");

        x = Width / 2;
        y = 2;

        var pl = playerShip.player;
        Print(x, y++, "[Player]");
        Print(x, y++, $"Name:       {pl.name}");
        Print(x, y++, $"Identity:   {pl.Genome.name}");
        Print(x, y++, $"Money:      {pl.money}");
        Print(x, y++, $"Title:      Harmless");
        y++;
        var reactors = playerShip.ship.devices.Reactors;
        if (reactors.Any()) {
            Print(x, y++, "[Reactors]");
            foreach (var r in reactors) {
                Print(x, y++, $"{r.source.type.name}");

                Print(x, y++, $"Output:     {-r.energyDelta}");
                Print(x, y++, $"Max output: {r.desc.maxOutput}");
                Print(x, y++, $"Fuel:       {r.energy:0}");
                Print(x, y++, $"Max fuel:   {r.desc.capacity}");


                y++;
            }
        }


        var ds = playerShip.ship.damageSystem;
        if (ds is HPSystem hp) {
            Print(x, y++, "[Health]");
            Print(x, y++, $"HP: {hp.hp}");
            y++;
        } else if (ds is LayeredArmorSystem las) {
            Print(x, y++, "[Armor]");
            foreach (var a in las.layers) {
                Print(x, y++, $"{a.source.type.name}: {a.hp} / {a.desc.maxHP}");
            }
            y++;
        }

        var weapons = playerShip.ship.devices.Weapons;
        if (weapons.Any()) {
            Print(x, y++, "[Weapons]");
            foreach (var w in weapons) {
                Print(x, y++, $"{w.source.type.name,-32}{w.GetBar()}");
                Print(x, y++, $"Projectile damage:      {w.desc.damageHP.str}");
                Print(x, y++, $"Projectile speed:       {w.desc.missileSpeed}");
                Print(x, y++, $"Shots per second: {60f / w.desc.fireCooldown}");

                if (w.ammo is Weapon.ChargeAmmo c) {
                    Print(x, y++, $"Ammo: ${c.charges}");
                }

                y++;
            }
        }

        var misc = playerShip.ship.devices.Installed.OfType<MiscDevice>();
        if (misc.Any()) {
            Print(x, y++, "[Misc]");
            foreach (var m in misc) {
                Print(x, y++, $"{m.source.type.name}");
                y++;
            }
        }

        if (playerShip.messages.Any()) {
            Print(x, y++, "[Messages]");
            foreach (var m in playerShip.messages) {
                this.Print(x, y++, m.Draw());
            }
            y++;
        }
        /*
        foreach(var item in PlayerShip.ship.Items) {

        }
        */

        base.Render(delta);
    }
    public override bool ProcessKeyboard(Keyboard info) {

        Predicate<Keys> pr = info.IsKeyPressed;
        if (pr(Keys.S) || pr(Keys.Escape)) {
            prev.IsFocused = true;
            Parent.Children.Remove(this);
        } else if (pr(Keys.A)) {
            ShowPower();
        } else if (pr(Keys.C)) {
            ShowCargo();
        } else if (pr(Keys.D)) {
            ShowLoadout();
        } else if (pr(Keys.I)) {
            ShowInvokable();
        } else if (pr(Keys.L)) {
            ShowLogs();
        } else if (pr(Keys.M)) {
            ShowMissions();
        } else if (pr(Keys.R)) {
            ShowRefuel();
        }
        return base.ProcessKeyboard(info);
    }
    public void ShowInvokable() => Transition(SListScreen.InvokableScreen(this, playerShip));
    public void ShowPower() => Transition(SListScreen.PowerScreen(this, playerShip));
    public void ShowCargo() => Transition(SListScreen.CargoScreen(this, playerShip));
    public void ShowLoadout() => Transition(SListScreen.LoadoutScreen(this, playerShip));

    public void ShowLogs() => Transition(SListScreen.LogScreen(this, playerShip));
    public void ShowMissions() => Transition(SListScreen.MissionScreen(this, playerShip, story));
    public void ShowRefuel() => Transition(SListScreen.RefuelScreen1(this, playerShip));
    public void Transition(Console s) {
        Parent.Children.Add(s);
        Parent.Children.Remove(this);
        s.IsFocused = true;
    }
}
public class SListScreen {
    public static ListScreen<IPlayerMessage> LogScreen(Console prev, PlayerShip player) {
        ListScreen<IPlayerMessage> screen = null;
        List<IPlayerMessage> logs = player.logs;

        return screen = new(prev,
            player,
            logs,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(IPlayerMessage i) => i switch {
            Message m => m.text,
            Transmission t => $"{t.text}"
        };
        List<ColoredString> GetDesc(IPlayerMessage i) {
            return i switch {
                Message m => new(),
                Transmission t => new() {
                    new ColoredString("Source: ") + t.source.name,
                }
            };
        }
        void Invoke(IPlayerMessage item) {
            screen.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<IPlayerInteraction> MissionScreen(Console prev, PlayerShip player, PlayerStory story) {
        ListScreen<IPlayerInteraction> screen = null;
        List<IPlayerInteraction> missions = new();
        void UpdateList() {
            missions.Clear();
            missions.AddRange(story.mainInteractions);
            missions.AddRange(story.secondaryInteractions);
            missions.AddRange(story.completedInteractions);
        }
        UpdateList();

        return screen = new(prev,
            player,
            missions,
            GetName,
            GetDesc,
            Invoke,
            Escape
            );

        string GetName(IPlayerInteraction i) => i switch {
            DestroyTarget dt => "Destroy Target",
            _ => "Mission"
        };
        List<ColoredString> GetDesc(IPlayerInteraction i) {
            List<ColoredString> result = new();
            switch (i) {
                case DestroyTarget dt:
                    if (dt.complete) {
                        result.Add(new($"Mission complete"));
                        result.Add(new($"Return to {dt.source.name}"));
                    } else {
                        result.Add(new("Destroy the following targets:"));
                        foreach (var t in dt.targets) {
                            result.Add(new($"- {t.name}"));
                        }
                        result.Add(new(""));
                        result.Add(new($"Source: {dt.source.name}"));
                    }
                    result.Add(new(""));
                    result.Add(new($"[Enter] Update targets", Color.Yellow, Color.Black));
                    break;
            }
            return result;
        }
        void Invoke(IPlayerInteraction item) {
            screen.UpdateIndex();
            switch (item) {
                case DestroyTarget dt:
                    var a = dt.targets.Where(t => t.active);
                    player.SetTargetList(a.Any() ? a.ToList() : new() { dt.source });
                    player.AddMessage(new Message("Targeting updated"));
                    break;
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Item> InvokableScreen(Console prev, PlayerShip player) {
        ListScreen<Item> screen = null;
        IEnumerable<Item> cargoInvokable;
        IEnumerable<Item> installedInvokable;
        List<Item> usable = new();
        void UpdateList() {
            cargoInvokable = player.cargo.Where(i => i.type.invoke != null);
            installedInvokable = player.devices.Installed.Select(d => d.source).Where(i => i.type.invoke != null);
            usable.Clear();
            usable.AddRange(installedInvokable.Concat(cargoInvokable));
        }
        UpdateList();

        return screen = new ListScreen<Item>(prev,
            player,
            usable,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installedInvokable.Contains(i) ? "Equip> " : "Cargo> ")}{i.type.name}";
        List<ColoredString> GetDesc(Item item) {
            var invoke = item.type.invoke;
            List<ColoredString> result = new();
            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }
            if (invoke != null) {
                string action = $"[Enter] {invoke.GetDesc(player, item)}";
                result.Add(new(action, Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item item) {
            item.type.invoke?.Invoke(screen, player, item, Update);
            screen.UpdateIndex();
        }
        void Update() {
            UpdateList();
            screen.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Device> LoadoutScreen(Console prev, PlayerShip player) {
        ListScreen<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );
        string GetName(Device d) => d.source.type.name;
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.invoke;
            List<ColoredString> result = new();
            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }

            if (invoke != null) {
                result.Add(new($"[Enter] {invoke.GetDesc(player, item)}", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeDevice(Device device) {
            var item = device.source;
            var invoke = item.type.invoke;

            invoke?.Invoke(screen, player, item);
            screen.UpdateIndex();
        }

        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Item> CargoScreen(Console prev, PlayerShip player) {
        ListScreen<Item> screen = null;
        var items = player.cargo;
        return screen = new ListScreen<Item>(prev,
            player,
            items,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => i.type.name;
        List<ColoredString> GetDesc(Item item) {
            var invoke = item.type.invoke;
            var result = new List<ColoredString>();
            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }
            if (invoke != null) {
                result.Add(new($"[Enter] {invoke.GetDesc(player, item)}", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeItem(Item item) {
            var invoke = item.type.invoke;
            invoke?.Invoke(screen, player, item);
            screen.UpdateIndex();
        }

        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Powered> PowerScreen(Console prev, PlayerShip player) {
        ListScreen<Powered> screen = null;
        var disabled = player.energy.disabled;
        var powered = player.devices.Powered;
        return screen = new(prev,
            player,
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Powered i) => $"{(disabled.Contains(i) ? "Disabled" : "Enabled")}> {i.source.type.name}";
        List<ColoredString> GetDesc(Powered p) {
            var result = new List<ColoredString>();
            var desc = p.source.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }
            var off = disabled.Contains(p);
            var word = (off ? "Enable" : "Disable");
            result.Add(new($"[Enter] {word} this device", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Powered p) {
            if (disabled.Contains(p)) {
                disabled.Remove(p);
                player.AddMessage(new Message($"Enabled {p.source.type.name}"));
            } else {
                disabled.Add(p);
                p.OnDisable();
                player.AddMessage(new Message($"Disabled {p.source.type.name}"));
            }
            screen.UpdateIndex();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Device> UninstallScreen(Console prev, PlayerShip player) {
        ListScreen<Device> screen = null;
        var devices = player.devices.Installed;
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            InvokeDevice,
            Escape
            );

        string GetName(Device d) => d.source.type.name;
        List<ColoredString> GetDesc(Device d) {
            var item = d.source;
            var invoke = item.type.invoke;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }

            if (invoke != null) {
                result.Add(new($"[Enter] Uninstall this device", Color.Yellow, Color.Black));
            }
            return result;
        }
        void InvokeDevice(Device device) {
            var item = device.source;
            player.devices.Remove(device);
            player.cargo.Add(item);
            screen.UpdateIndex();
        }

        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Armor> RepairArmorScreen(Console prev, PlayerShip player, Item source, RepairArmor repair, Action callback) {
        ListScreen<Armor> screen = null;
        var devices = (player.hull as LayeredArmorSystem).layers;
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Repair,
            Escape
            ) { IsFocused = true };

        string GetName(Armor d) => $"{$"[{d.hp} / {d.desc.maxHP}]",-12}{d.source.type.name}";
        List<ColoredString> GetDesc(Armor d) {
            var item = d.source;
            var invoke = item.type.invoke;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }

            if (d.hp < d.desc.maxHP) {
                result.Add(new("[Enter] Repair this armor", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This armor is at full HP", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Repair(Armor segment) {
            var before = segment.hp;
            var repairHP = Math.Min(repair.repairHP, segment.desc.maxHP - segment.hp);

            if (repairHP > 0) {
                segment.hp += repairHP;
                player.cargo.Remove(source);
                player.AddMessage(new Message($"Used {source.type.name} to restore {repairHP} hp on {segment.source.type.name}"));

                callback?.Invoke();
                Escape();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Reactor> RefuelReactor(Console prev, PlayerShip player, Item source, Refuel refuel, Action callback) {
        ListScreen<Reactor> screen = null;
        var devices = player.devices.Reactors;
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.invoke;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }

            if (r.energy < r.desc.capacity) {
                result.Add(new("[Enter] Refuel this reactor", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This reactor is at full capacity", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            var before = r.energy;
            var refuelEnergy = Math.Min(refuel.energy, r.desc.capacity - r.energy);

            if (refuelEnergy > 0) {
                r.energy += refuelEnergy;
                player.cargo.Remove(source);
                player.AddMessage(new Message($"Used {source.type.name} to refuel {refuelEnergy:0} energy on {r.source.type.name}"));

                callback?.Invoke();
                Escape();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }

    public static ListScreen<Reactor> RefuelScreen1(Console prev, PlayerShip player) {
        ListScreen<Reactor> screen = null;
        var devices = player.devices.Reactors;
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.invoke;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(32);
            if (desc.Any()) {
                result.AddRange(desc.Select(l => new ColoredString(l)));
                result.Add(new(""));
            }

            if (r.energy < r.desc.capacity) {
                result.Add(new("[Enter] Refuel this reactor", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This reactor is at full capacity", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            if (r.energy < r.desc.capacity) {
                var p = screen.Parent;
                p.Children.Remove(screen);
                p.Children.Add(RefuelScreen2(prev, player));
            }

            ListScreen<Item> RefuelScreen2(Console prev, PlayerShip player) {
                ListScreen<Item> screen = null;
                var items = player.cargo.Where(i => i.type.invoke is Refuel r);
                return screen = new(prev, player, items,
                    GetName, GetDesc, Invoke, Escape
                    ) { IsFocused = true };
                string GetName(Item i) => i.type.name;
                List<ColoredString> GetDesc(Item item) {
                    var result = new List<ColoredString>();
                    var desc = item.type.desc.SplitLine(32);
                    if (desc.Any()) {
                        result.AddRange(desc.Select(l => new ColoredString(l)));
                        result.Add(new(""));
                    }
                    result.Add(new($"Fuel amount: {(item.type.invoke as Refuel).energy}"));
                    result.Add(new(""));
                    result.Add(new(r.energy < r.desc.capacity ?
                        "[Enter] Use this item" : "Reactor is at full capacity",
                        Color.Yellow, Color.Black));
                    return result;
                }
                void Invoke(Item i) {
                    var refuel = i.type.invoke as Refuel;
                    var before = r.energy;
                    var refuelEnergy = Math.Min(refuel.energy, r.desc.capacity - r.energy);
                    if (refuelEnergy > 0) {
                        r.energy += refuelEnergy;
                        player.cargo.Remove(i);
                        player.AddMessage(new Message($"Used {i.type.name} to refuel {refuelEnergy:0} energy on {r.source.type.name}"));
                    }
                }
                void Escape() {
                    var p = screen.Parent;
                    p.Children.Remove(screen);
                    p.Children.Add(prev);
                    p.IsFocused = true;
                }
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }


}
public class ListScreen<T> : Console {
    Console prev;
    PlayerShip player;

    public bool groupMode = true;
    public IEnumerable<T> items;
    public IEnumerable<(T item, int count)> groups;
    public int count => groupMode ? groups.Count() : items.Count();
    public T currentItem => index.HasValue ? (groupMode ? groups.ElementAt(index.Value).item : items.ElementAt(index.Value)) : default;
    int? index;
    GetName getName;
    GetDesc getDesc;
    Invoke invoke;
    Escape escape;
    int tick;
    public delegate string GetName(T t);
    public delegate List<ColoredString> GetDesc(T t);
    public delegate void Invoke(T t);
    public delegate void Escape();
    public ListScreen(Console prev, PlayerShip player, IEnumerable<T> items, GetName getName, GetDesc getDesc, Invoke invoke, Escape escape) : base(prev.Width, prev.Height) {
        this.prev = prev;
        this.player = player;
        this.items = items;
        this.getName = getName;
        this.getDesc = getDesc;
        this.invoke = invoke;
        this.escape = escape;
        UpdateIndex();
    }
    public void UpdateGroups() {
        var l = items.ToList();
        groups = items.GroupBy(i => getName(i))
            .OrderBy(g => l.IndexOf(g.First()))
            .Select(g => (g.Last(), g.Count()))
            .ToHashSet();

    }
    public void UpdateIndex() {
        if (groupMode) UpdateGroups();
        index = count > 0 ? Math.Min(index ?? 0, count - 1) : null;
    }

    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.Up:
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 1, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageUp:
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 26, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.Down:
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 1, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageDown:
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 26, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.Enter:
                    var i = currentItem;
                    if (i != null) {
                        invoke(i);
                        UpdateIndex();
                    }
                    break;
                case Keys.Escape:
                    /*
                    var parent = Parent;
                    parent.Children.Remove(this);
                    prev.IsFocused = true;
                    */
                    escape();
                    break;
                default:
                    var ch = char.ToLower(key.Character);
                    if (ch >= 'a' && ch <= 'z') {
                        int start = Math.Max((index ?? 0) - 13, 0);
                        var letterIndex = start + letterToIndex(ch);
                        if (letterIndex == index) {
                            invoke(currentItem);
                            UpdateIndex();
                        } else if (letterIndex < count) {
                            //var item = items.ElementAt(letterIndex);
                            index = letterIndex;
                        }
                    }
                    break;
            }
        }
        return base.ProcessKeyboard(keyboard);
    }
    public override void Update(TimeSpan delta) {
        tick++;
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        int x = 16;
        int y = 16;

        void line(Point from, Point to, int glyph) {
            this.DrawLine(from, to, '-', Color.White, null);
        }

        this.RenderBackground();

        //this.Fill(new Rectangle(x, y, 32, 26), Color.Gray, null, '.');
        this.DrawBox(new Rectangle(x - 2, y - 3, 34, 3), new ColoredGlyph(Color.Yellow, Color.Black, '-'));
        this.Print(x, y - 2, player.name, Color.Yellow, Color.Black);
        int start = 0;
        int? highlight = null;
        if (index.HasValue) {
            start = Math.Max(index.Value - 16, 0);
            highlight = index;
        }

        Func<int, string> GetName = groupMode ? i => {
            var g = groups.ElementAt(i);
            return $"{g.count}x {getName(g.item)}";
        } : i => getName(items.ElementAt(i));

        int end = Math.Min(count, start + 26);
        if (count > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = GetName(i);
                if (n.Length > 26) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - 26);

                        n = n.Substring(index);
                        if (n.Length > 26) {
                            n = $"{n.Substring(0, 23)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, 23)}...";
                    }
                }
                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", highlightColor, Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                this.Print(x, y, name);

                i++;
                y++;
            }


            int height = 26;
            int barStart = (height * (start)) / count;
            int barEnd = (height * (end)) / count;
            int barX = x - 2;

            for (i = 0; i < height; i++) {
                ColoredGlyph cg = (i < barStart || i > barEnd) ?
                    new ColoredGlyph(Color.LightGray, Color.Black, '|') :
                    new ColoredGlyph(Color.White, Color.Black, '#');
                this.SetCellAppearance(barX, 16 + i, cg);
            }

            line(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-');
            barX += 33;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        } else {
            var highlightColor = Color.Yellow;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            this.Print(x, y, name);

            int barX = x - 2;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            line(new Point(barX, 16 + 26), new Point(barX + 33, 16 + 26), '-');
            barX += 33;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        }

        //this.DrawLine(new Point(x, y));

        y = Height - 16;
        foreach (var m in player.messages) {
            this.Print(x, y++, m.Draw());
        }

        x += 32 + 2;
        y = 14;
        var item = currentItem;
        if (item != null) {
            this.Print(x, y, getName(item), Color.Yellow, Color.Black);
            y += 2;
            foreach (var l in getDesc(item)) {
                this.Print(x, y++, l);
            }
        }

        base.Render(delta);
    }

}
