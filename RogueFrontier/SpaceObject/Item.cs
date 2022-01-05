﻿using Common;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using Helper = Common.Main;
using SadRogue.Primitives;
using Newtonsoft.Json;
namespace RogueFrontier;
public class Item {
    public string name => type.name;
    public ItemType type;

    //These fields are to remain null while the item is not installed and to be populated upon installation
    
    public Armor armor;
    public Engine engine;
    public Reactor reactor;
    public Service service;
    public Shield shield;
    public Solar solar;
    public Weapon weapon;

    public Modifier mod;

    public Item() { }
    public Item(Item copy) {
        type = copy.type;
        weapon = copy.weapon?.Copy(this);
        armor = copy.armor?.Copy(this);
        shield = copy.shield?.Copy(this);
        reactor = copy.reactor?.Copy(this);
        solar = copy.solar.Copy(this);
        service = copy.service.Copy(this);
        mod = copy.mod with { };
    }
    public Item(ItemType type, Modifier mod = null) {
        this.type = type;
        this.mod = mod;

        weapon = null;
        armor = null;
        shield = null;
        reactor = null;
        solar = null;
        service = null;
    }
    public T Get<T>() where T:class, Device{
        return (T)new Dictionary<Type, Device>() {
                [typeof(Armor)] = armor,
                [typeof(Engine)] = engine,
                [typeof(Reactor)] = reactor,
                [typeof(Service)]= service,
                [typeof(Shield)] = shield,
                [typeof(Solar)] = solar,
                [typeof(Weapon)] = weapon,
        }[typeof(T)];
    }
    public bool Get<T>(out T result) where T : class, Device => (result = Get<T>()) != null;
    public bool Has<T>() where T : class, Device => Get<T>() != null;
    public void Remove<T>() where T : class, Device {
        new Dictionary<Type, Func<Device>>() {
            [typeof(Armor)] = () => armor = null,
            [typeof(Engine)] = () => engine = null,
            [typeof(Reactor)] = () => reactor = null,
            [typeof(Service)] = () => service = null,
            [typeof(Shield)] = () => shield = null,
            [typeof(Solar)] = () => solar = null,
            [typeof(Weapon)] = () => weapon = null,
        }[typeof(T)]();
    }
    public T Install<T>() where T:class, Device {
        return (T) (new Dictionary<Type, Func<Device>>() {
            [typeof(Armor)] = () => armor ??= type.armor?.GetArmor(this),
            [typeof(Engine)] = () => engine = type.engine?.GetEngine(this),
            [typeof(Reactor)] = () => reactor ??= type.reactor?.GetReactor(this),
            [typeof(Service)] = () => service ??= type.service?.GetService(this),
            [typeof(Shield)] = () => shield ??= type.shield?.GetShield(this),
            [typeof(Solar)] = () => solar ??= type.solar?.GetSolar(this),
            [typeof(Weapon)] = () => weapon ??= type.weapon?.GetWeapon(this),
        }[typeof(T)]());
    }
    public bool Install<T>(out T result) where T:class, Device {
        return (result = Install<T>()) != null;
    }
    public void RemoveAll() {
        armor = null;
        engine = null;
        reactor = null;
        service = null;
        shield = null;
        solar = null;
        weapon = null;
    }
}
public interface Device {
    Item source { get; }
    void Update(IShip owner);
    int? powerUse => null;
    public bool IsEnabled(IShip owner) =>
        (owner as PlayerShip)?.energy.off.Contains(this) != false;
    public void OnOverload(PlayerShip owner) { }
    public void OnDisable() { }
}
/*
public class MultiItemAmmo : IAmmo {
    public int index;
    public List<IAmmo> missiles;
    public IAmmo current => missiles[index];
    public bool AllowFire => current.AllowFire;
    public MultiItemAmmo(List<IAmmo> missiles) {
        this.missiles = missiles;
    }
    public void Update(IShip source) => current.Update(source);
    public void Update(Station source) => current.Update(source);

    public void OnFire() => current.OnFire();
}
*/
public interface PowerSource {
    double energyDelta { get; set; }
    int maxOutput { get; }
}
public class Armor : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ArmorDesc desc;
    public int hp;
    public int lastDamageTick;
    public Armor() { }
    public Armor(Item source, ArmorDesc desc) {
        this.source = source;
        this.desc = desc;
        this.hp = desc.maxHP;
    }
    public Armor Copy(Item source) => desc.GetArmor(source);
    public void Update(IShip owner) { }
}
public class Engine : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public EngineDesc desc;
    public bool thrusting;
    public Engine() { }
    public Engine(Item source, EngineDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Engine Copy(Item source) => desc.GetEngine(source);
    public void Update(IShip owner) {
        var rotationDeg = owner.rotationDeg;
        var ship = (owner is PlayerShip ps ? ps.ship : owner is AIShip a ? a.ship : null);
        var sc = ship.shipClass;
        UpdateThrust();
        UpdateTurn();
        UpdateRotation();
        UpdateBrake();
        void UpdateThrust() {
            if (thrusting) {
                var rotationRads = rotationDeg * Math.PI / 180;

                var exhaust = new EffectParticle(ship.position + XY.Polar(rotationRads, -1),
                    ship.velocity + XY.Polar(rotationRads, -sc.thrust),
                    new ColoredGlyph(Color.Yellow, Color.Transparent, '.'),
                    4);
                ship.world.AddEffect(exhaust);

                ship.velocity += XY.Polar(rotationRads, sc.thrust);
                if (ship.velocity.magnitude > ship.shipClass.maxSpeed) {
                    ship.velocity = ship.velocity.normal * sc.maxSpeed;
                }

                thrusting = false;
            }
        }
        void UpdateTurn() {
            if (ship.rotating != Rotating.None) {
                ref var rv = ref ship.rotatingVel;
                if (ship.rotating == Rotating.CCW) {
                    /*
                    if (rotatingSpeed < 0) {
                        rotatingSpeed += Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv < 0) {
                        Decel();
                    }
                    rv += sc.rotationAccel / Program.TICKS_PER_SECOND;
                } else if (ship.rotating == Rotating.CW) {
                    /*
                    if(rotatingSpeed > 0) {
                        rotatingSpeed -= Math.Min(Math.Abs(rotatingSpeed), ShipClass.rotationDecel);
                    }
                    */
                    //Add decel if we're turning the other way
                    if (rv > 0) {
                        Decel();
                    }
                    rv -= sc.rotationAccel / Program.TICKS_PER_SECOND;
                }
                rv = Math.Min(Math.Abs(rv), sc.rotationMaxSpeed) * Math.Sign(rv);
                ship.rotating = Rotating.None;
            } else {
                Decel();
            }
            void Decel() => ship.rotatingVel -= Math.Min(Math.Abs(ship.rotatingVel), sc.rotationDecel / Program.TICKS_PER_SECOND) * Math.Sign(ship.rotatingVel); ;
        }
        void UpdateRotation() => ship.rotationDeg += ship.rotatingVel;
        void UpdateBrake() {
            if (ship.decelerating) {
                if (ship.velocity.magnitude > 0.05) {
                    ship.velocity -= ship.velocity.normal * Math.Min(ship.velocity.magnitude, sc.thrust / 2);
                } else {
                    ship.velocity = new XY();
                }
                ship.decelerating = false;
            }
        }
    }
}

public class Enhancer : Device {
    [JsonProperty]
    public Item source { get; set; }
    public EnhancerDesc desc;
    public int powerUse => desc.powerUse;
    public Enhancer() { }
    public Enhancer(Item source, EnhancerDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Enhancer Copy(Item source) => desc.GetEnhancer(source);
    public void Update(IShip owner) {
    }
}

public class Launcher : Device {
    public LauncherDesc desc;
    public Weapon weapon;
    public int index;
    [JsonIgnore]
    public Item source => weapon.source;
    [JsonIgnore]
    public LaunchDesc fragmentDesc => desc.missiles[index];
    [JsonIgnore]
    int? Device.powerUse => ((Device)weapon).powerUse;
    public FragmentDesc GetFragmentDesc() => weapon.GetFragmentDesc();
    [JsonIgnore]
    public int missileSpeed => GetFragmentDesc().missileSpeed;
    [JsonIgnore]
    public int currentRange => GetFragmentDesc().range;
    [JsonIgnore]
    public int lifetime => GetFragmentDesc().lifetime;
    [JsonIgnore]
    public int currentRange2 => currentRange * currentRange;
    [JsonIgnore]
    public Capacitor capacitor => weapon.capacitor;
    [JsonIgnore]
    public Aiming aiming => weapon.aiming;
    [JsonIgnore]
    public IAmmo ammo => weapon.ammo;
    [JsonIgnore]
    public int delay => weapon.delay;
    [JsonIgnore]
    public bool firing => weapon.firing;
    [JsonIgnore]
    public int repeatsLeft => weapon.repeatsLeft;
    public Launcher() { }
    public Launcher(Item source, LauncherDesc desc) {
        this.weapon = desc.GetWeapon(source);
        this.desc = desc;
    }
    public Launcher Copy(Item source) => desc.GetLauncher(source);
    public void SetMissile(int index) {
        this.index = index;
        var l = desc.missiles[index];
        weapon.ammo = new ItemAmmo(l.ammoType);
        weapon.desc.fragment = l.shot;
    }
    public string GetReadoutName() => weapon.GetReadoutName();
    public ColoredString GetBar() => weapon.GetBar();
    public void Update(Station owner) => weapon.Update(owner);
    public void Update(IShip owner) => weapon.Update(owner);
    public void OnDisable() => weapon.OnDisable();
    public bool RangeCheck(SpaceObject user, SpaceObject target) => weapon.RangeCheck(user, target);
    public bool AllowFire => weapon.AllowFire;
    public bool ReadyToFire => weapon.ReadyToFire;
    public void Fire(SpaceObject owner, double direction) => weapon.Fire(owner, direction);
    public SpaceObject target => weapon.target;
    public void OverrideTarget(SpaceObject target) => weapon.OverrideTarget(target);
    public void SetFiring(bool firing = true) => weapon.SetFiring(firing);
    public void SetFiring(bool firing = true, SpaceObject target = null) => weapon.SetFiring(firing, target);
}
public class Reactor : Device, PowerSource {
    [JsonProperty]
    public Item source { get; set; }
    public ReactorDesc desc;
    public double energy;
    [JsonProperty]
    public double energyDelta { get; set; }
    public int rechargeDelay;
    public int maxOutput => energy > 0 ? desc.maxOutput : 0;
    public Reactor() { }
    public Reactor(Item source, ReactorDesc desc) {
        this.source = source;
        this.desc = desc;
        energy = desc.capacity;
        energyDelta = 0;
    }
    public Reactor Copy(Item source) => desc.GetReactor(source);
    public void Update(IShip owner) {
        energy = Math.Max(0, Math.Min(
            energy + (energyDelta < 0 ? energyDelta / desc.efficiency : energyDelta) / 30,
            desc.capacity));
    }
}
public class Service : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ServiceDesc desc;
    public int ticks;
    [JsonProperty]
    public int powerUse { get; private set; }
    int? Device.powerUse => powerUse;
    public Service() { }
    public Service(Item source, ServiceDesc desc) {
        this.source = source;
        this.desc = desc;
        powerUse = 0;
    }
    public Service Copy(Item source) => desc.GetService(source);
    public void Update(IShip owner) {
        ticks++;
        if (ticks % desc.interval == 0) {
            var powerUse = 0;
            switch (desc.type) {
                case ServiceType.missileJack: {
                        //May not work in Arena mode if we assume control
                        //bc weapon locks are focused on the old AI ship
                        var missile = owner.world.entities.all
                            .OfType<Projectile>()
                            .FirstOrDefault(
                                p => (owner.position - p.position).magnitude < 24
                                  && p.maneuver != null
                                  && p.maneuver.maneuver > 0
                                  && Equals(p.maneuver.target, owner)
                                );
                        if (missile != null) {
                            missile.maneuver.target = missile.source;
                            missile.source = owner;
                            var offset = (missile.position - owner.position);
                            var dist = offset.magnitude;
                            var inc = offset.normal;
                            for (var i = 0; i < dist; i++) {
                                var p = owner.position + inc * i;
                                owner.world.AddEffect(new EffectParticle(p, new ColoredGlyph(Color.Orange, Color.Transparent, '-'), 10));
                            }
                            powerUse = desc.powerUse;
                        }
                        break;
                    }
                case ServiceType.armorRepair: {
                        break;
                    }
                case ServiceType.grind:
                    if (owner is PlayerShip player) {
                        powerUse = this.powerUse + (player.energy.totalOutputMax - player.energy.totalOutputUsed);
                    }
                    break;
            }
            this.powerUse = powerUse;
        }
    }
    void Device.OnOverload(PlayerShip owner) {
        powerUse = owner.energy.totalOutputLeft;
    }
}
public class Shield : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public ShieldDesc desc;
    public int hp;
    public double regenHP;
    public int delay;
    public double absorbFactor => desc.absorbFactor;
    public int maxAbsorb => hp;
    /*
    public int maxAbsorb => desc.absorbMaxHP == -1 ?
        hp : Math.Min(hp, absorbHP);
    public int absorbHP;
    public double absorbRegenHP;
    */
    int? Device.powerUse => hp < desc.maxHP ? desc.powerUse : desc.idlePowerUse;
    public Shield() { }
    public Shield(Item source, ShieldDesc desc) {
        this.source = source;
        this.desc = desc;
    }
    public Shield Copy(Item source) => desc.GetShield(source);
    public void OnDisable() => Deplete();
    public void Deplete() {

        hp = 0;
        regenHP = 0;
        delay = desc.depletionDelay;
    }
    public void Update(IShip owner) {
        if (delay > 0) {
            delay--;
        } else {
            regenHP += desc.regen;
            while (regenHP >= 1) {
                if (hp < desc.maxHP) {
                    hp++;
                    regenHP--;
                } else {
                    regenHP = 0;
                }
            }
            /*
            absorbRegenHP += desc.absorbRegen;
            while(absorbRegenHP >= 1) {
                if(absorbHP < desc.absorbMaxHP) {
                    absorbHP++;
                    absorbRegenHP--;
                } else {
                    absorbRegenHP = 0;
                }
            }
            */
        }
    }
    public void Absorb(int damage) {
        hp = Math.Max(0, hp - damage);
        //absorbHP = Math.Max(0, absorbHP - damage);
        delay = (hp == 0 ? desc.depletionDelay : desc.damageDelay);
    }
}
public class Solar : Device, PowerSource {
    [JsonProperty]
    public Item source { get; private set; }
    public SolarDesc desc;
    public int lifetimeOutput;
    public bool dead;
    [JsonProperty]
    public int maxOutput { get; private set; }
    [JsonProperty]
    public double energyDelta { get; set; }
    public Solar() { }
    public Solar(Item source, SolarDesc desc) {
        this.source = source;
        this.desc = desc;
        lifetimeOutput = desc.lifetimeOutput;
    }
    public Solar Copy(Item source) => desc.GetSolar(source);
    public void Update(IShip owner) {
        void Update() {
            var t = owner.world.backdrop.starlight.GetTile(owner.position);
            var b = t.A;
            maxOutput = (b * desc.maxOutput / 255);
        }
        switch (lifetimeOutput) {
            case -1: 
                Update();
                break;
            case 0: 
                break;
            case 1:
                lifetimeOutput = 0;
                maxOutput = 0;
                if (owner is PlayerShip ps) {
                    ps.AddMessage(new Message($"{source.name} has stopped functioning"));
                }
                break;
            default:
                lifetimeOutput = (int)Math.Max(1, lifetimeOutput + energyDelta);
                Update();
                break;
        }
    }
}
public static class SWeapon {

    public static void CreateShot(this FragmentDesc fragment, SpaceObject source, double direction, SpaceObject target = null) {
        var world = source.world;
        var position = source.position;
        var velocity = source.velocity;
        var angleInterval = fragment.spreadAngle / fragment.count;
        for (int i = 0; i < fragment.count; i++) {
            double angle = direction + ((i + 1) / 2) * angleInterval * (i % 2 == 0 ? -1 : 1);
            var p = new Projectile(source,
                fragment,
                position + XY.Polar(angle, 0.5),
                velocity + XY.Polar(angle, fragment.missileSpeed),
                fragment.GetManeuver(target));
            world.AddEntity(p);
        }
    }
    public static Maneuver GetManeuver(this FragmentDesc f, SpaceObject target) =>
        target != null && f.maneuver != 0 ? new Maneuver(target, f.maneuver, f.maneuverRadius) : null;
    public static Maneuver GetManeuver(this Weapon w, FragmentDesc f) =>
        f.GetManeuver(w.target);
    public static Maneuver GetManeuver(this Weapon w) =>
        w.GetFragmentDesc().GetManeuver(w.target);
}
public class Weapon : Device {
    [JsonProperty]
    public Item source { get; private set; }
    public WeaponDesc desc;
    [JsonIgnore]
    int? Device.powerUse => (firing || delay > 0) ? desc.powerUse : desc.powerUse / 10;
    public FragmentDesc GetFragmentDesc() =>
        Modifier.Sum(capacitor?.mod, source.mod, enhancements) * desc.fragment;
    
    [JsonIgnore]
    public int missileSpeed => GetFragmentDesc().missileSpeed;
    [JsonIgnore]
    public int currentRange {
        get {
            var f = GetFragmentDesc();
            return f.missileSpeed * f.lifetime / Program.TICKS_PER_SECOND;
        }
    }
    public int lifetime => GetFragmentDesc().lifetime;
    [JsonIgnore]
    public int currentRange2 => currentRange * currentRange;
    public Capacitor capacitor;
    public Aiming aiming;
    public IAmmo ammo;
    public Modifier enhancements;
    public int delay;
    public bool firing;
    public int repeatsLeft;
    public Weapon() { }
    public Weapon(Item source, WeaponDesc desc) {
        this.source = source;
        this.desc = desc;
        this.delay = 0;
        firing = false;
        if (desc.capacitor != null) {
            capacitor = new(desc.capacitor);
        }
        if (desc.fragment.omnidirectional) {
            aiming = new Omnidirectional();
        } else if (desc.fragment.maneuver > 0) {
            aiming = new Targeting();
        }
        if (desc.initialCharges > -1) {
            ammo = new ChargeAmmo(desc.initialCharges);
        } else if (desc.ammoType != null) {
            ammo = new ItemAmmo(desc.ammoType);
        }
    }
    public Weapon Copy(Item source) => desc.GetWeapon(source);
    public string GetReadoutName() {
        if (ammo is ChargeAmmo c) {
            return $"{source.type.name} [{c.charges}]";
        } else if (ammo is ItemAmmo i) {
            return $"{source.type.name} [{i.count}]";
        }
        return source.type.name;
    }
    public ColoredString GetBar() {
        if (ammo?.AllowFire == false) {
            return new(new(' ', 16), Color.Transparent, Color.Black);
        }

        int fireBar = (int)(16f * (desc.fireCooldown - delay) / desc.fireCooldown);
        ColoredString bar;
        if (capacitor != null && capacitor.desc.minChargeToFire > 0) {
            var chargeBar = (int)(16 * Math.Min(1, capacitor.charge / capacitor.desc.minChargeToFire));
            bar = new ColoredString(new('>', chargeBar), Color.Gray, Color.Black)
                + new ColoredString(new(' ', 16 - chargeBar), Color.Transparent, Color.Black);
        } else {
            bar = new(new('>', 16), Color.Gray, Color.Black);
        }
        foreach (var cg in bar.Take(fireBar)) {
            cg.Foreground = Color.White;
        }
        if (capacitor != null) {
            var n = 16 * capacitor.charge / capacitor.desc.maxCharge;
            foreach (var cg in bar.Take((int)n + 1)) {
                cg.Foreground = cg.Foreground.Blend(Color.Cyan.SetAlpha(128));
            }
        }
        return bar;
    }
    public void Update(Station owner) {
        double? direction = null;
        if (aiming != null) {
            aiming.Update(owner, this);
            direction = aiming.GetFireAngle() ??
                Omnidirectional.GetFireAngle(owner, aiming.target, this);
        }
        capacitor?.Update();
        ammo?.Update(owner);
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
        } else {
            //Stations always fire for now
            firing = true;
            bool beginRepeat = true;
            if (repeatsLeft > 0) {
                repeatsLeft--;
                firing = true;
                beginRepeat = false;
            } else if (desc.autoFire) {
                if (desc.targetProjectile) {
                    var target = Aiming.AcquireMissile(owner, this, s => SStation.IsEnemy(owner, s));
                    if (target != null
                        && Aiming.CalcFireAngle(owner, target, this, out var d)) {
                        direction = d;
                        firing = true;
                    }
                } else if (aiming?.target != null) {
                    firing = true;
                }
            }
            //bool allowFire = (firing || true) && (capacitor?.AllowFire ?? true);
            capacitor?.CheckFire(ref firing);
            ammo?.CheckFire(ref firing);
            if (firing && direction.HasValue) {
                enhancements = new();

                Fire(owner, direction.Value);
                delay = desc.fireCooldown;
                if (beginRepeat) {
                    repeatsLeft = desc.repeat;
                }
            } else {
                repeatsLeft = 0;
            }
        }
        firing = false;
    }
    public void Update(IShip owner) {
        double? direction = owner.rotationRad;

        if (aiming != null) {
            aiming.Update(owner, this);
            direction = aiming.GetFireAngle() ?? direction;
        }
        capacitor?.Update();
        ammo?.Update(owner);
        if (delay > 0 && repeatsLeft == 0) {
            delay--;
        } else {
            bool beginRepeat = true;
            if (repeatsLeft > 0) {
                repeatsLeft--;
                firing = true;
                beginRepeat = false;
            } else if (desc.autoFire) {
                if (desc.targetProjectile) {
                    var target = Aiming.AcquireMissile(owner, this, s => s == null || SShip.IsEnemy(owner, s));
                    if (target != null
                        && Aiming.CalcFireAngle(owner, target, this, out var d)) {
                        direction = d;
                        firing = true;
                    }
                } else if (aiming?.target != null) {
                    firing = true;
                }
            }

            //bool allowFire = firing && (capacitor?.AllowFire ?? true);
            capacitor?.CheckFire(ref firing);
            ammo?.CheckFire(ref firing);

            if (firing) {
                Fire(owner, direction.Value);

                //Apply on next tick (create a delta-momentum variable)
                if (desc.recoil > 0) {
                    owner.velocity += XY.Polar(direction.Value + Math.PI, desc.recoil);
                }

                delay = desc.fireCooldown;
                if (beginRepeat) {
                    repeatsLeft = desc.repeat;
                }
            } else {
                repeatsLeft = 0;
            }
        }
        firing = false;
    }

    public void OnDisable() {
        delay = desc.fireCooldown;
        capacitor?.Clear();
        aiming?.ClearTarget();
    }
    public bool RangeCheck(SpaceObject user, SpaceObject target) =>
        (user.position - target.position).magnitude < currentRange;
    public bool AllowFire => ammo?.AllowFire ?? true;
    public bool ReadyToFire => delay == 0 && (capacitor?.AllowFire ?? true) && (ammo?.AllowFire ?? true);
    
    public void Fire(SpaceObject owner, double direction, List<Projectile> l = null) {
        var projectiles = GetFragmentDesc().GetProjectile(owner, this, direction);
        projectiles.ForEach(owner.world.AddEntity);
        l?.AddRange(projectiles);
        ammo?.OnFire();
        capacitor?.OnFire();
    }

    public SpaceObject target => aiming?.target;
    public void OverrideTarget(SpaceObject target) {

        if (aiming != null) {
            aiming.ClearTarget();
            aiming.UpdateTarget(target);
        }
    }
    public void SetFiring(bool firing = true) => this.firing = firing;

    //Use this if you want to override auto-aim
    public void SetFiring(bool firing = true, SpaceObject target = null) {
        this.firing = firing;
        aiming?.UpdateTarget(target);
    }

}
public class Capacitor {
    public CapacitorDesc desc;
    public double charge;
    public Capacitor(CapacitorDesc desc) {
        this.desc = desc;
    }
    public void CheckFire(ref bool firing) => firing = firing && AllowFire;
    public bool AllowFire => desc.minChargeToFire <= charge;
    public void Update() =>
        charge = Math.Min(desc.maxCharge, charge + desc.rechargePerTick);
    public Modifier mod => new() {
        damageHPInc = (int)(desc.bonusDamagePerCharge * charge),
        missileSpeedInc = (int)(desc.bonusSpeedPerCharge * charge),
        lifetimeInc = (int)(desc.bonusLifetimePerCharge * charge)
    };
    /*
    public FragmentDesc Modify(FragmentDesc fd) =>
        fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    public void Modify(ref FragmentDesc fd) =>
        fd = fd with {
            damageHP = new DiceInc(fd.damageHP, (int)(desc.bonusDamagePerCharge * charge)),
            missileSpeed = fd.missileSpeed + (int)(desc.bonusSpeedPerCharge * charge),
            lifetime = fd.lifetime + (int)(desc.bonusLifetimePerCharge * charge)
        };
    */
    public void OnFire() =>
        charge = Math.Max(0, charge - desc.dischargeOnFire);
    public void Clear() => charge = 0;
}
public interface Aiming {
    public SpaceObject target { get; }
    void Update(Station owner, Weapon weapon);
    void Update(IShip owner, Weapon weapon);
    double? GetFireAngle() => null;
    void ClearTarget() { }
    void UpdateTarget(SpaceObject target) { }

    public static double? CalcFireAngle(XY deltaPosition, XY deltaVelocity, FragmentDesc f) =>
        (deltaPosition.magnitude < f.range) ?
            Helper.CalcFireAngle(deltaPosition, deltaVelocity, f.missileSpeed, out var _) :
            null;
    public static bool CalcFireAngle(MovingObject owner, MovingObject target, FragmentDesc f, out double? result) {
        var b = ((target.position - owner.position).magnitude < f.range);
        result = b ? Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, f.missileSpeed, out var _) : null;
        return b;
    }
    public static double? CalcFireAngle(MovingObject owner, MovingObject target, FragmentDesc f) =>
        ((target.position - owner.position).magnitude < f.range) ?
            Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, f.missileSpeed, out var _) :
            null;
    public static double CalcFireAngle(MovingObject owner, MovingObject target, int missileSpeed) =>
        Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
    public static bool CalcFireAngle(MovingObject owner, MovingObject target, int missileSpeed, out double result) {
        var velDiff = target.velocity - owner.velocity;
        var b = velDiff.magnitude < missileSpeed;
        result = b ? Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _) : 0;
        return b;
    }

    public static bool CalcFireAngle(MovingObject owner, Projectile target, Weapon weapon, out double? result) {
        bool b = ((target.position - owner.position).magnitude < weapon.currentRange);
        result = b ? Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, weapon.missileSpeed, out var _) : null;
        return b;
    }
    public static SpaceObject AcquireTarget(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) =>
        owner.world.entities.GetAll(p => (owner.position - p).magnitude2 < weapon.currentRange2).OfType<SpaceObject>().FirstOrDefault(filter);

    public static Projectile AcquireMissile(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) =>
        owner.world.entities.all
            .OfType<Projectile>()
            .Where(p => (owner.position - p.position).magnitude2 < weapon.currentRange2)
            .Where(p => filter(p.source))
            .OrderBy(p => (owner.position - p.position).Dot(p.velocity))
            //.OrderBy(p => (owner.Position - p.Position).Magnitude2)
            .FirstOrDefault();    
}
public class Targeting : Aiming {
    public SpaceObject target { get; set; }
    public Targeting() { }
    public void Update(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
        if (target?.active != true
            || (owner.position - target.position).magnitude > weapon.currentRange
            ) {
            target = Aiming.AcquireTarget(owner, weapon, filter);
        }
    }
    public void Update(Station owner, Weapon weapon) =>
        Update(owner, weapon, s => SStation.IsEnemy(owner, s));
    public void Update(IShip owner, Weapon weapon) =>
        Update(owner, weapon, s => SShip.IsEnemy(owner, s));
    public void ClearTarget() => target = null;
    public void UpdateTarget(SpaceObject target) =>
        this.target = target ?? this.target;
}
public class Omnidirectional : Aiming {
    public SpaceObject target { get; set; }
    double? direction;
    public Omnidirectional() { }
    public static double? GetFireAngle(SpaceObject owner, SpaceObject target, Weapon w) =>
        target == null ? null : Aiming.CalcFireAngle(owner, target, w.GetFragmentDesc());
    public void Update(SpaceObject owner, Weapon weapon, Func<SpaceObject, bool> filter) {
        if (target?.active == true) {
            UpdateDirection();
        } else {
            direction = null;
            target = Aiming.AcquireTarget(owner, weapon, filter);

            if (target?.active == true) {
                UpdateDirection();
            }
        }

        void UpdateDirection() {
            if (Aiming.CalcFireAngle(owner, target, weapon.GetFragmentDesc(), out direction)) {
                Heading.AimLine(owner.world, owner.position, direction.Value);
                Heading.Crosshair(owner.world, target.position);
            }
        }
    }
    public void Update(Station owner, Weapon weapon) =>
        Update(owner, weapon, s => SStation.IsEnemy(owner, s));
    public void Update(IShip owner, Weapon weapon) =>
        Update(owner, weapon, s => SShip.IsEnemy(owner, s));
    public double? GetFireAngle() => direction;
    public void ClearTarget() => target = null;
    public void UpdateTarget(SpaceObject target) =>
        this.target = target ?? this.target;
}
public interface IAmmo {
    bool AllowFire { get; }
    public void Update(IShip source) { }
    public void Update(Station source) { }
    void CheckFire(ref bool firing) => firing &= AllowFire;
    void OnFire();
}
public class ChargeAmmo : IAmmo {
    public int charges;
    public bool AllowFire => charges > 0;
    public ChargeAmmo(int charges) {
        this.charges = charges;
    }

    public void OnFire() => charges--;
}
public class ItemAmmo : IAmmo {
    public ItemType itemType;
    public HashSet<Item> inventory;
    public Item unit;
    public bool AllowFire => unit != null;

    public int count;
    public int ticks;
    public ItemAmmo(ItemType itemType) {
        this.itemType = itemType;
    }
    public void Update(IShip source) =>
        Update(source.cargo);
    public void Update(Station source) =>
        Update(source.cargo);
    public void Update(HashSet<Item> inventory) {
        ticks++;
        if (ticks % 10 != 0) {
            return;
        }
        this.inventory = inventory;
        UpdateUnit();
    }
    public void UpdateUnit() {
        var units = inventory.Where(i => i.type == itemType);
        unit = units.FirstOrDefault();
        count = inventory.Count(i => i.type == itemType);
    }
    public void OnFire() {
        inventory.Remove(unit);
        UpdateUnit();
    }
}