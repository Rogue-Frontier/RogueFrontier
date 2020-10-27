﻿using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using SadRogue.Primitives;
using Console = SadConsole.Console;
using ArchConsole;

namespace ASECII {
    class LayerMenu : Console {
        SpriteModel model;
        public LayerMenu(int width, int height, SpriteModel model) : base(width, height) {
            this.model = model;
        }
        public void UpdateListing() {
            this.Children.Clear();
            int i = 0;
            var layers = model.sprite.layers;
            foreach (var l in layers) {
                int index = i;

                this.Children.Add(new ColorCellButton(() => l.visible ? Color.White : Color.Black,
                    () => {
                        l.visible = !l.visible;
                    }, '*') {
                    Position = new Point(0, index)
                });

                this.Children.Add(new CellButton(() => index < layers.Count - 1,
                    () => {
                        layers.RemoveAt(index);
                        layers.Insert(index + 1, l);

                        if(model.currentLayer == index) {
                            model.currentLayer++;
                        }

                        UpdateListing();
                    }, '-') {
                    Position = new Point(1, index)
                });

                this.Children.Add(new CellButton(() => index > 0,
                        () => {
                            if (model.currentLayer == index) {
                                model.currentLayer--;
                            }

                            layers.RemoveAt(index);
                            layers.Insert(index - 1, l);
                            UpdateListing();
                        }, '+') {
                    Position = new Point(2, index)
                });

                ColorButton nameButton = null;

                string GetLabel() => $">{(l.name.Length > 8 ? l.name.Remove(8) : l.name)}";

                var layerSettings = new LayerSettings(l, () => nameButton.text = GetLabel()) { Position = new Point(16, index) };

                this.Children.Add(new ColorCellButton(() => !this.Children.Contains(layerSettings) ? Color.White : Color.Black,
                    () => {
                        //Show the LayerSettings console
                        if (this.Children.Contains(layerSettings)) {
                            this.Children.Remove(layerSettings);
                        } else {
                            this.Children.Add(layerSettings);
                        }
                    }, '?') {
                    Position = new Point(3, index)
                });

                nameButton = new ColorButton(GetLabel(),
                    () => model.currentLayer == index ? Color.Yellow : Color.White,
                    () => model.currentLayer = index) {
                    Position = new Point(4, index)
                };
                this.Children.Add(nameButton);

                this.Children.Add(new CellButton(() => index > 0,
                    () => {
                        var below = layers[index - 1];
                        below.Flatten(l);
                        layers.RemoveAt(index);

                        if (model.currentLayer == index) {
                            model.currentLayer--;
                        }

                        UpdateListing();
                    }, '%') {
                    Position = new Point(13, index)
                });

                this.Children.Add(new CellButton(() => model.sprite.layers.Count > 1,
                        () => {
                            layers.RemoveAt(index);

                            if (model.currentLayer == index && index > 0) {
                                model.currentLayer--;
                            }

                            UpdateListing();
                        }, 'X') {
                    Position = new Point(15, index)
                });
                i++;
            }
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            return base.ProcessMouse(state);
        }
    }

    class LayerSettings : Console {
        public LayerSettings(Layer layer, Action nameChanged = null) : base(16, 1) {
            int x = 0;
            var nameField = new TextField(12) { text = layer.name };
            nameField.TextChanged += t => UpdateName();
            nameField.EnterPressed += t => UpdateName();

            void UpdateName() {
                layer.name = nameField.text;
                nameChanged?.Invoke();
            }
            x += 12;
            var glyphButton = new ColorCellButton(() => layer.applyGlyph ? Color.White : Color.Black,
                () => {
                    layer.applyGlyph = !layer.applyGlyph;
                }, 'G') {
                Position = new Point(x, 0)
            };
            x++;

            var foregroundButton = new ColorCellButton(() => layer.applyForeground ? Color.White : Color.Black,
                () => {
                    layer.applyForeground = !layer.applyForeground;
                }, 'F') {
                Position = new Point(x, 0)
            };
            x++;

            var backgroundButton = new ColorCellButton(() => layer.applyBackground ? Color.White : Color.Black,
                () => {
                    layer.applyBackground = !layer.applyBackground;
                }, 'B') {
                Position = new Point(x, 0)
            };
            x++;

            Action<Console> add = this.Children.Add;
            add(nameField);
            add(glyphButton);
            add(foregroundButton);
            add(backgroundButton);
        }
    }
}