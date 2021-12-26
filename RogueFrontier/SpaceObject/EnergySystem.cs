﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RogueFrontier;

public class EnergySystem {
    public DeviceSystem devices;
    public HashSet<Device> on => devices.Installed.Except(off).ToHashSet();
    public HashSet<Device> off = new();
    public int totalMaxOutput;
    public int totalUsedOutput;
    public EnergySystem(DeviceSystem devices) {
        this.devices = devices;
    }
    public void Update(PlayerShip player) {
        var reactors = devices.Reactors;
        if (!reactors.Any()) {
            return;
        }

        var solars = devices.Solars;
        var generators = new List<Reactor>();
        var batteries = new List<Reactor>();
        foreach(var s in solars) {
            s.energyDelta = 0;
        }
        foreach (var r in reactors) {
            r.energyDelta = 0;
            if (r.desc.battery) {
                batteries.Add(r);
            } else {
                generators.Add(r);
            }
        }
        List<PowerSource> sources = new();
        sources.AddRange(solars);
        sources.AddRange(generators);
        sources.AddRange(batteries);

        totalMaxOutput = sources.Sum(r => r.maxOutput);
        int maxOutputLeft = totalMaxOutput;
        int sourceIndex = 0;
        int sourceOutput = sources[sourceIndex].maxOutput;
        HashSet<Device> deactivated = new HashSet<Device>();
        //Devices consume power
        int outputUsed = 0;
        foreach (var powered in devices.Powered.Where(p => !off.Contains(p))) {
            var powerUse = powered.powerUse.Value;
            if (powerUse <= 0) { continue; }
            if (powerUse > maxOutputLeft) {
                powered.OnOverload();
                powerUse = powered.powerUse.Value;
                if (powerUse <= 0) { continue; }
                if (powerUse > maxOutputLeft) {
                    deactivated.Add(powered);
                    continue;
                }
            }
            outputUsed += powerUse;
            maxOutputLeft -= powerUse;

        CheckReactor:
            var source = sources[sourceIndex];

            if (source is Reactor r && r.desc.battery) {
                r.rechargeDelay = 60;
            }
            if (outputUsed > sourceOutput) {
                outputUsed -= sourceOutput;
                source.energyDelta = -sourceOutput;
                //Go to the next reactor
                sourceIndex++;
                sourceOutput = sources[sourceIndex].maxOutput;
                goto CheckReactor;
            } else {
                source.energyDelta = -outputUsed;
            }
        }


        if (deactivated.Any()) {
            foreach(var d in deactivated) {
                d.OnDisable();
                off.Add(d);
            }
            player.AddMessage(new Message("Reactor output overload!"));
            foreach (var d in deactivated) {
                player.AddMessage(new Message($"{d.source.type.name} deactivated!"));
            }
        }

        //Batteries recharge from reactor
        int maxReactorOutputLeft = maxOutputLeft - batteries.Sum(b => b.maxOutput);
        foreach (var battery in batteries.Where(b => b.energy < b.desc.capacity)) {
            if (maxReactorOutputLeft == 0) {
                continue;
            }
            if (battery.rechargeDelay > 0) {
                battery.rechargeDelay--;
                continue;
            }

            int delta = Math.Min(battery.maxOutput, maxReactorOutputLeft);
            battery.energyDelta = delta;

            outputUsed += delta;
            maxReactorOutputLeft -= delta;

        CheckReactor:
            if (outputUsed > sourceOutput) {
                outputUsed -= sourceOutput;
                sources[sourceIndex].energyDelta = -sourceOutput;

                //Go to the next reactor
                sourceIndex++;
                sourceOutput = sources[sourceIndex].maxOutput;
                goto CheckReactor;
            } else {
                sources[sourceIndex].energyDelta = -outputUsed;
            }
        }

        totalUsedOutput = totalMaxOutput - maxOutputLeft;
    }
}
