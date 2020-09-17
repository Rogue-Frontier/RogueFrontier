﻿using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace TranscendenceRL.Types {
    public class PowerType : DesignType {
        public string name;
        public int cooldownTime;
        public int invokeDelay;

        public PowerEffect Effect;
        public void Initialize(TypeCollection collection, XElement e) {
            name = e.ExpectAttribute(nameof(name));
            cooldownTime = e.ExpectAttributeInt(nameof(cooldownTime));
            invokeDelay = e.ExpectAttributeInt(nameof(invokeDelay));

            if(e.HasElement("Weapon", out XElement weapon)) {
                Effect = new PowerWeapon(new FragmentDesc(weapon));
            }
        }
    }
    //Interface for invokable powers
    public interface PowerEffect {
        void Invoke(PlayerShip invoker);
    }
    //Power that generates a weapon effect
    public class PowerWeapon : PowerEffect {
        FragmentDesc desc;
        public PowerWeapon(FragmentDesc desc) {
            this.desc = desc;
        }
        public void Invoke(PlayerShip invoker) => SWeapon.CreateShot(desc, invoker, invoker.rotationDegrees * Math.PI / 180);
    }
    public class Power {
        public PowerType type;

        public int cooldownPeriod => type.cooldownTime;
        public int invokeDelay => type.invokeDelay;

        public int cooldownLeft;
        public bool ready => cooldownLeft == 0;
        public int invokeCharge;
        public bool charging;
        public Power(PowerType type) {
            this.type = type;
        }
    }
}