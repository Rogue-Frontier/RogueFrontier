﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
    class PlayerMain : Window {
        Island World;
        public PlayerMain(int Width, int Height, Island world) : base(Width, Height) {
            
            Theme = new WindowTheme {
                ModalTint = Color.Transparent,
                FillStyle = new Cell(Color.White, Color.Black),
            };
            UseKeyboard = true;
            UseMouse = true;
            this.World = world;
            this.Transparent();
            
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);

            World.entities.UpdateSpace();       //	Update all entity positions on the grid
            foreach (var e in World.entities.all) {
                e.UpdateRealtime(delta);
            }

            World.effects.UpdateSpace();
            foreach(var e in World.effects.all) {
                e.UpdateRealtime(delta);
            }

            if (World.player.AllowUpdate()) {
                this.DebugInfo("Global Update");
                foreach (var e in World.entities.all.ToList()) {
                    e.DebugInfo("UpdateStep() by world");
                    e.UpdateStep();
                }
                foreach (var e in World.effects.all.ToList()) {
                    e.DebugInfo("UpdateStep() by world");
                    e.UpdateStep();
                }
                World.camera = World.player.Position;
            } else {
                //System.Console.WriteLine("not updating");
            }

            var Removed = new List<Entity>();
            World.entities.all.RemoveWhere(e => {
                bool result = !e.Active;
                if (result) {
                    Removed.Add(e);
                }
                return result;
            });
            Removed.ForEach(e => e.OnRemoved());

            World.effects.all.RemoveWhere(e => !e.Active);

        }
        public override void Draw(TimeSpan delta) {
            //this.Info($"Drawing {DateTime.Now.Millisecond}");
            Clear();
            this.DebugInfo($"Draw({delta})");
            Print(1, 1, "" + World.player.Position.z, Color.White);
            Print(1, 2, "" + World.camera.z, Color.White);
            for (int i = 0; i < 30 && i < World.player.HistoryRecent.Count(); i++) {
                var entry = World.player.HistoryRecent[World.player.HistoryRecent.Count() - 1 - i];
                var y = (Height - 1) - i;
                Print(1, y, entry.ScreenTime > 1 ? entry.Desc : entry.Desc.Opacity((byte) (255 * entry.ScreenTime)));
            }

            int PreviewWidth = 20;
            int PreviewHeight = 20;

            int previewX = Width - PreviewWidth / 2;
            int previewY = PreviewWidth / 2;
            //Draw a border, up/down arrow, and z difference
            foreach (var thrown in World.player.Projectiles) {
                for (int x = -PreviewWidth/2; x < PreviewWidth/2; x++) {
                    for (int y = -PreviewHeight/2; y < PreviewHeight/2; y++) {
                        XYZ location = thrown.Position + new XYZ(x, y, 0);
                        Print(x + previewX, y + previewY, World.GetGlyph(location));
                    }
                }
                previewY += PreviewHeight;
            }


            base.Draw(delta);
        }
        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            var player = World.player;
            if (info.IsKeyDown(Keys.Up) || info.IsKeyDown(Keys.Down) || info.IsKeyDown(Keys.Right) || info.IsKeyDown(Keys.Left)) {
                XYZ direction = new XYZ();
                if (info.IsKeyDown(Keys.Up)) {
                    direction += new XYZ(0, -1);
                }
                if (info.IsKeyDown(Keys.Down)) {
                    direction += new XYZ(0, 1);
                }
                if (info.IsKeyDown(Keys.Right)) {
                    direction += new XYZ(1, 0);
                }
                if (info.IsKeyDown(Keys.Left)) {
                    direction += new XYZ(-1, 0);
                }
                if(direction.Magnitude > 0) {
                    direction = direction.Normal;
                    if (info.IsKeyDown(Keys.RightControl)) {
                        //Run
                        if (World.player.OnGround() && !World.player.Actions.Any(a => a is Jump)) {
                            double runAccel = 2;
                            int runCooldown = 10;
                            int runTime = 10;
                            World.player.Actions.Add(new Jump(World.player, direction * runAccel, runCooldown, runTime));
                        }
                    } else {
                        if (World.player.OnGround() && !World.player.Actions.Any(a => a is WalkAction))
                            World.player.Actions.Add(new WalkAction(World.player, direction));
                    }
                }
                
            }

            //Other actions besides walking/running
            if (info.IsKeyDown(Keys.OemOpenBrackets)) {
                //Go up stairs
            } else if(info.IsKeyDown(Keys.OemCloseBrackets)) {
                //Go down stairs
            } else if (info.IsKeyDown(Keys.J)) {
                //Jump up
                if (player.OnGround() && !player.Actions.Any(a => a is Jump))
                    player.Actions.Add(new Jump(player, new XYZ(0, 0, 5)));
			} else if (info.IsKeyPressed(Keys.D)) {
				new ListMenu<IItem>(Width, Height, "Select inventory items to drop. Press ESC to finish.", player.Inventory.Select(Item => new ListItem(Item)), item => {
                    //Just drop the item for now
                    player.Inventory.Remove(item);
					World.entities.Place(item);

					World.player.Witness(new InfoEvent(new ColoredString("You drop: ") + item.Name.WithBackground(Color.Black)));
					return true;
				}).Show(true);
			} else if(info.IsKeyPressed(Keys.U)) {
                //World.AddEntity(new ExplosionSource(World, World.player.Position, 10));
                //Use menu
                new ListMenu<IItem>(Width, Height, "Select items to use. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {

                    if(item.Grenade != null && !item.Grenade.Armed) {
                        item.Grenade.Arm();
                        player.Witness(new InfoEvent(new ColoredString("You arm: ") + item.Name.WithBackground(Color.Black)));
                    }
                    return false;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.G)) {
                new ListMenu<IItem>(Width, Height, "Select items to get. Press ESC to finish.", World.entities[World.player.Position].OfType<IItem>().Select(Item => new ListItem(Item)), item => {
                    //Just take the item for now
                    World.player.Inventory.Add(item);
                    World.entities.Remove(item);

                    World.player.Witness(new InfoEvent(new ColoredString("You get: ") + item.Name.WithBackground(Color.Black)));
                    return true;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.I)) {
                new ListMenu<IItem>(Width, Height, "Select inventory items to examine. Press ESC to finish.", World.player.Inventory.Select(Item => new ListItem(Item)), item => {
                    //	Later, we might have a chance of identifying the item upon selecting it in the inventory

                    //World.player.Witness(new SelfEvent(new ColoredString("You examine: ") + item.Name.WithBackground(Color.Black)));
                    return false;
                }).Show(true);
            } else if (info.IsKeyPressed(Keys.L)) {
                new LookMenu(Width, Height, World).Show(true);
            } else if(info.IsKeyPressed(Keys.S)) {
                new ShootMenu(Width, Height, World, World.player).Show(true);
            } else if (info.IsKeyPressed(Keys.T)) {
                new ThrowMenu(Width, Height, World, World.player).Show(true);
            } else if(info.IsKeyDown(Keys.OemPeriod) && info.IsKeyDown(Keys.RightControl)) {
                if (!World.player.Actions.Any(a => a is WaitAction)) {
                    World.player.Actions.Add(new WaitAction(1));
                }
            } else if(info.IsKeyPressed(Keys.OemPeriod)) {
                if (!World.player.Actions.Any(a => a is WaitAction)) {
                    Debug.Print("waiting");
                    World.player.Actions.Add(new WaitAction(Constants.STEPS_PER_SECOND));
                    World.player.Witness(new InfoEvent(new ColoredString("You wait")));
                }
            }
            return base.ProcessKeyboard(info);
        }
    }
    interface ListChoice<T> {
        T Value { get; }
        ColoredGlyph GetSymbolCenter();
        ColoredString GetName();
    }
    class ListItem : ListChoice<IItem> {
        public IItem Value { get; }
        public ListItem(IItem Value) {
            this.Value = Value;
        }
        public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
        public ColoredString GetName() => Value.Name;
    }
    class ListEntity : ListChoice<Entity> {
        public Entity Value { get; }
        public ListEntity(Entity Value) {
            this.Value = Value;
        }
        public ColoredGlyph GetSymbolCenter() => Value.SymbolCenter;
        public ColoredString GetName() => Value.Name;
    }
    class ListMenu<T> : Window {
        string hint;
        HashSet<ListChoice<T>> Choices;
        Func<T, bool> select;       //Fires when we select an item. If true, then we remove the item from the selections
        int startIndex;
        public static ListMenu<IItem> itemSelector(int Width, int Height, string hint, IEnumerable<IItem> Items, Func<IItem, bool> select) {
            return new ListMenu<IItem>(Width, Height, hint, Items.Select(item => new ListItem(item)), select);
        }
        public ListMenu(int Width, int Height, string hint, IEnumerable<ListChoice<T>> Choices, Func<T, bool> select) : base(Width, Height) {
            Theme = Themes.Sub;
            this.hint = hint;
            this.Choices = new HashSet<ListChoice<T>>(Choices);
            this.select = select;
            startIndex = 0;

            this.Transparent();
            Theme.ModalTint = Color.Transparent;
        }
        public override void Update(TimeSpan delta) {
            base.Update(delta);
        }
        public override void Draw(TimeSpan delta) {
            this.Clear();
            int x = 5;
            int y = 5;
            Print(x, y, hint, Color.White, Color.Black);
            y++;
            if (Choices.Count > 0) {
                string UP = ((char)24).ToString();
                string LEFT = ((char)27).ToString();
                Print(x, y, "    ", foreground: Color.White, background: Color.Black);
                if (CanScrollUp) {
                    Print(x, y, UP, Color.White, Color.Black);
                    if (CanPageUp)
                        Print(x + 2, y, LEFT, Color.White, Color.Black);
                    Print(x + 4, y, startIndex.ToString(), Color.White, Color.Black);
                } else {
                    Print(x, y, "-", Color.White, Color.Black);
                }
                y++;

                List<ListChoice<T>> list = Choices.ToList();
                for (int i = startIndex; i < startIndex + 26; i++) {
                    if (i < Choices.Count) {
                        char binding = (char)('a' + (i - startIndex));
                        Print(x, y, "" + binding, Color.LimeGreen, Color.Transparent);
                        Print(x + 1, y, " ", Color.Black, Color.Black);
                        Print(x + 2, y, list[i].GetSymbolCenter().ToColoredString());
                        Print(x + 3, y, " ", Color.Black, Color.Black);
                        Print(x + 4, y, list[i].GetName());
                    } else {
                        Print(x, y, ".", Color.Gray, Color.Black);
                    }
                    y++;
                }

                string DOWN = ((char)25).ToString();
                string RIGHT = ((char)26).ToString();
                Print(x, y, "    ", foreground:Color.White, background: Color.Black);
                if (CanScrollDown) {
                    Print(x, y, DOWN, Color.White, Color.Black);
                    if (CanPageDown)
                        Print(x + 2, y, RIGHT, Color.White, Color.Black);
                    Print(x + 4, y, ((Choices.Count - 26) - startIndex).ToString(), Color.White, Color.Black);
                } else {
                    Print(x, y, "-", Color.White, Color.Black);
                }

                y++;
            } else {
                Print(x, y, "There is nothing here.", Color.Red, Color.Black);
            }

            base.Draw(delta);
        }
        private bool CanScrollUp => startIndex > 0;
        private bool CanPageUp => startIndex - 25 > 0;
        private bool CanScrollDown => startIndex + 26 < Choices.Count;
        private bool CanPageDown => startIndex + 26 + 25 < Choices.Count;
        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            if (info.IsKeyPressed(Keys.Escape)) {
                Hide();
            } else {
                ListControls(info);
            }
            return true;
        }
        public void ListControls(SadConsole.Input.Keyboard info) {
            if (info.IsKeyPressed(Keys.Up)) {
                if (CanScrollUp)
                    startIndex--;
            } else if (info.IsKeyPressed(Keys.Down)) {
                if (CanScrollDown)
                    startIndex++;
            } else if (info.IsKeyPressed(Keys.Left)) {
                if (CanPageUp)
                    startIndex -= 26;
                else
                    startIndex = 0;
            } else if (info.IsKeyPressed(Keys.Right)) {
                if (CanPageDown)
                    startIndex += 26;
                else
                    startIndex = Math.Max(0, Choices.Count - 26);
            } else {
                //If this key represents an item, then we select it
                foreach (var k in info.KeysPressed) {
                    var key = k.Key;
                    if (Keys.A <= key && key <= Keys.Z) {
                        //A represents the first displayed item (i.e. the one at startIndex). Z represents the last displayed item (startIndex + 25)
                        int index = (key - Keys.A) + startIndex;
                        if (index < Choices.Count) {
                            //Select the item
                            ListChoice<T> selected = Choices.ToList()[index];
                            if (select.Invoke(selected.Value)) {
                                Choices.Remove(selected);

                                //If we're at the bottom of the menu and we're removing an item here, move the list view up so that we don't have empty slots
                                if (Choices.Count > 25 && !CanPageDown) {
                                    startIndex = Choices.Count - 26;
                                }
                            }

                        }
                        break;
                    }
                }
            }
        }
    }
    class ShootMenu : Window {
        Island w;
        Player p;
        ListMenu<IItem> itemSelector;
        LookMenu targetSelector;

        public ShootMenu(int width, int height, Island w, Player p) : base(width, height) {
            Theme = Themes.Sub;
            this.w = w;
            this.p = p;

            this.Transparent();
        }
        public override void Update(TimeSpan time) {
            base.Update(time);
            if (itemSelector == null) {
                UpdateItemSelector();
            }
            if (targetSelector != null) {
                targetSelector.Update(time);
            } else {
                itemSelector.Update(time);
            }
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();
            base.Draw(drawTime);
            if (targetSelector != null) {
                targetSelector.Draw(drawTime);
            } else {
                itemSelector.Draw(drawTime);
            }
        }
        public void UpdateItemSelector() {
            Hide();
            itemSelector = new ListMenu<IItem>(Width, Height, "Select item to shoot with. ESC to cancel.", p.Inventory.Select(Item => new ListItem(Item)), item => {
                itemSelector.Hide();
                targetSelector = new LookMenu(Width, Height, w, "Select target to shoot at. Enter to select a general location. ESC to cancel.", target => {
                    targetSelector.Hide();
                    Shoot(item, target);
                    return false;
                }, xyz => {
                    Shoot(item, xyz);
                    targetSelector.Hide();
                });
                targetSelector.Show(true);
                return false;
            });
            itemSelector.Show(true);
        }
        public void Shoot(IItem item, Entity target) {
            var bulletSpeed = 30;
            var bulletVel = (target.Position - p.Position).Normal * bulletSpeed;
            Bullet b = new Bullet(p, item, target, bulletVel);
            w.AddEntity(b);
            p.Projectiles.Add(b);
            p.Witness(new InfoEvent(new ColoredString("You shoot: ") + item.Name.WithBackground(Color.Black) + new ColoredString(" | at: ") + target.Name.WithBackground(Color.Black)));
        }
        public void Shoot(IItem item, XYZ target) {
            var bulletSpeed = 30;
            var bulletVel = (target - p.Position).Normal * bulletSpeed;
            Bullet b = new Bullet(p, item, null, bulletVel);
            w.AddEntity(b);
            p.Projectiles.Add(b);
            p.Witness(new InfoEvent(new ColoredString("You shoot: ") + item.Name.WithBackground(Color.Black)));
        }
        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            if (info.IsKeyDown(Keys.Escape)) {
                if (targetSelector != null) {
                    targetSelector.Hide();
                    targetSelector = null;
                    UpdateItemSelector();
                } else {
                    itemSelector.Hide();
                }
                return true;
            } else {
                if (targetSelector != null) {
                    return targetSelector.ProcessKeyboard(info);
                } else {
                    return itemSelector.ProcessKeyboard(info);
                }
            }
        }
        class FireMenu : Window {
            Player p;
            Gun g;
            Entity target;
            public FireMenu(int Width, int Height, Player p, Gun g, Entity target) : base(Width, Height) {
                this.p = p;
                this.g = g;
                this.target = target;
            }
        }
    }
    class ThrowMenu : Window {
        Island w;
        Player p;
        ListMenu<IItem> itemSelector;
        LookMenu targetSelector;
        public ThrowMenu(int width, int height, Island w, Player p) : base(width, height) {
            Theme = Themes.Sub;

            this.w = w;
            this.p = p;

            this.Transparent();
        }
        public override void Update(TimeSpan time) {
            base.Update(time);
            if (itemSelector == null) {
                UpdateItemSelector();
            }
            if (targetSelector != null) {
                targetSelector.Update(time);
            } else {
                itemSelector.Update(time);
            }
        }
        public override void Draw(TimeSpan drawTime) {
            this.Clear();
            base.Draw(drawTime);
            if (targetSelector != null) {
                targetSelector.Draw(drawTime);
            } else {
                itemSelector.Draw(drawTime);
            }
        }
        public void UpdateItemSelector() {
            Hide();
            itemSelector = new ListMenu<IItem>(Width, Height, "Select item to throw. ESC to cancel.", p.Inventory.Select(Item => new ListItem(Item)), item => {
                itemSelector.Hide();
                targetSelector = new LookMenu(Width, Height, w, "Select target to throw item at. Enter to select a general location. ESC to cancel.", target => {
                    targetSelector.Hide();

                    ThrowItem(target, item);
                    return false;
                }, point => {
                    targetSelector.Hide();
                    ThrowItem(point, item);
                });
                targetSelector.Show(true);
                return false;
            });
            itemSelector.Show(true);
        }
        public void ThrowItem(Entity target, IItem item) {
            if (Helper.CalcAim2(target.Position - p.Position, 60, out XYZ lower, out XYZ _)) {
                item.Velocity = lower / Constants.STEPS_PER_SECOND;
                //Remove the item from the player's inventory and create a thrown item in the world
                p.Inventory.Remove(item);
                var t = new ThrownItem(p, item);
                w.AddEntity(t);
                //Track this on the player
                p.Projectiles.Add(t);
                p.Witness(new InfoEvent(new ColoredString("You throw: ") + item.Name.WithBackground(Color.Black) + new ColoredString(" | at: ") + target.Name.WithBackground(Color.Black)));
            }
        }
        public void ThrowItem(XYZ target, IItem item) {
            if (Helper.CalcAim2(target - p.Position, 60, out XYZ lower, out XYZ _)) {
                item.Velocity = lower / Constants.STEPS_PER_SECOND;
                //Remove the item from the player's inventory and create a thrown item in the world
                p.Inventory.Remove(item);
                var t = new ThrownItem(p, item);
                w.AddEntity(t);
                //Track this on the player
                p.Projectiles.Add(t);
                p.Witness(new InfoEvent(new ColoredString("You throw: ") + item.Name.WithBackground(Color.Black)));
            }
        }
        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            if (info.IsKeyDown(Keys.Escape)) {
                if (targetSelector != null) {
                    targetSelector.Hide();
                    targetSelector = null;
                    UpdateItemSelector();
                } else {
                    itemSelector.Hide();
                }
                return true;
            } else {
                if (targetSelector != null) {
                    return targetSelector.ProcessKeyboard(info);
                } else {
                    return itemSelector.ProcessKeyboard(info);
                }
            }
        }
    }
    class LookMenu : Window {
        Island world;

        string hint;
        Func<Entity, bool> select;
        Action<XYZ> selectAt;

        Timer cursorBlink;
        bool cursorVisible;

        ListMenu<Entity> examineMenu;

        readonly ColoredGlyph cursor = new ColoredGlyph('?', Color.Yellow, Color.Black);
        
        public LookMenu(int width, int height, Island world, string hint = null, Func<Entity, bool> select = null, Action<XYZ> selectAt = null) : base(width, height) {
            this.world = world;
            this.hint = hint ?? "Select an entity to examine";
            this.select = select ?? (e => false);
            this.selectAt = selectAt ?? (xyz => { });
            cursorVisible = true;
            cursorBlink = new Timer(0.4, () => {
                cursorVisible = !cursorVisible;
            });
            UpdateExamine();
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
            int delta = 1;    //Distance moved by camera
            if(info.IsKeyDown(Keys.RightControl)) {
                delta *= 8;
            }
            /*
            if (info.IsKeyDown(Keys.RightControl)) {
                examineMenu.ListControls(info);
            } else 
            */
            if (info.IsKeyPressed(Keys.Up)) {
                if (info.IsKeyDown(Keys.RightShift)) {
                    world.camera += new XYZ(0, 0, delta);
                } else {
                    world.camera += new XYZ(0, -delta);
                }
                UpdateExamine();
            } else if (info.IsKeyPressed(Keys.Down)) {
                if (info.IsKeyDown(Keys.RightShift)) {
                    world.camera += new XYZ(0, 0, -delta);
                } else {
                    world.camera += new XYZ(0, delta);
                }
                UpdateExamine();
            } else if (info.IsKeyPressed(Keys.Left)) {
                world.camera += new XYZ(-delta, 0);
                UpdateExamine();
            } else if (info.IsKeyPressed(Keys.Right)) {
                world.camera += new XYZ(delta, 0);
                UpdateExamine();
            } else if (info.IsKeyPressed(Keys.Escape)) {
                world.camera = world.player.Position;
                Hide();
            } else if (info.IsKeyPressed(Keys.Enter)) {
                selectAt(world.camera);
            } else {
                examineMenu.ListControls(info);
            }
            return true;
        }
        public override void Draw(TimeSpan delta) {
            Clear();
            if (cursorVisible) {
                this.DebugInfo($"Draw Cursor @ ({Width / 2}, {Height / 2})");
                Print(Width / 2, Height / 2, cursor);
            }
            base.Draw(delta);
            examineMenu?.Draw(delta);
        }
        public override void Update(TimeSpan delta) {
            examineMenu?.Update(delta);
            cursorBlink.Update(delta.TotalSeconds);
            base.Update(delta);
        }
        public override void Hide() {
            base.Hide();
            examineMenu?.Hide();
        }
        public void UpdateExamine() {

            examineMenu?.Hide();
            var ent = world.entities[world.camera];
            if (ent != null) {
                examineMenu = new ListMenu<Entity>(Width, Height, hint, ent.Select(e => new ListEntity(e)), select) {
                    IsVisible = true
                };
            }
        }
    }
}
