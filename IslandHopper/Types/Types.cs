﻿using Common;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace IslandHopper {
	public class TypeCollection {
		public Dictionary<string, XElement> sources;
        public Dictionary<string, DesignType> all;
        public Dictionary<string, ItemType> itemType;
		enum InitState {
			Uninitialized,
			Initializing,
			Initialized
		}
		InitState state;

		//After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
		public TypeCollection(XElement root) {
			sources = new Dictionary<string, XElement>();
			all = new Dictionary<string, DesignType>();
            itemType = new Dictionary<string, ItemType>();
			state = InitState.Uninitialized;

			Debug.Print("TypeCollection created");

			//We do two passes
			//The first pass creates DesignType references for each type and stores the source code
			ProcessRoot(root);
			//The second pass initializes each type from the source code
			Initialize();
		}
		void Initialize() {
			state = InitState.Initializing;
            //We don't evaluate all sources; just the ones that are used by DesignTypes
			foreach(string key in all.Keys.ToList()) {
				DesignType type = all[key];
				XElement source = sources[key];
				type.Initialize(this, source);
			}
			state = InitState.Initialized;
		}
		void ProcessRoot(XElement root) {
			foreach (var element in root.Elements()) {
				ProcessElement(element);
			}
		}
		public void ProcessElement(XElement element) {
			switch (element.Name.LocalName) {
				case "Module":
					XElement module = XDocument.Load(element.ExpectAttribute("file")).Root.ExpectElement("IslandHopperModule");
					ProcessRoot(module);
					break;
                case "Source":
                    AddSource(element);
                    break;
				case "ItemType":
					AddType<ItemType>(element);
					break;
				default:
					//throw new Exception($"Unknown element <{element.Name}>");
					Debug.Print($"Unknown element <{element.Name}>");
					break;
			}
		}
        void AddSource(XElement element) {
            if (!element.TryAttribute("type", out string type)) {
                //throw new Exception("DesignType requires type attribute");
                type = System.Guid.NewGuid().ToString();
            }

            if (sources.ContainsKey(type)) {
                throw new Exception($"DesignType type conflict: {type}");
            } else {
                Debug.Print($"Created <{element.Name}> of type {type}");
                sources[type] = element;
            }
        }
		void AddType<T>(XElement element) where T : DesignType, new() {
			if (!element.TryAttribute("type", out string type)) {
				//throw new Exception("DesignType requires type attribute");
				type = System.Guid.NewGuid().ToString();
			}

			if (sources.ContainsKey(type)) {
				throw new Exception($"DesignType type conflict: {type}");
			} else {
				Debug.Print($"Created <{element.Name}> of type {type}");
				sources[type] = element;
				all[type] = new T();
                switch(all[type]) {
                    case ItemType it:
                        itemType[type] = it;
                        break;
                }

				//If we're uninitialized, then we will definitely initialize later
				if(state != InitState.Uninitialized) {
                    //Otherwise, initialize now
					all[type].Initialize(this, sources[type]);
				}
			}
		}
		public bool Lookup(string type, out DesignType result) {
			return all.TryGetValue(type, out result);
		}
        public bool Lookup<T>(string type, out T result) where T:DesignType {
            if(all.TryGetValue(type, out var t) && t is T) {
                result = (T)t;
                return true;
            } else {
                result = default(T);
                return false;
            }
        }
        public T Lookup<T>(string type) where T:DesignType {
            if(all.TryGetValue(type, out var t) && t is T) {
                return (T)t;
            } else {
                return default(T);
            }
        }
    }
	public interface DesignType {
		void Initialize(TypeCollection collection, XElement e);
	}
	public class ItemType : DesignType {
        public string name, desc;
        public double mass;

		//Identity identity;
		public int knownChance;
        public ItemType unknownType;
        public GunType gun;
        public GrenadeType grenade;

        public Item GetItem(Island World, XYZ Position) {
            Item i = new Item() {
                type = this,
                World = World,
                Position = Position,
                Velocity = new XYZ(),
            };
            i.Gun = gun?.CreateGun();
            i.Grenade = grenade?.GetGrenade(i);
            return i;
        }

		public void Initialize(TypeCollection collection, XElement e) {
			name = e.ExpectAttribute("name");
			desc = e.ExpectAttribute("desc");
			mass = e.TryAttributeDouble("mass", 0);

			switch (e.Attribute("known")?.Value) {
				case "true":
					knownChance = 100;
					break;
				case "common":
					knownChance = 80;
					break;
				case "uncommon":
					knownChance = 60;
					break;
				case "rare":
					knownChance = 40;
					break;
				case "exotic":
					knownChance = 20;
					break;
				case "false":
					knownChance = 0;
					break;
			}
			string unknownType = e.TryAttribute("unknownType");
			if (!string.IsNullOrWhiteSpace(unknownType)) {
				if (collection.Lookup(unknownType, out DesignType d) && d is ItemType it) {
					this.unknownType = it;
				} else {
					throw new Exception($"Unknown DesignType: {unknownType}");
				}
			}
            if (e.HasElement(GrenadeType.Tag, out XElement grenade)) {
                this.grenade = new GrenadeType(collection, grenade);
            }
            //If we have a gun, initialize it now
            if (e.HasElement(GunType.Tag, out XElement gun)) {
				this.gun = new GunType(collection, gun);
			}
            
            //Initialize our nested types now (they are not accessible to anyone else at bind time)
            foreach (var inner in e.Elements("ItemType")) {
				collection.ProcessElement(inner);
			}
		}
        public class GrenadeType {
            public static string Tag = "Grenade";
            //TO DO: Implement DetonateOnDamage / DeonateOnImpact
            public bool detonateOnDamage;
            public bool detonateOnImpact;
            public bool canArm;
            public int fuseTime;
            public int explosionRadius;
            public int explosionDamage;
            public int explosionForce;

            public GrenadeType(TypeCollection collection, XElement e) {
                string inherit = e.TryAttribute(nameof(inherit), null);
                if (inherit != null) {
                    var source = collection.sources[inherit].Element(Tag);
                    e.InheritAttributes(source);
                }

                detonateOnDamage = e.TryAttributeBool(nameof(detonateOnDamage), true);
                detonateOnImpact = e.TryAttributeBool(nameof(detonateOnImpact), false);
                canArm = e.TryAttributeBool(nameof(canArm), true);
                fuseTime = e.TryAttributeInt(nameof(fuseTime), 5);
                explosionDamage = e.TryAttributeInt(nameof(explosionDamage), 5);
                explosionForce = e.TryAttributeInt(nameof(explosionForce), 5);
                explosionRadius = e.TryAttributeInt(nameof(explosionRadius), 5);
            }
            public Grenade GetGrenade(IItem item) => new Grenade(item) {
                type = this,
                Armed = false,
                Countdown = fuseTime
            };
        }
        public class GunType {
            public static string Tag = "Gun";
			public Gun CreateGun() => new Gun() {
                gunType = this,
                AmmoLeft = initialAmmo,
                ClipLeft = initialClip,
                FireTimeLeft = 0,
                ReloadTimeLeft = 0
            };
            private string inherit;
            private HashSet<string> sourceAttributes;
			public enum ProjectileType {
				beam, bullet
			}
            public ProjectileType projectile;
            public int difficulty;
            public int recoil;
            public int noiseRange;
            public int damage;
            public int bulletSpeed;

            public int bulletsPerShot;
            public int spread;
            public int knockback;
            public int fireTime;
            public int reloadTime;

            public int clipSize;
            public int maxAmmo;

            public int initialClip;
            public int initialAmmo;

            //TO DO: Implement critOnLastShot


			public GunType(TypeCollection collection, XElement e) {
                //Don't modify the original source when we inherit
                e = new XElement(e);
                string inherit = e.TryAttribute(nameof(inherit), null);
                if (inherit != null) {
                    var source = collection.sources[inherit].Element(Tag);
                    e.InheritAttributes(source);
                }
                if (!Enum.TryParse(e.TryAttribute(nameof(projectile)), out projectile)) {
					projectile = ProjectileType.bullet;
				}
                Dictionary<string, int> difficultyMap = new Dictionary<string, int> {
                    { "none", 0 },
                    { "easy", 20 },
                    { "medium", 40 },
                    { "hard", 60 },
                    { "expert", 80 },
                    { "master", 100 },
                };
                difficulty = difficultyMap.TryLookup(e.TryAttribute(nameof(difficulty)), 0);
                recoil = e.TryAttributeInt(nameof(recoil), 0);
				noiseRange = e.TryAttributeInt(nameof(noiseRange), 0);
				damage = e.TryAttributeInt(nameof(damage), 0);
				bulletSpeed = e.TryAttributeInt(nameof(bulletSpeed), 0);

                bulletsPerShot = e.TryAttributeInt(nameof(bulletsPerShot), 0);
                knockback = e.TryAttributeInt(nameof(knockback), 0);
                spread = e.TryAttributeInt(nameof(spread), 0);
                fireTime = e.TryAttributeInt(nameof(fireTime), 0);
                reloadTime = e.TryAttributeInt(nameof(reloadTime), 0);

                clipSize = e.TryAttributeInt(nameof(clipSize), 0);
                maxAmmo = e.TryAttributeInt(nameof(maxAmmo), 0);

                initialClip = e.TryAttributeInt(nameof(initialClip), clipSize);
                initialAmmo = e.TryAttributeInt(nameof(initialAmmo), maxAmmo);
            }
		}
        class Symbol {
			private char c;
			private Color background, foreground;
			public Symbol(XElement e) {
				c = e.TryAttribute("char", "?")[0];
				background = (Color)typeof(Color).GetProperty(e.TryAttribute("background", "black")).GetValue(null, null);
				foreground = (Color)typeof(Color).GetProperty(e.TryAttribute("foreground", "red")).GetValue(null, null);
			}
			public ColoredGlyph String => new ColoredGlyph(c, background, foreground);
		}
	}
}