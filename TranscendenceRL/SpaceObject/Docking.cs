﻿using Common;

namespace TranscendenceRL {
    public class Docking {
        public SpaceObject Target;
        public XY Offset;
        public bool docked;
        public bool justDocked;
        public Docking() {}
        public Docking(SpaceObject Target) {
            this.Target = Target;
            this.Offset = new XY(0, 0);
        }
        public Docking(SpaceObject Target, XY Offset) {
            this.Target = Target;
            this.Offset = Offset;
        }
        public void Update(IShip owner) {
            if(!docked) {
                docked = UpdateDocking(owner);
                if(docked) {
                    justDocked = true;
                }
            } else {
                owner.position = Target.position;
                owner.velocity = Target.velocity;
            }
        }
        public bool UpdateDocking(IShip ship) {
            double decel = ship.shipClass.thrust / 2 * Program.TICKS_PER_SECOND;
            double stoppingTime = (ship.velocity - Target.velocity).magnitude / decel;
            double stoppingDistance = ship.velocity.magnitude * stoppingTime - (decel * stoppingTime * stoppingTime) / 2;
            var stoppingPoint = ship.position;
            if (!ship.velocity.isZero) {
                stoppingPoint += ship.velocity.normal * stoppingDistance;
            }

            var dest = Target.position + Offset;
            var offset = dest + (Target.velocity * stoppingTime) - stoppingPoint;

            if (offset.magnitude > 0.25) {
                ship.velocity += XY.Polar(offset.angleRad, ship.shipClass.thrust);
            } else if ((ship.position - dest).magnitude < 1) {
                ship.velocity = Target.velocity;
                return true;
            }
            return false;
        }
    }
}
