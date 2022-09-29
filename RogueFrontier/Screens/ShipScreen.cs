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
using SFML.Audio;
using CloudJumper;

namespace RogueFrontier;

class ShipScreen : ScreenSurface {
    public ScreenSurface prev;
    public PlayerShip playerShip;
    public PlayerStory story;
    //Idea: Show an ASCII-art map of the ship where the player can walk around
    public ShipScreen(ScreenSurface prev, PlayerShip playerShip, PlayerStory story) : base(prev.Surface.Width, prev.Surface.Height) {
        this.prev = prev;

        this.playerShip = playerShip;
        this.story = story;

        int x = 1, y = Surface.Height - 9;
        Children.Add(new LabelButton("[A] Active Devices", ShowPower) { Position = (x, y++) });
        Children.Add(new LabelButton("[C] Cargo", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[D] Devices", ShowCargo) { Position = (x, y++) });
        Children.Add(new LabelButton("[I] Invoke Items", ShowInvokable) { Position = (x, y++) });
        Children.Add(new LabelButton("[M] Missions", ShowMissions) { Position = (x, y++) });
        Children.Add(new LabelButton("[R] Refuel", ShowRefuel) { Position = (x, y++) });
    }
    public override void Render(TimeSpan delta) {

        //back.Render(delta);



        Surface.Clear();
        this.RenderBackground();
        var name = playerShip.shipClass.name;
        var x = Surface.Width / 4 - name.Length / 2;
        var y = 4;

        void Print(int x, int y, string s) =>
            Surface.Print(x, y, s, Color.White, Color.Black);
        void Print2(int x, int y, string s) =>
            Surface.Print(x, y, s, Color.White, Color.Black.SetAlpha(102));

        Print(x, y, name);


        var map = playerShip.shipClass.playerSettings?.map ?? new string[] { "" };
        x = Math.Max(0, Surface.Width / 4 - map.Select(line => line.Length).Max() / 2);
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

        x = Surface.Width / 2;
        y = 2;

        var pl = playerShip.person;
        Print(x, y++, "[Player]");
        Print(x, y++, $"Name:       {pl.name}");
        Print(x, y++, $"Identity:   {pl.Genome.name}");
        Print(x, y++, $"Money:      {pl.money}");
        Print(x, y++, $"Title:      Harmless");
        y++;
        var reactors = playerShip.ship.devices.Reactor;
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
        if (ds is HP hp) {
            Print(x, y++, "[Health]");
            Print(x, y++, $"HP: {hp.hp}");
            y++;
        } else if (ds is LayeredArmor las) {
            Print(x, y++, "[Armor]");
            foreach (var a in las.layers) {
                Print(x, y++, $"{a.source.type.name}: {a.hp} / {a.maxHP}");
            }
            y++;
        }

        var weapons = playerShip.ship.devices.Weapon;
        if (weapons.Any()) {
            Print(x, y++, "[Weapons]");
            foreach (var w in weapons) {
                Print(x, y++, $"{w.source.type.name,-32}{w.GetBar(8)}");
                Print(x, y++, $"Projectile damage: {w.desc.damageHP.str}");
                Print(x, y++, $"Projectile speed:  {w.desc.missileSpeed}");
                Print(x, y++, $"Shots per second:  {60f / w.desc.fireCooldown}");

                if (w.ammo is ChargeAmmo c) {
                    Print(x, y++, $"Ammo Remaining:    {c.charges}");
                }
                y++;
            }
        }

        var misc = playerShip.ship.devices.Installed.OfType<Service>();
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
                Surface.Print(x, y++, m.Draw());
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
            Tones.pressed.Play();
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
    public void ShowInvokable() => Transition(SListScreen.UsableScreen(this, playerShip));
    public void ShowPower() => Transition(SListScreen.PowerScreen(this, playerShip));
    public void ShowCargo() => Transition(SListScreen.CargoScreen(this, playerShip));
    public void ShowLoadout() => Transition(SListScreen.LoadoutScreen(this, playerShip));

    public void ShowLogs() => Transition(SListScreen.LogScreen(this, playerShip));
    public void ShowMissions() => Transition(SListScreen.MissionScreen(this, playerShip, story));
    public void ShowRefuel() => Transition(SListScreen.RefuelScreen1(this, playerShip));
    public void Transition(ScreenSurface s) {
        Tones.pressed.Play();
        Parent.Children.Add(s);
        Parent.Children.Remove(this);
        s.IsFocused = true;
    }
}
public class SListScreen {
    public static ListScreen<IPlayerMessage> LogScreen(ScreenSurface prev, PlayerShip player) {
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
            Transmission t => $"{t.text}",
            _ => throw new NotImplementedException()
        };
        List<ColoredString> GetDesc(IPlayerMessage i) {
            return i switch {
                Message m => new(),
                Transmission t => new() {
                    new ColoredString("Source: ") + (t.source as ActiveObject)?.name ?? new("N/A"),
                },
                _ => throw new NotImplementedException()
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
    public static ListScreen<IPlayerInteraction> MissionScreen(ScreenSurface prev, PlayerShip player, PlayerStory story) {
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
    public static ListScreen<Item> UsableScreen(ScreenSurface prev, PlayerShip player) {
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
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
            if (prev != null && prev != p) {
                p.Children.Add(prev);
            }
            p.IsFocused = true;
        }
    }
    public static ListScreen<Device> LoadoutScreen(ScreenSurface prev, PlayerShip player) {
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
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
    public static ListScreen<Item> CargoScreen(ScreenSurface prev, PlayerShip player) {
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
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
    public static ListScreen<Device> PowerScreen(ScreenSurface prev, PlayerShip player) {
        ListScreen<Device> screen = null;
        var disabled = player.energy.off;
        var powered = player.devices.Powered;
        return screen = new(prev,
            player,
            powered,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Device i) => $"{(disabled.Contains(i) ? " " : "*")} {i.source.type.name}";
        List<ColoredString> GetDesc(Device p) {
            var result = new List<ColoredString>();
            var desc = p.source.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            var off = disabled.Contains(p);
            var word = (off ? "Enable" : "Disable");
            result.Add(new($"[Enter] {word} this device", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Device p) {
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
    public static ListScreen<Device> UninstallScreen(ScreenSurface prev, PlayerShip player) {
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

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
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
    public static ListScreen<Armor> RepairArmorScreen(ScreenSurface prev, PlayerShip player, Item source, RepairArmor repair, Action callback) {
        ListScreen<Armor> screen = null;
        Sound s = new();


        var devices = (player.hull as LayeredArmor).layers;

        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Repair,
            Escape
            ) { IsFocused = true };

        string GetName(Armor d) => $"{$"[{d.hp} / {d.maxHP}]",-12}{d.source.type.name}";
        List<ColoredString> GetDesc(Armor d) {
            var item = d.source;
            var invoke = item.type.invoke;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            if (d.desc.restrictRepair?.Matches(source) == false) {
                result.Add(new("This armor is not compatible", Color.Yellow, Color.Black));
            } else if (d.hp < d.maxHP) {
                result.Add(new("[Enter] Repair this armor", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This armor is at full HP", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Repair(Armor segment) {
            if(segment.desc.restrictRepair?.Matches(source) == false) {
                return;
            }
            var before = segment.hp;
            var repairHP = Math.Min(repair.repairHP, segment.maxHP - segment.hp);
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
    public static ListScreen<Reactor> RefuelReactor(ScreenSurface prev, PlayerShip player, Item source, Refuel refuel, Action callback) {
        ListScreen<Reactor> screen = null;
        var devices = player.devices.Reactor;
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            result.Add(new($"Refuel amount: {refuel.energy}"));
            result.Add(new($"Fuel needed:   {r.desc.capacity - (int)r.energy}"));
            result.Add(new(""));
            if (r.energy < r.desc.capacity) {
                result.Add(new("[Enter] Refuel", Color.Yellow, Color.Black));
            } else {
                result.Add(new("This reactor is full", Color.Yellow, Color.Black));
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
    public static ListScreen<Device> ReplaceDevice(ScreenSurface prev, PlayerShip player, Item source, ReplaceDevice replace, Action callback) {
        ListScreen<Device> screen = null;
        var devices = player.devices.Installed.Where(i => i.source.type == replace.from);
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Device d) => $"{d.source.type.name}";
        List<ColoredString> GetDesc(Device r) {
            var item = r.source;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }

            result.Add(new("Replace this device", Color.Yellow, Color.Black));
            return result;
        }
        void Invoke(Device d) {
            d.source.type = replace.to;
            switch (d) {
                case Weapon w: w.SetWeaponDesc(replace.to.weapon); break;
                default:
                    throw new Exception("Unsupported ReplaceDevice type");
            }
            player.AddMessage(new Message($"Used {source.type.name} to replace {d.source.type.name} with {replace.to.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }


    public static ListScreen<Weapon> RechargeWeapon(ScreenSurface prev, PlayerShip player, Item source, RechargeWeapon recharge, Action callback) {
        ListScreen<Weapon> screen = null;
        var devices = player.devices.Weapon.Where(i => i.desc == recharge.weaponType);
        return screen = new(prev,
            player,
            devices,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Weapon d) => $"{d.source.type.name}";
        List<ColoredString> GetDesc(Weapon r) {
            var item = r.source;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }

            result.Add(new("Recharge this weapon", Color.Yellow, Color.Black));
            return result;
        }
        void Invoke(Weapon d) {
            var c = (d.ammo as ChargeAmmo);
            c.charges += recharge.charges;
            player.AddMessage(new Message($"Recharged {d.source.type.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }










    public static ListScreen<ItemType> Workshop(ScreenSurface prev, PlayerShip player, Dictionary<ItemType, Dictionary<ItemType, int>> recipes, Action callback) {
        ListScreen<ItemType> screen = null;
        var listing = new Dictionary<ItemType, Dictionary<ItemType, HashSet<Item>>>();
        var available = new Dictionary<ItemType, bool>();
        Calculate();


        void Calculate() {
            foreach ((var result, var rec) in recipes) {
                var components = new Dictionary<ItemType, HashSet<Item>>();
                foreach (var compType in rec.Keys) {
                    components[compType] = new();
                }
                foreach (var item in player.cargo) {
                    var type = item.type;
                    if (components.TryGetValue(type, out var set)) {
                        set.Add(item);
                    }
                }

                available[result] = rec.All(pair => components[pair.Key].Count >= pair.Value);
                listing[result] = components;
            }
        }
        return screen = new(prev,
            player,
            recipes.Keys,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };
        string GetName(ItemType type) => $"{type.name}";
        List<ColoredString> GetDesc(ItemType type) {
            var result = new List<ColoredString>();
            var desc = type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }



            var rec = recipes[type];
            foreach((var compType, var minCount) in rec) {
                var count = listing[type][compType].Count;
                result.Add(new($"{compType.name}: {count} / {minCount}", count >= minCount ? Color.Yellow : Color.Gray, Color.Black));
            }
            result.Add(new(""));

            if (available[type]) {
                result.Add(new("[Enter] Fabricate this item", Color.Yellow, Color.Black));
            } else {
                result.Add(new("Additional materials required", Color.Yellow, Color.Black));
            }

        Done:
            return result;
        }
        void Invoke(ItemType type) {
            if (available[type]) {
                foreach((var compType, var minCount) in recipes[type]) {
                    player.cargo.ExceptWith(listing[type][compType].Take(minCount));
                }
                player.cargo.Add(new(type));

                Calculate();
                callback?.Invoke();
            }
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }

















    public static ListScreen<Reactor> RefuelService(ScreenSurface prev, PlayerShip player, Func<Reactor, int> GetPrice, Action callback) {
        ListScreen<Reactor> screen = null;
        var reactors = player.devices.Reactor;
        RefuelEffect job = null;
        return screen = new(prev,
            player,
            reactors,
            GetName,
            GetDesc, Invoke, Escape) { IsFocused = true };
        string GetName(Reactor r) => $"{$"[{r.energy:0} / {r.desc.capacity}]",-12} {r.source.type.name}";
        List<ColoredString> GetDesc(Reactor r) {
            var item = r.source;
            var invoke = item.type.invoke;
            var result = new List<ColoredString>();
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            int unitPrice = GetPrice(r);
            if (unitPrice < 0) {
                result.Add(new("Refuel services not available for this reactor", Color.Yellow, Color.Black));
                return result;
            }
            var delta = r.desc.capacity - (int)r.energy;
            result.Add(new($"Fuel needed: {delta}"));
            result.Add(new($"Total cost:  {unitPrice * delta}"));
            result.Add(new($"Your money:  {player.person.money}"));
            result.Add(new(""));
            if (delta <= 0) {
                result.Add(new("This reactor is full", Color.Yellow, Color.Black));
            } else if(job?.active == true) {
                if(job.reactor == r) {
                    result.Add(new("This reactor is currently refueling.", Color.Yellow, Color.Black));
                } else {
                    result.Add(new("Please wait for current refuel job to finish.", Color.Yellow, Color.Black));
                }
            } else if(unitPrice > player.person.money) {
                result.Add(new($"You cannot afford refueling", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"[Enter] Order refueling", Color.Yellow, Color.Black));
            }
            return result;
        }
        void Invoke(Reactor r) {
            if (job?.active == true) {
                return;
            }
            int delta = r.desc.capacity - (int)r.energy;
            if (delta == 0) {
                return;
            }
            int unitPrice = GetPrice(r);
            int price = delta * unitPrice;
            if (unitPrice > player.person.money) {
                return;
            }
            job = new RefuelEffect(player, r, 6, unitPrice, Done);
            player.world.AddEvent(job);
            player.AddMessage(new Message($"Refuel job initiated..."));
            callback?.Invoke();
        }

        void Done(RefuelEffect r) {
            player.world.RemoveEvent(r);
            player.AddMessage(new Message($"Refuel job {(r.terminated ? "terminated" : "completed")}"));
        }
        void Escape() {
            if (job?.active == true) {
                job.active = false;
                player.world.RemoveEvent(job);
                job = null;
                player.AddMessage(new Message($"Refuel job canceled"));
                return;
            }
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Armor> ArmorRepairService(ScreenSurface prev, PlayerShip player, Func<Armor, int> GetPrice, Action callback) {
        ListScreen<Armor> screen = null;
        var layers = (player.hull as LayeredArmor)?.layers ?? new();
        RepairEffect job = null;

        Sound s = new();
        SoundBuffer
            start = new("RogueFrontierContent/sounds/repair_start.wav"),
            stop = new("RogueFrontierContent/sounds/repair_stop.wav");


        return screen = new(prev,
            player,
            layers,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };



        string GetName(Armor a) => $"[{a.hp}/{a.maxHP}] {a.source.type.name}";
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }


            int unitPrice = GetPrice(a);
            if (unitPrice < 0) {
                result.Add(new("Repair services not available for this armor", Color.Yellow, Color.Black));
                return result;
            }
            int delta = a.maxHP - a.hp;
            result.Add(new($"Price per HP: {unitPrice}"));
            result.Add(new($"HP to repair: {delta}"));
            result.Add(new($"Your money:   {player.person.money}"));
            result.Add(new($"Full price:   {unitPrice * delta}"));
            result.Add(new(""));
            if (delta <= 0) {
                result.Add(new("This armor is at full HP", Color.Yellow, Color.Black));
                goto Done;
            }
            if (job?.active == true) {
                if (job.armor == a) {
                    result.Add(new("This armor is currently under repairs", Color.Yellow, Color.Black));
                } else {
                    result.Add(new("Please wait for current repair job to finish", Color.Yellow, Color.Black));
                }
                goto Done;
            }
            if (unitPrice > player.person.money) {
                result.Add(new($"You cannot afford repairs", Color.Yellow, Color.Black));
                goto Done;
            }
            result.Add(new($"Order repairs", Color.Yellow, Color.Black));

        Done:
            return result;
        }
        void Invoke(Armor a) {
            if (job?.active == true) {
                if(job.armor == a) {
                    job.active = false;
                    player.AddMessage(new Message($"Repair job terminated."));

                    s.SoundBuffer = stop;
                    s.Play();
                }
                return;
            }
            int delta = a.maxHP - a.hp;
            if (delta == 0) {
                return;
            }
            int unitPrice = GetPrice(a);
            if(unitPrice < 0) {
                return;
            }


            if(unitPrice > player.person.money) {
                return;
            }
            job = new RepairEffect(player, a, 6, unitPrice, Done);
            player.world.AddEvent(job);
            player.AddMessage(new Message($"Repair job initiated..."));


            s.SoundBuffer = start;
            s.Play();

            callback?.Invoke();
        }
        void Done(RepairEffect r) {
            player.world.RemoveEvent(r);
            player.AddMessage(new Message($"Repair job {(r.terminated ? "terminated" : "completed")}"));

            s.SoundBuffer = stop;
            s.Play();
        }
        void Escape() {
            if(job?.active == true) {
                job.active = false;
                player.world.RemoveEvent(job);
                job = null;
                player.AddMessage(new Message($"Repair job canceled"));


                s.SoundBuffer = stop;
                s.Play();
                return;
            }
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }



    public static ListScreen<Device> DeviceRemovalService(ScreenSurface prev, PlayerShip player, Func<Device, int> GetPrice, Action callback) {
        ListScreen<Device> screen = null;
        var installed = player.devices.Installed;
        return screen = new(prev,
            player,
            installed,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Device a) => $"{a.source.type.name}";
        List<ColoredString> GetDesc(Device a) {
            var item = a.source;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }


            int unitPrice = GetPrice(a);
            if (unitPrice < 0) {
                result.Add(new("Removal services are unavailable for this device", Color.Yellow, Color.Black));
            } else {

                result.Add(new($"Removal fee: {unitPrice}"));
                result.Add(new($"Your money:  {player.person.money}"));
                result.Add(new(""));
                if (unitPrice > player.person.money) {
                    result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(new($"Remove device", Color.Yellow, Color.Black));
                }
            }
            return result;
        }
        void Invoke(Device a) {
            var price = GetPrice(a);
            ref var money = ref player.person.money;
            if (price > money) {
                return;
            }
            money -= price;
            player.devices.Remove(a);
            player.cargo.Add(a.source);
            player.AddMessage(new Message($"Removed {GetName(a)}"));
            callback?.Invoke();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }

    public static ListScreen<Item> DeviceInstallService(ScreenSurface prev, PlayerShip player, Func<Item, int> GetPrice, Action callback) {
        ListScreen<Item> screen = null;
        var cargo = player.cargo.Where(i => i.HasDevice());
        return screen = new(prev,
            player,
            cargo,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Item a) => $"{a.type.name}";
        List<ColoredString> GetDesc(Item a) {
            var item = a;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }


            int price = GetPrice(a);
            if (price < 0) {
                result.Add(new("Install services are unavailable for this device", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"Install fee: {price}"));
                result.Add(new($"Your money:  {player.person.money}"));
                result.Add(new(""));
                if (price > player.person.money) {
                    result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(new($"Install device", Color.Yellow, Color.Black));
                }
            }
            return result;
        }
        void Invoke(Item a) {
            var price = GetPrice(a);
            ref var money = ref player.person.money;
            if (price > money) {
                return;
            }
            money -= price;

            player.cargo.Remove(a);
            player.devices.Install(a.GetDevices());

            player.AddMessage(new Message($"Installed {GetName(a)}"));
            callback?.Invoke();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }

    public static ListScreen<Armor> ReplaceArmorService(ScreenSurface prev, PlayerShip player, Func<Armor, int> GetPrice, Action callback) {
        ListScreen<Armor> screen = null;
        var armor = (player.hull as LayeredArmor)?.layers??new List<Armor>();
        return screen = new(prev,
            player,
            armor,
            GetName,
            GetDesc,
            Invoke,
            Escape
            ) { IsFocused = true };

        string GetName(Armor a) => $"{a.source.type.name}";
        List<ColoredString> GetDesc(Armor a) {
            var item = a.source;

            var result = new List<ColoredString>();

            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }

            int price = GetPrice(a);
            if (price < 0) {
                result.Add(new("Removal services are unavailable for this armor", Color.Yellow, Color.Black));
            } else {
                result.Add(new($"Your money:  {player.person.money}"));
                result.Add(new($"Removal fee: {price}"));
                result.Add(new(""));
                if (price > player.person.money) {
                    result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                } else {
                    result.Add(new($"Select replacement", Color.Yellow, Color.Black));
                }
            }
            return result;
        }
        void Invoke(Armor removed) {
            var removalPrice = GetPrice(removed);
            ref var money = ref player.person.money;
            if (removalPrice > money) {
                return;
            }


            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(GetReplacement());
            ListScreen<Armor> GetReplacement() {
                ListScreen<Armor> screen = null;
                var armor = player.cargo.Select(i => i.armor).Where(i => i != null);
                return screen = new(prev,
                    player,
                    armor,
                    GetName,
                    GetDesc,
                    Invoke,
                    Escape
                    ) { IsFocused = true };
                string GetName(Armor a) => $"{a.source.type.name}";
                List<ColoredString> GetDesc(Armor a) {
                    var item = a.source;
                    var result = new List<ColoredString>();
                    var desc = item.type.desc.SplitLine(64);
                    if (desc.Any()) {
                        result.AddRange(desc.Select(Main.ToColoredString));
                        result.Add(new(""));
                    }


                    if (player.shipClass.restrictArmor?.Matches(a.source) == false) {
                        result.Add(new("This armor is not compatible", Color.Yellow, Color.Black));
                        return result;
                    }

                    int installPrice = GetPrice(a);
                    if (installPrice < 0) {
                        result.Add(new("Install services are unavailable for this armor", Color.Yellow, Color.Black));
                        return result;
                    }

                    var totalCost = removalPrice + installPrice;
                    result.Add(new($"Your money:  {player.person.money}"));
                    result.Add(new($"Removal fee: {removalPrice}"));
                    result.Add(new($"Install fee: {installPrice}"));
                    result.Add(new($"Total cost:  {totalCost}"));
                    result.Add(new(""));

                    if(totalCost > player.person.money) {
                        result.Add(new($"You cannot afford service", Color.Yellow, Color.Black));
                        return result;
                    }
                    result.Add(new($"Replace with this armor", Color.Yellow, Color.Black));
                    return result;
                }
                void Invoke(Armor installed) {
                    if (player.shipClass.restrictArmor?.Matches(installed.source) == false) {
                        return;
                    }
                    var price = removalPrice + GetPrice(installed);
                    ref var money = ref player.person.money;
                    if (price > money) {
                        return;
                    }
                    money -= price;
                    var l = ((LayeredArmor)player.hull).layers;
                    l[l.IndexOf(removed)] = installed;
                    player.cargo.Add(removed.source);
                    player.cargo.Remove(installed.source);

                    callback?.Invoke();

                    Escape();
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
    public static ListScreen<Item> SetMod(ScreenSurface prev, PlayerShip player, Item source, Modifier mod, Action callback) {
        ListScreen<Item> screen = null;
        IEnumerable<Item> cargo;
        IEnumerable<Item> installed;
        List<Item> all = new();
        void UpdateList() {
            cargo = player.cargo;
            installed = player.devices.Installed.Select(d => d.source);
            all.Clear();
            all.AddRange(installed.Concat(cargo));
            all.Remove(source);
        }
        UpdateList();

        return screen = new ListScreen<Item>(prev,
            player,
            all,
            GetName,
            GetDesc,
            InvokeItem,
            Escape
            );

        string GetName(Item i) => $"{(installed.Contains(i) ? "Equip> " : "Cargo> ")}{i.type.name}";
        List<ColoredString> GetDesc(Item item) {
            List<ColoredString> result = new();
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            result.Add(new("[Enter] Apply modifier", Color.Yellow, Color.Black));
            return result;
        }
        void InvokeItem(Item item) {
            item.mod = mod;
            player.cargo.Remove(source);
            player.AddMessage(new Message($"Applied {source.name} to {item.name}"));
            callback?.Invoke();
            Escape();
        }
        void Escape() {
            var p = screen.Parent;
            p.Children.Remove(screen);
            p.Children.Add(prev);
            p.IsFocused = true;
        }
    }
    public static ListScreen<Reactor> RefuelScreen1(ScreenSurface prev, PlayerShip player) {
        ListScreen<Reactor> screen = null;
        var devices = player.devices.Reactor;
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
                result.Add(new(""));
            }
            var a = (string s) => result.Add(new(s));
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
            ListScreen<Item> RefuelScreen2(ScreenSurface prev, PlayerShip player) {
                ListScreen<Item> screen = null;
                var items = player.cargo.Where(i => i.type.invoke is Refuel r);
                return screen = new(prev, player, items,
                    GetName, GetDesc, Invoke, Escape
                    ) { IsFocused = true };
                string GetName(Item i) => i.type.name;
                List<ColoredString> GetDesc(Item item) {
                    var result = new List<ColoredString>();
                    var desc = item.type.desc.SplitLine(64);
                    if (desc.Any()) {
                        result.AddRange(desc.Select(Main.ToColoredString));
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
public class ListScreen<T> : ScreenSurface {
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

    public ListScreen(ScreenSurface prev, PlayerShip player, IEnumerable<T> items, GetName getName, GetDesc getDesc, Invoke invoke, Escape escape) : base(prev.Surface.Width, prev.Surface.Height) {
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
        tick = 0;
    }

    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var key in keyboard.KeysPressed) {
            switch (key.Key) {
                case Keys.Up:
                    Tones.pressed.Play();
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 1, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageUp:
                    Tones.pressed.Play();
                    index = count > 0 ?
                        (index == null ? (count - 1) :
                            index == 0 ? null :
                            Math.Max(index.Value - 26, 0))
                        : null;
                    tick = 0;
                    break;
                case Keys.Down:
                    Tones.pressed.Play();
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 1, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.PageDown:
                    Tones.pressed.Play();
                    index = count > 0 ?
                        (index == null ? 0 :
                            index == count - 1 ? null :
                            Math.Min(index.Value + 26, count - 1))
                        : null;
                    tick = 0;
                    break;
                case Keys.Enter:
                    Tones.pressed.Play();
                    var i = currentItem;
                    if (i != null) {
                        invoke(i);
                        UpdateIndex();
                    }
                    break;
                case Keys.Escape:
                    Tones.pressed.Play();
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
                        Tones.pressed.Play();
                        int start = Math.Max((index ?? 0) - 13, 0);
                        var letterIndex = start + letterToIndex(ch);
                        if (letterIndex == index) {
                            invoke(currentItem);
                            UpdateIndex();
                        } else if (letterIndex < count) {
                            //var item = items.ElementAt(letterIndex);
                            index = letterIndex;
                            tick = 0;
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
        int x = 6;
        int y = 16;

        void line(Point from, Point to, int glyph) {
            Surface.DrawLine(from, to, '-', Color.White, null);
        }
        this.RenderBackground();
        //this.Fill(new Rectangle(x, y, 32, 26), Color.Gray, null, '.');
        const int lineWidth = 36;
        Surface.DrawBox(new Rectangle(x - 2, y - 3, lineWidth + 8, 3), new ColoredGlyph(Color.Yellow, Color.Black, '-'));
        Surface.Print(x, y - 2, player.name, Color.Yellow, Color.Black);
        int start = 0;
        int? highlight = null;
        if (index.HasValue) {
            start = Math.Max(index.Value - 16, 0);
            highlight = index;
        }
        Func<int, string> NameAt = groupMode ? i => {
            var g = groups.ElementAt(i);
            return $"{g.count}x {getName(g.item)}";
        } : i => getName(items.ElementAt(i));
        int end = Math.Min(count, start + 26);

        if (count > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = NameAt(i);
                if (n.Length > lineWidth) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - lineWidth);

                        n = n.Substring(index);
                        if (n.Length > lineWidth) {
                            n = $"{n.Substring(0, lineWidth-3)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, lineWidth-3)}...";
                    }
                }
                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", highlightColor, Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                Surface.Print(x, y, name);
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
                Surface.SetCellAppearance(barX, 16 + i, cg);
            }

            line(new Point(barX, 16 + 26), new Point(barX + lineWidth + 7, 16 + 26), '-');
            barX += lineWidth + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        } else {
            var highlightColor = Color.Yellow;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            Surface.Print(x, y, name);

            int barX = x - 2;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            line(new Point(barX, 16 + 26), new Point(barX + lineWidth + 7, 16 + 26), '-');
            barX += lineWidth + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        }
        //this.DrawLine(new Point(x, y));
        y = Surface.Height - 16;
        foreach (var m in player.messages) {
            Surface.Print(x, y++, m.Draw());
        }
        x += lineWidth + 7 + 1;
        y = 14;
        var item = currentItem;
        if (item != null) {
            foreach(var l in getName(item).SplitLine(64)) {
                Surface.Print(x, y++, l, Color.Yellow, Color.Black);
            }
            y++;
            foreach (var l in getDesc(item)) {
                Surface.Print(x, y++, l);
            }
        }
        base.Render(delta);
    }
}
public static class SListWidget {
    public static ListWidget<Item> InvokableWidget(ScreenSurface prev, PlayerShip player) {
        ListWidget<Item> screen = null;
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

        return screen = new(prev,
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
            var desc = item.type.desc.SplitLine(64);
            if (desc.Any()) {
                result.AddRange(desc.Select(Main.ToColoredString));
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
            p.IsFocused = true;
        }
    }
}
public class ListWidget<T> : ScreenSurface {
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
    public ListWidget(ScreenSurface prev, PlayerShip player, IEnumerable<T> items, GetName getName, GetDesc getDesc, Invoke invoke, Escape escape) : base(prev.Surface.Width, prev.Surface.Height) {
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
        tick = 0;
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
                            tick = 0;
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
        int x = 5;
        int y = 16;

        void line(Point from, Point to, int glyph) {
            Surface.DrawLine(from, to, '-', Color.White, null);
        }
        const int WIDTH = 36;
        Surface.Clear();
        int start = 0;
        int? highlight = null;
        if (index.HasValue) {
            start = Math.Max(index.Value - 16, 0);
            highlight = index;
        }

        Func<int, string> NameAt = groupMode ? i => {
            var g = groups.ElementAt(i);
            return $"{g.count}x {getName(g.item)}";
        }
        : i => getName(items.ElementAt(i));

        int end = Math.Min(count, start + 26);


        if (count > 0) {
            int i = start;
            while (i < end) {
                var highlightColor = i == highlight ? Color.Yellow : Color.White;
                var n = NameAt(i);
                if (n.Length > WIDTH) {
                    if (i == highlight) {
                        //((tick / 15) % (n.Length - 25));
                        int initialDelay = 60;
                        int index = tick < initialDelay ? 0 : Math.Min((tick - initialDelay) / 15, n.Length - WIDTH);

                        n = n.Substring(index);
                        if (n.Length > WIDTH) {
                            n = $"{n.Substring(0, WIDTH-3)}...";
                        }
                    } else {
                        n = $"{n.Substring(0, WIDTH-3)}...";
                    }
                }
                var name = new ColoredString($"{UI.indexToLetter(i - start)}. ", highlightColor, Color.Black)
                         + new ColoredString(n, highlightColor, Color.Black);
                Surface.Print(x, y, name);

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
                Surface.SetCellAppearance(barX, 16 + i, cg);
            }

            line(new(barX, 15), new(barX + WIDTH + 7, 15), '-');
            line(new(barX, 16 + 26), new(barX + WIDTH + 7, 16 + 26), '-');
            barX += WIDTH + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        } else {
            var highlightColor = Color.Yellow;
            var name = new ColoredString("<Empty>", highlightColor, Color.Black);
            Surface.Print(x, y, name);

            int barX = x - 2;
            line(new(barX, 15), new(barX + WIDTH + 7, 15), '-');
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
            line(new Point(barX, 16 + 26), new Point(barX + WIDTH + 7, 16 + 26), '-');
            barX += WIDTH + 7;
            line(new Point(barX, 16), new Point(barX, 16 + 25), '|');
        }

        x += WIDTH + 7;
        y = 16;
        var item = currentItem;
        if (item != null) {
            Surface.Print(x, y, getName(item), Color.Yellow, Color.Black);
            y += 2;
            foreach (var l in getDesc(item)) {
                Surface.Print(x, y++, l);
            }
        }

        base.Render(delta);
    }

}