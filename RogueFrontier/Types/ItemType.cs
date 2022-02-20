﻿using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using Newtonsoft.Json;

namespace RogueFrontier;
public enum EItemUse {
    none, deployShip, installWeapon, repairArmor, invokePower, refuel,
    depleteTargetShields
}
public interface ItemUse {
    string GetDesc(PlayerShip player, Item item);
    void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) { }
}
public record DeployShip : ItemUse {
    [Req] public string shipClass;
    public ShipClass shipType;
    public DeployShip() { }
    public DeployShip(TypeCollection tc, XElement e) {
        e.Initialize(this);
        shipType = tc.Lookup<ShipClass>(shipClass);
    }
    public string GetDesc(PlayerShip player, Item item) => $"Deploy {shipType.name}";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {

        var w = new Wingmate(player);
        var a = new AIShip(
            new BaseShip(player.world, shipType, player.sovereign, player.position),
            behavior:w
            );
        player.onDestroyed += w;
        player.world.AddEntity(a);
        player.wingmates.Add(a);
        player.AddMessage(new Transmission(a, $"Deployed {shipType.name}"));
        player.cargo.Remove(item);
        callback?.Invoke();
    }
    class Avenge : IContainer<BaseShip.Destroyed> {
        AIShip avenger;
        public Avenge(AIShip avenger) {
            this.avenger = avenger;
        }
        [JsonIgnore]
        public BaseShip.Destroyed Value => (s, d, w) => {
            avenger.behavior = new AttackOrder(d);
        };
    }
}
public record InstallWeapon : ItemUse {
    public string GetDesc(PlayerShip player, Item item) =>
        player.cargo.Contains(item) ? "Install this weapon" : "Remove this weapon";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        if (player.cargo.Contains(item)) {
            player.AddMessage(new Message($"Installed weapon {item.type.name}"));

            player.cargo.Remove(item);
            player.devices.Install(item.Install<Weapon>());
        } else {
            player.AddMessage(new Message($"Removed weapon {item.type.name}"));

            player.devices.Remove(item.weapon);
            item.Remove<Weapon>();
            player.cargo.Add(item);
        }
        callback?.Invoke();
    }
}
public record RepairArmor : ItemUse {
    [Req] public int repairHP;
    public string GetDesc(PlayerShip player, Item item) => "Repair armor";
    public RepairArmor() { }
    public RepairArmor(XElement e) {
        e.Initialize(this);
    }
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SListScreen.RepairArmorScreen(prev, player, item, this, callback));
    }
}
public record InvokePower : ItemUse {
    [Req] public string powerType;
    [Req] public int charges;
    public PowerType power;
    public InvokePower() { }
    public InvokePower(TypeCollection tc, XElement e) {
        e.Initialize(this);
        power = tc.Lookup<PowerType>(powerType);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        $"Invoke {power.name} ({charges} charges remaining)";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        player.AddMessage(new Message($"Invoked the power of {item.type.name}"));

        charges--;
        if (charges == 0) {
            player.cargo.Remove(item);
        }
        power.Effect.ForEach(e=>e.Invoke(player));
        callback?.Invoke();
    }
}
public record Refuel : ItemUse {
    public int energy;
    public Refuel() { }
    public Refuel(TypeCollection tc, XElement e) {
        energy = e.ExpectAttInt("energy");
    }
    public string GetDesc(PlayerShip player, Item item) {
        return "Refuel reactor";
    }
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SListScreen.RefuelReactor(prev, player, item, this, callback));
    }
}
public record DepleteTargetShields() : ItemUse {
    public DepleteTargetShields(XElement e) : this() {
        e.Initialize(this);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        player.GetTarget(out var t) ? $"Deplete shields on {t.name}" : "Deplete shields on target";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        if(!player.GetTarget(out var t)) {
            player.AddMessage(new Message($"No target available"));
            return;
        } 
        
        if(!(t is IShip s)) {
            player.AddMessage(new Message($"Target must be a ship"));
            return;
        }

        if (!s.devices.Shield.Any()) {
            player.AddMessage(new Message($"Target does not have shields"));
            return;
        }
        s.devices.Shield.ForEach(s => s.Deplete());
        player.AddMessage(new Message($"Depleted shields on {s.name}"));

        player.cargo.Remove(item);
        callback?.Invoke();
    }
}


public record ApplyMod() : ItemUse {
    Modifier mod;
    public ApplyMod(XElement e) : this() {
        e.Initialize(this);
        mod = new(e);
    }
    public string GetDesc(PlayerShip player, Item item) =>
        $"Apply modifier to item (shows menu)";
    public void Invoke(Console prev, PlayerShip player, Item item, Action callback = null) {
        var p = prev.Parent;
        p.Children.Remove(prev);
        p.Children.Add(SListScreen.SetMod(prev, player, item, mod, callback));
    }
}
public record ItemType : DesignType {
    [Req] public string codename;
    [Req] public string name;
    [Opt] public string desc = "";
    [Req] public int level;
    [Req] public int mass;
    [Opt] public int value = 0;
    public FragmentDesc ammo;
    public ArmorDesc armor;
    public EngineDesc engine;
    public LauncherDesc launcher;
    public ReactorDesc reactor;
    public ServiceDesc service;
    public ShieldDesc shield;
    public SolarDesc solar;
    public WeaponDesc weapon;
    public ItemUse invoke;
    public void Initialize(TypeCollection tc, XElement e) {
        e.Initialize(this);
        invoke = e.TryAttEnum(nameof(invoke), EItemUse.none) switch {
            EItemUse.none => null,
            EItemUse.deployShip => new DeployShip(tc, e),
            EItemUse.installWeapon => new InstallWeapon(),
            EItemUse.repairArmor => new RepairArmor(e),
            EItemUse.invokePower => new InvokePower(tc, e),
            EItemUse.refuel => new Refuel(tc, e),
            EItemUse.depleteTargetShields=>new DepleteTargetShields(e),
            _ => null
        };
        foreach (var (tag, action) in new Dictionary<string, Action<XElement>> {
            ["Ammo"] = e =>     ammo = new(e),
            ["Armor"] = e =>    armor = new(e),
            ["Engine"] = e =>   engine = new(e),
            ["Launcher"] = e => launcher = new(tc, e),
            ["Reactor"] = e =>  reactor = new(e),
            ["Service"] = e =>  service = new(e),
            ["Shield"] = e =>   shield = new(e),
            ["Solar"] = e =>    solar = new(e),
            ["Weapon"] = e =>   weapon = new(tc, e),
        }) {
            if (e.HasElement(tag, out var sub)) {
                action(sub);
            }
        }
    }
}
public record ArmorDesc {
    [Req] public int maxHP;
    public Armor GetArmor(Item i) => new(i, this);
    public ArmorDesc() { }
    public ArmorDesc(XElement e) {
        e.Initialize(this);
    }
}
public record EngineDesc {
    [Req] public int powerUse;
    [Req] public double thrust;
    [Req] public double maxSpeed;
    [Req] public double rotationMaxSpeed;
    [Req] public double rotationDecel;
    [Req] public double rotationAccel;
    public Engine GetEngine(Item i) => new(i, this);
    public EngineDesc() { }
    public EngineDesc(XElement e) {
        e.Initialize(this);
    }
}
public record EnhancerDesc {
    [Req] public int powerUse;
    public Modifier mod;
    public Enhancer GetEnhancer(Item i) => new(i, this);
    public EnhancerDesc() { }
    public EnhancerDesc(XElement e) {
        e.Initialize(this);
        mod = new(e);
    }
}
public record LaunchDesc {
    public FragmentDesc shot;
    public ItemType ammoType;
    public LaunchDesc() {}
    public LaunchDesc(TypeCollection types, XElement e) {
        ammoType = types.Lookup<ItemType>(e.ExpectAtt(nameof(ammoType)));
        shot = ammoType.ammo ?? new(e);
    }
}
public record LauncherDesc {
    [Req] public int powerUse;
    [Req] public int fireCooldown;
    [Opt] public int recoil = 0;
    [Opt] public int repeat = 0;
    public CapacitorDesc capacitor;
    public List<LaunchDesc> missiles;
    public Launcher GetLauncher(Item i) => new Launcher(i, this);
    public Weapon GetWeapon(Item i) => new Weapon(i, weaponDesc);
    public LauncherDesc() { }
    public LauncherDesc(TypeCollection types, XElement e) {
        e.Initialize(this);

        if (e.HasElement("Capacitor", out var xmlCapacitor)) {
            capacitor = new CapacitorDesc(xmlCapacitor);
        }
        missiles = new();
        if(e.HasElements("Missile", out var xmlMissileArr)) {
            missiles.AddRange(xmlMissileArr.Select(m => new LaunchDesc(types, m)));
        }
    }

    public WeaponDesc weaponDesc => new() {
        powerUse = powerUse,
        fireCooldown = fireCooldown,
        recoil = recoil,
        repeat = repeat,
        fragment = missiles.First().shot,
        initialCharges = -1,
        capacitor = capacitor,
        ammoType = missiles.First().ammoType,
        targetProjectile = false,
        autoFire = false
    };
}
public record WeaponDesc {
    [Req] public int powerUse;
    [Req] public int fireCooldown;
    [Opt] public int recoil = 0;
    [Opt] public int repeat = 0;
    public FragmentDesc fragment;
    [Opt] public int initialCharges = -1;
    public CapacitorDesc capacitor;
    public ItemType ammoType;
    public bool targetProjectile;
    public bool autoFire;
    public int missileSpeed => fragment.missileSpeed;
    public int damageType => fragment.damageType;
    public IDice damageHP => fragment.damageHP;
    public int lifetime => fragment.lifetime;
    public int minRange => fragment.missileSpeed * fragment.lifetime / (Program.TICKS_PER_SECOND * Program.TICKS_PER_SECOND); //DOES NOT INCLUDE CAPACITOR EFFECTS
    public Weapon GetWeapon(Item i) => new Weapon(i, this);
    public WeaponDesc() { }
    public WeaponDesc(TypeCollection types, XElement e) {
        e.Initialize(this);
        fragment = new FragmentDesc(e);

        if (e.HasElement("Capacitor", out var xmlCapacitor)) {
            capacitor = new CapacitorDesc(xmlCapacitor);
        }

        if (e.TryAtt(nameof(ammoType), out string at)) {
            ammoType = types.Lookup<ItemType>(at);
        }
        if (e.TryAttBool("pointDefense")) {
            fragment.hitProjectile = true;
            targetProjectile = true;
            autoFire = true;
        }
        

    }
}
public record FragmentDesc {
    [Opt] public int count = 1;
    [Opt] public bool omnidirectional;
    [Opt] public bool? targetLocked;
    [Opt] public double spreadAngle;
    [Req] public int missileSpeed;
    [Req] public int damageType;
    [Req] public IDice damageHP;
    [Opt] public int knockback;
    [Opt] public int shock;
    [Req] public int lifetime;
    [Opt] public bool passthrough;
    [Opt] public double maneuver;
    [Opt] public double maneuverRadius;
    [Opt] public int fragmentInterval;
    [Opt] public bool hitProjectile;
    [Opt] public bool hitBarrier = true;

    public int range => missileSpeed * lifetime / Program.TICKS_PER_SECOND;
    public DisruptorDesc disruptor;
    public HashSet<FragmentDesc> fragments;
    public StaticTile effect;
    public TrailDesc trail;

    public Modifier mod;
    public FragmentDesc() { }
    public FragmentDesc(XElement e) {
        Initialize(e);
    }

    public void Initialize(XElement e) {
        e.Initialize(this);
        if (e.TryAttBool("spreadOmni")) {
            spreadAngle = (2 * Math.PI) / count;
        } else {
            spreadAngle = e.TryAttDouble(nameof(spreadAngle), count == 1 ? 0 : 3) * Math.PI / 180;
        }
        maneuver *= Math.PI / (180);
        if (e.HasElements("Fragment", out var fragmentsList)) {
            fragments = new(fragmentsList.Select(f => new FragmentDesc(f)));
        }
        if (e.HasElement("Disruptor", out var xmlDisruptor)) {
            disruptor = new(xmlDisruptor);
        }
        if (e.HasElement("Trail", out var xmlTrail)) {
            trail = new(xmlTrail);
        }
        effect = new StaticTile(e);
    }

    public List<Projectile> GetProjectile(SpaceObject owner, Weapon w, double direction) {
        double angleInterval = spreadAngle / count;
        return new(Enumerable.Range(0, count).Select(i => {
            double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            return new Projectile(owner, this,
                owner.position + XY.Polar(angle),
                owner.velocity + XY.Polar(angle, missileSpeed),
                this.GetManeuver(w.target)
                );
        }));
    }
}
public record TrailDesc : ITrail {
    [Req] public int lifetime;
    [Req] public char glyph;
    [Req] public Color foreground;
    [Req] public Color background;
    public TrailDesc() { }
    public TrailDesc(XElement e) {
        e.Initialize(this);
    }
    public Effect GetTrail(XY Position) => new FadingTile(Position, new ColoredGlyph(foreground, background, glyph), lifetime);
}
public record DisruptorDesc {
    DisruptMode thrustMode, turnMode, brakeMode, fireMode;
    public int lifetime;
    public DisruptorDesc() { }
    public DisruptorDesc(XElement e) {
        thrustMode = GetMode(e.TryAtt(nameof(thrustMode), null));
        turnMode = GetMode(e.TryAtt(nameof(turnMode), null));
        brakeMode = GetMode(e.TryAtt(nameof(brakeMode), null));
        fireMode = GetMode(e.TryAtt(nameof(fireMode), null));
        lifetime = e.TryAttInt(nameof(lifetime), 60);
    }
    public Disrupt GetHijack() => new Disrupt() {
        thrustMode = thrustMode,
        turnMode = turnMode,
        brakeMode = brakeMode,
        fireMode = fireMode,
        ticksLeft = lifetime
    };
    public DisruptMode GetMode(string str) {
        switch (str) {
            case "on":
                return DisruptMode.FORCE_ON;
            case "off":
                return DisruptMode.FORCE_OFF;
            case "none":
            case null:
                return DisruptMode.NONE;
            default:
                throw new Exception($"Invalid value {str}");

        }
    }
}
public record CapacitorDesc {
    [Opt] public double minChargeToFire = 0;
    [Req] public double dischargeOnFire,
                        rechargePerTick,
                        maxCharge;
    [Opt] public double bonusSpeedPerCharge,
                        bonusDamagePerCharge,
                        bonusLifetimePerCharge;
    
    public CapacitorDesc() { }
    public CapacitorDesc(XElement e) {
        e.Initialize(this);
    }
}
public record ReactorDesc {
    [Req] public int maxOutput;
    [Req] public int capacity;
    [Opt] public double efficiency = 1;
    [Opt] public bool battery = false;        //If true, then we recharge using power from other reactors when available

    public Reactor GetReactor(Item i) => new Reactor(i, this);
    public ReactorDesc() { }
    public ReactorDesc(XElement e) {
        e.Initialize(this);
    }
}
public enum ServiceType {
    missileJack,
    armorRepair,
    grind
}
public record ServiceDesc {
    public ServiceType type;
    [Req] public int powerUse;
    [Req] public int interval;
    public Service GetService(Item i) => new(i, this);
    public ServiceDesc() { }
    public ServiceDesc(XElement e) {
        e.Initialize(this);
        type = e.ExpectAttEnum<ServiceType>(nameof(type));
    }
}
public record ShieldDesc {
    [Req] public int powerUse, idlePowerUse;
    [Req] public int maxHP;
    [Req] public int damageDelay, depletionDelay;
    [Req] public double regen;
    [Opt] public double absorbFactor = 1;
    public Shield GetShield(Item i) => new Shield(i, this);
    public ShieldDesc() { }
    public ShieldDesc(XElement e) {
        e.Initialize(this);
    }
}
public record SolarDesc {
    [Req] public int maxOutput;
    [Opt] public int lifetimeOutput = -1 + 0 * 360000;
    public Solar GetSolar(Item i) => new(i, this);
    public SolarDesc() { }
    public SolarDesc(XElement e) {
        e.Initialize(this);
    }
}