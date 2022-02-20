﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Helper = Common.Main;
using static RogueFrontier.SShipBehavior;
using Newtonsoft.Json;

namespace RogueFrontier;
public interface IShipOrder : IShipBehavior {
    bool Active { get; }
    //void Update(AIShip owner);
    public bool CanTarget(SpaceObject other) => false;

    public delegate IShipOrder Create(SpaceObject target);
}

public interface ICombatOrder {
    public bool CanTarget(SpaceObject other) => false;
}
public class CompoundOrder : IShipOrder {
    public List<IShipOrder> orders;
    public IShipOrder current => orders.Any() ? orders[0] : null;
    public CompoundOrder() { }
    public CompoundOrder(params IShipOrder[] orders) {
        this.orders = new List<IShipOrder>(orders);
    }
    public void Update(AIShip owner) {
    Start:
        var first = orders.FirstOrDefault();
        switch (first?.Active) {
            case true:
                first.Update(owner);
                return;
            case false:
                orders.RemoveAt(0);
                goto Start;
            default:
                return;
        }
    }
    public bool Active => orders.Any();
}
public class EscortOrder : IShipOrder {
    [JsonProperty]
    private AttackOrder attack;
    [JsonProperty]
    private FollowOrder follow;
    int ticks = 0;
    public EscortOrder() { }
    public EscortOrder(IShip target, XY offset) {
        this.attack = new(null);
        this.follow = new(target, offset);
    }
    public bool CanTarget(SpaceObject other) => other == attack?.target;
    public void Update(AIShip owner) {
        ticks++;
        if(attack.Active == true) {
            attack.Update(owner);
            return;
        }
        if (ticks % 30 == 0) {
            var attacker = owner.world.entities.all
                .OfType<AIShip>()
                .FirstOrDefault(s => (s.position - owner.position).magnitude < 100
                            && (s.behavior.CanTarget(follow.target) || s.behavior.CanTarget(owner)));
            if (attacker != null) {
                attack.SetTarget(attacker);
                attack.Update(owner);
                return;
            }
        }
        follow.Update(owner);
    }
    public bool Active => follow.Active;
}
public class FollowOrder : IShipOrder {
    public XY baseOffset;
    public IShip target => approach.target;
    [JsonProperty]
    private ApproachOrder approach;
    public FollowOrder(IShip target, XY offset) {
        this.baseOffset = offset;
        this.approach = new(target, offset);
    }
    public void Update(AIShip owner) {
        approach.offset = baseOffset.Rotate(target.stoppingRotation * Math.PI / 180);
#if DEBUG
        //Heading.Crosshair(owner.world, target.position + offset);
#endif
        approach.Update(owner);
    }
    public bool Active => approach.Active;
}
public class ApproachOrder : IShipOrder {
    public IShip target;
    public XY offset;
    [JsonProperty]
    private FaceOrder face;
    public ApproachOrder(IShip target, XY offset) {
        this.target = target;
        this.offset = offset;
        this.face = new(0);
    }

    public void Update(AIShip owner) {
        //Remove dock
        owner.dock = null;
        var velDiff = owner.velocity - target.velocity;
        double decel = owner.shipClass.thrust * Program.TICKS_PER_SECOND / 2;
        double stoppingTime = velDiff.magnitude / decel;
        double stoppingDistance = owner.velocity.magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
        var stoppingPoint = owner.position;
        if (!owner.velocity.isZero) {
            stoppingPoint += owner.velocity.normal * stoppingDistance;
        }
        var formationPoint = target.position + this.offset.Rotate(target.stoppingRotation * Math.PI / 180);
        var dest = formationPoint + (target.velocity * stoppingTime);
        var offset = formationPoint - owner.position;
#if DEBUG
        //Heading.Crosshair(owner.world, dest);
#endif
        var velProjection = velDiff * velDiff.Dot(offset.normal) / velDiff.Dot(velDiff);
        var velRejection = velDiff - velProjection;
        if (velRejection.magnitude2 > 1) {
            owner.SetDecelerating(true);
        }
        if (offset.magnitude > velDiff.magnitude / 10) {
            //Approach the target
            //Face the target
            face.targetRads = offset.angleRad;
            //var Face = new FaceOrder(Helper.CalcFireAngle(target.Position - owner.Position, target.Velocity - owner.Velocity, owner.ShipClass.thrust * 30, out _));
            face.Update(owner);
            //If we're facing close enough
            if (Math.Abs(Helper.AngleDiff(owner.rotationDeg, offset.angleRad * 180 / Math.PI)) < 10 && (velProjection.magnitude < offset.magnitude / 2 || velDiff.magnitude == 0)) {
                //Go
                owner.SetThrusting(true);
            }
        } else {
            owner.velocity = target.velocity;
            owner.position = formationPoint;
            //Match the target's facing
            face.targetRads = target.rotationDeg * Math.PI / 180;
            face.Update(owner);
        }
    }
    public bool Active => true;
}
public class GuardOrder : IShipOrder {
    [JsonProperty]
    public SpaceObject GuardTarget { get; private set; }
    [JsonProperty]
    public AttackOrder attackOrder { get; private set; }
    [JsonProperty]
    private ApproachOrbitOrder approach;
    public int attackTime;
    public int ticks;
    public GuardOrder(SpaceObject guard) {
        this.GuardTarget = guard;
        this.attackOrder = new(null);
        this.approach = new(guard);
        attackOrder = new(null);
        attackTime = 0;
    }
    public void SetTarget(SpaceObject guard) {
        GuardTarget = guard;
        approach.target = guard;
    }
    public bool CanTarget(SpaceObject other) => other == attackOrder?.target;
    public void Attack(SpaceObject target, int attackTime = -1) {
        this.attackOrder.SetTarget(target);
        this.attackTime = attackTime;
    }
    public void ClearAttack() {
        this.attackOrder.SetTarget(null);
        this.attackTime = -1;
    }
    public void Update(AIShip owner) {
        ticks++;
        //If we have a target, then attack!
        if (attackOrder.Active == true) {
            attackOrder.Update(owner);
            //If we have finite attackTime set, then our attack order expires on time out
            attackTime--;
            if (attackTime == 0) {
                attackOrder.ClearTarget(); ;
            }
            return;
        }
        //Otherwise, we're idle
        //If we're docked, then don't check for enemies every tick
        if (owner.dock?.docked == true) {
            if (ticks % 150 != 0) {
                return;
            }
        }
        //Look for a nearby attack target periodically
        if (ticks % 15 == 0) {
            var target = owner.world.entities
                .GetAll(p => (GuardTarget.position - p).magnitude2 < 50 * 50)
                .OfType<SpaceObject>()
                .Where(o => !o.IsEqual(owner) && GuardTarget.CanTarget(o))
                .GetRandomOrDefault(owner.destiny);
            //If we find a target, start attacking
            if (target != null) {
                Attack(target);
                attackOrder.Update(owner);
                return;
            }
        }
        //At this point, we definitely don't have an attack target so we return
        if ((owner.position - GuardTarget.position).magnitude2 < 6 * 6) {
            owner.dock = new Docking(GuardTarget, GuardTarget is Dockable d ? d.GetDockPoint() : XY.Zero);
        } else {
            approach.Update(owner);
        }
    }
    public bool Active => GuardTarget.active;
}
public class AttackAllOrder : IShipOrder {
    public int sleepTicks;
    public AttackOrder attack;
    public bool CanTarget(SpaceObject other) => other == attack.target;
    public AttackAllOrder() {
        attack = new(null);
    }
    public void Update(AIShip owner) {
        if (sleepTicks > 0) {
            sleepTicks--;
            return;
        }

        if (owner.devices.Weapon.Count == 0) {
            sleepTicks = 150;
            return;
        }
        if (attack.Active == true) {
            attack.Update(owner);
            return;
        }
        //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
        var target = owner.world.entities.all
            .OfType<SpaceObject>()
            .Where(o => owner.IsEnemy(o) && !owner.IsEqual(o))
            .GetRandomOrDefault(owner.destiny);

        //If we can't find a target, then give up for a while
        if (target != null) {
            attack.SetTarget(target);
        } else {
            sleepTicks = 150;
        }
    }
    public bool Active => true;
}
public class AttackGroupOrder : IShipOrder {
    public HashSet<SpaceObject> targets;
    public AttackOrder attackOrder;
    public bool CanTarget(SpaceObject other) => targets.Contains(other);
    public AttackGroupOrder() {
        attackOrder = new AttackOrder(null);
    }
    public void Update(AIShip owner) {
        if (owner.devices.Weapon.Count == 0) {
            return;
        }
        if (attackOrder.target?.active == true) {
            attackOrder.Update(owner);
            return;
        }
        //currentRange is variable and minRange is constant, so weapon dynamics may affect attack range
        targets.RemoveWhere(t => !owner.world.entities.all.Contains(t));
        var target = targets.GetRandomOrDefault(owner.destiny);

        //If we can't find a target, then give up for a while
        if (target != null) {
            attackOrder.SetTarget(target);
        }
    }
    public bool Active => targets.Any();
}
public class AttackOrder : IShipOrder {
    public SpaceObject target { get; private set; }
    public Weapon weapon;
    public List<Weapon> omni=new();
    [JsonProperty]
    private AimOrder aim = new(null, 0);
    [JsonProperty]
    private ApproachOrbitOrder approach = new(null);
    [JsonProperty]
    private GateOrder gate = null;
    [JsonProperty]
    private FaceOrder face = new(0);
    public AttackOrder(SpaceObject target) {
        SetTarget(target);
    }
    public void ClearTarget() => this.target = null;
    public void SetTarget(SpaceObject target) {
        this.target = target;

        this.aim = new(target, 0);
        this.approach = new(target);
    }
    public bool CanTarget(SpaceObject other) => other == target;
    private void Set(Weapon w) => w.SetFiring(true, target);
    public void Update(AIShip owner) {
        if (target == null) {
            return;
        }

        if (gate != null) {
            var gateWorld = gate.gate.world;
            if (gateWorld == owner.world && owner.world != target.world) {
                gate.Update(owner);
                return; 
            } else {
                gate = null;
            }
        }
        if(owner.world != target.world) {
            gate = new GateOrder(owner.world.FindGateTo(target.world));
            gate.Update(owner);
            return;
        }

        var weapons = owner.devices.Weapon;
        if (weapon?.AllowFire != true) {
            var w = weapons.Where(w => w.AllowFire);
            weapon = w.FirstOrDefault(w => w.aiming == null) ?? w.FirstOrDefault();
            if (weapon == null) {
                //omni = null;
                return;
            }
            aim.missileSpeed = weapon.missileSpeed;
            omni.Clear();
            omni.AddRange(w
               .Where(w => w.aiming != null)
               .Where(w => w != weapon));
        } else if (!weapon.ReadyToFire && weapons.Count > 1) {
            weapon = weapons.Where(w => w.ReadyToFire)
                .FirstOrDefault(w => w.aiming == null)
                ?? weapon;
        }
        bool RangeCheck() => (owner.position - target.position).magnitude2 < weapon.currentRange2;
        //Remove dock
        if (owner.dock != null) {
            owner.dock = null;
        }
        var offset = (target.position - owner.position);
        var dist = offset.magnitude;
        omni.ForEach(w => {
            if (dist < w.currentRange) {
                Set(w);
            }
        });
        void SetFiringPrimary() {
            Set(weapon);
        }
        if (dist < 10) {
            //If we are too close, then move away
            //Face away from the target
            face.targetRads = offset.angleRad + Math.PI;
            face.Update(owner);
            //Get moving!
            owner.SetThrusting(true);
        } else {
            bool freeAim = weapon.aiming != null && dist < weapon.currentRange;
            if (dist < weapon.currentRange / 2) {
                //If we are in range, then aim and fire
                //Aim at the target
                aim.Update(owner);
                if (Math.Abs(aim.GetAngleDiff(owner)) < 10
                    && (owner.velocity - target.velocity).magnitude2 < 5 * 5) {
                    owner.SetThrusting(true);
                }
                //Fire if we are close enough
                if (freeAim
                    || Math.Abs(aim.GetAngleDiff(owner)) * dist < 3) {
                    SetFiringPrimary();
                }
            } else {
                //Otherwise, get closer
                approach.Update(owner);
                //Fire if our angle is good enough
                if (freeAim
                    || Math.Abs(aim.GetAngleDiff(owner)) * dist < 3 && RangeCheck()) {
                    SetFiringPrimary();
                }

            }
        }
    }
    public bool Active => target?.active == true;
}

public class GateOrder : IShipOrder {
    public Stargate gate;
    public GateOrder(Stargate gate) {
        this.gate = gate;
        Active = true;
    }
    public bool CanTarget(SpaceObject other) => false;
    public void Update(AIShip owner) {
        if ((owner.position - gate.position).magnitude2 > 10) {
            new ApproachOrbitOrder(gate).Update(owner);
        } else {
            gate.Gate(owner);
            Active = false;
        }
    }
    public bool Active { get; private set; }
}

public class PatrolOrbitOrder : IShipOrder {
    public SpaceObject patrolTarget;
    public double patrolRadius;
    public double attackLimit;
    public AttackOrder attackOrder;
    public int tick;
    public PatrolOrbitOrder(SpaceObject patrolTarget, double patrolRadius) {
        this.patrolTarget = patrolTarget;
        this.patrolRadius = patrolRadius;
        this.attackLimit = 2 * patrolRadius;
        this.attackOrder = new(null);
    }
    public void Update(AIShip owner) {
        tick++;
        //Carry out our current attack order
        if (attackOrder.Active == true) {
            attackOrder.Update(owner);
            return;
        }
        //Look for an attack target periodically
        if (tick % 15 == 0) {
            List<SpaceObject> except = new List<SpaceObject> { owner, patrolTarget };
            var attackLimit2 = attackLimit * attackLimit;
            var attackRange2 = 50 * 50;
            var target = owner.world.entities.all
                .OfType<SpaceObject>()
                .Where(p => (patrolTarget.position - p.position).magnitude2 < attackLimit2)
                .Where(p => (owner.position - p.position).magnitude2 < attackRange2)
                .Where(o => owner.IsEnemy(o))
                .Where(o => !SSpaceObject.IsEqual(o, owner) && !SSpaceObject.IsEqual(o, patrolTarget))
                .GetRandomOrDefault(owner.destiny);
            if (target != null) {
                attackOrder.SetTarget(target);
                attackOrder.Update(owner);
                return;
            }
        }
        var offsetFromTarget = (owner.position - patrolTarget.position);
        var dist = offsetFromTarget.magnitude;
        var deltaDist = patrolRadius - dist;
        var nextDist = Math.Abs(deltaDist) > 10 ?
            dist + Math.Sign(deltaDist) * 10 :
            patrolRadius;
        var nextOffset = offsetFromTarget
            .Rotate(2 * Math.PI / 16)
            .WithMagnitude(nextDist);
        var deltaOffset = nextOffset - offsetFromTarget;
        var Face = new FaceOrder(deltaOffset.angleRad);
        Face.Update(owner);
        owner.SetThrusting(true);
    }
    public bool Active => patrolTarget.active;
}
public class PatrolCircuitOrder : IShipOrder {
    public SpaceObject patrolTarget;
    public List<SpaceObject> nearbyFriends;
    public SpaceObject nearestFriend;
    public double patrolRadius;
    public double attackLimit;
    public AttackOrder attackOrder;
    public FaceOrder face;
    public int tick;
    public PatrolCircuitOrder(SpaceObject patrolTarget, double patrolRadius) {
        this.patrolTarget = patrolTarget;
        this.patrolRadius = patrolRadius;
        this.attackLimit = 2 * patrolRadius;
        this.nearbyFriends = new();
        this.attackOrder = new(null);
        this.face = new(0);
    }
    public void Update(AIShip owner) {
        tick++;
        //If we have an active attack order, then attack!
        if (attackOrder.Active == true) {
            attackOrder.Update(owner);
            return;
        }


        //Look for an attack target periodically
        if (tick % 15 == 0) {
            List<SpaceObject> except = new List<SpaceObject> { owner, patrolTarget };
            var attackLimit2 = attackLimit * attackLimit;
            var attackRange2 = 50 * 50;
            var target = owner.world.entities.all
                .OfType<SpaceObject>()
                .Where(p => (patrolTarget.position - p.position).magnitude2 < attackLimit2)
                .Where(p => (owner.position - p.position).magnitude2 < attackRange2)
                .Where(o => owner.IsEnemy(o))
                .Where(o => !SSpaceObject.IsEqual(o, owner) && !SSpaceObject.IsEqual(o, patrolTarget))
                .GetRandomOrDefault(owner.destiny);

            if (target != null) {
                attackOrder.SetTarget(target);
                attackOrder.Update(owner);
                return;
            }
        }



        //Update our awareness of friendly stations periodically
        if (tick % 300 == 0) {
            var friendlyStations = owner.world.entities.all.OfType<Station>()
                .Where(s => s.sovereign == patrolTarget.sovereign)
                .OrderBy(s => (s.position - patrolTarget.position).magnitude2);
            nearbyFriends = new();
            nearbyFriends.Add(patrolTarget);
            var threshold = 100 * 100;
            foreach (var s in friendlyStations) {
                if (nearbyFriends.Any(f => (f.position - s.position).magnitude2 < threshold)) {
                    nearbyFriends.Add(s);
                }
            }

        }

        var offsetFromTarget = (owner.position - patrolTarget.position);
        var dist = offsetFromTarget.magnitude;

        var patrolRadius = this.patrolRadius;

        //Update our nearest friend periodically
        if (tick % 15 == 0) {
            nearestFriend = nearbyFriends?.OrderBy(s => (s.position - owner.position).magnitude2).FirstOrDefault();
        }

        if (nearestFriend != null) {
            patrolRadius += (nearestFriend.position - patrolTarget.position).magnitude;
        }

        var deltaDist = patrolRadius - dist;

        var nextDist = Math.Abs(deltaDist) > 25 ?
            dist + Math.Sign(deltaDist) * 25 :
            patrolRadius;

        var nextOffset = offsetFromTarget
            .Rotate(2 * Math.PI / 16)
            .WithMagnitude(nextDist);

        var deltaOffset = nextOffset - offsetFromTarget;
        face.targetRads = deltaOffset.angleRad;
        face.Update(owner);
        owner.SetThrusting(true);
    }
    public bool Active => patrolTarget.active;
}

public class SnipeOrder : IShipOrder {
    public SpaceObject target;
    public Weapon weapon;
    [JsonProperty]
    private AimOrder aim;
    public SnipeOrder(SpaceObject target) {
        this.target = target;
        this.aim = new(target, 0);
    }
    public bool CanTarget(SpaceObject other) => other == target;
    public void Update(AIShip owner) {
        var weapons = owner.devices.Weapon;
        if (weapon?.AllowFire != true) {
            weapon = weapons.FirstOrDefault(w => w.AllowFire);
            if (weapon == null) {
                return;
            }
            aim.missileSpeed = weapon.missileSpeed;
        } else if (!weapon.ReadyToFire && weapons.Count > 1) {
            weapon = weapons.FirstOrDefault(w => w.ReadyToFire) ?? weapon;
            aim.missileSpeed = weapon.missileSpeed;
        }
        //Aim at the target
        aim.Update(owner);

        //Fire if we are close enough
        if (weapon.desc.fragment.omnidirectional || Math.Abs(aim.GetAngleDiff(owner)) < 30) {
            weapon.SetFiring(true, target);
        }
    }
    public bool Active => target?.active == true && weapon?.AllowFire == true;
}
public class ApproachOrbitOrder : IShipOrder {
    public SpaceObject target;
    [JsonProperty]
    private FaceOrder face;
    public ApproachOrbitOrder(SpaceObject target) {
        this.target = target;
        this.face = new(0);
    }

    public void Update(AIShip owner) {
        //Remove dock
        owner.dock = null;
        //Find the direction we need to go
        var offset = (target.position - owner.position);
        var randomOffset = new XY((2 * owner.destiny.NextDouble() - 1) * offset.x, (2 * owner.destiny.NextDouble() - 1) * offset.y) / 5;
        offset += randomOffset;
        var speedTowards = (owner.velocity - target.velocity).Dot(offset.normal);
        if (speedTowards < 0) {
            //Decelerate
            face.targetRads = Math.PI + owner.velocity.angleRad;
            face.Update(owner);
            owner.SetThrusting(true);
        } else {
            //Approach

            //Face the target
            face.targetRads = offset.angleRad;
            face.Update(owner);

            //If we're facing close enough
            if (Math.Abs(Helper.AngleDiff(owner.rotationDeg, offset.angleRad * 180 / Math.PI)) < 10 && speedTowards < 10) {

                //Go
                owner.SetThrusting(true);
            }
        }
    }
    public bool Active => true;
}

public class AimOnceOrder : IShipOrder {
    public AimOrder order;
    public AimOnceOrder(BaseShip owner, BaseShip target, double missileSpeed) {
        this.order = new AimOrder(target, missileSpeed);
        Active = true;
    }
    public void Update(AIShip owner) {
        order.Update(owner);
        Active = Math.Abs(order.GetAngleDiff(owner)) > 1;
    }

    public bool Active { get; private set; }
}
public class AimOrder : IShipOrder {
    public SpaceObject target;
    public double missileSpeed;
    public double GetTargetRads(AIShip owner) => Helper.CalcFireAngle(target.position - owner.position, target.velocity - owner.velocity, missileSpeed, out var _);
    public double GetAngleDiff(AIShip owner) => Helper.AngleDiff(owner.rotationDeg, GetTargetRads(owner) * 180 / Math.PI);

    public AimOrder(SpaceObject target, double missileSpeed) {
        this.target = target;
        this.missileSpeed = missileSpeed;
    }
    public bool Active => true;
    public void Update(AIShip owner) {
        var targetRads = this.GetTargetRads(owner);
        var facingRads = owner.stoppingRotation * Math.PI / 180;

        var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        if (ccw < cw) {
            owner.SetRotating(Rotating.CCW);
        } else if (cw < ccw) {
            owner.SetRotating(Rotating.CW);
        }
    }
}
public class FaceOrder : IShipOrder {
    public double targetRads;
    public FaceOrder(double targetRads) {
        this.targetRads = targetRads;
    }
    public void Update(AIShip owner) {
        var facingRads = owner.ship.stoppingRotationWithCounterTurn * Math.PI / 180;

        var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(targetRads)).magnitude2;
        if (ccw < cw) {
            owner.SetRotating(Rotating.CCW);
        } else if (cw < ccw) {
            owner.SetRotating(Rotating.CW);
        } else {
            if (owner.ship.rotatingVel > 0) {
                owner.SetRotating(Rotating.CW);
            } else {
                owner.SetRotating(Rotating.CCW);
            }
        }
    }
    public bool Active => true;
}
