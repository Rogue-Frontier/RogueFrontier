﻿using Common;
using SadRogue.Primitives;
using SadConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace RogueFrontier;

public class TypeCollection {
    private Dictionary<string, XElement> sources;
    public Dictionary<string, DesignType> all;
    private Dictionary<Type, object> dicts;

    enum InitState {
        InitializePending,
        Initializing,
        Initialized
    }
    InitState state;

    //After our first initialization, any types we create later must be initialized immediately. Any dependency types must already be bound
    public TypeCollection() {
        sources = new Dictionary<string, XElement>();
        all = new Dictionary<string, DesignType>();
        dicts = new() {
            [typeof(GenomeType)] = new Dictionary<string, GenomeType>(),
            [typeof(ImageType)] = new Dictionary<string, ImageType>(),
            [typeof(ItemType)] = new Dictionary<string, ItemType>(),
            [typeof(PowerType)] = new Dictionary<string, PowerType>(),
            [typeof(SceneType)] = new Dictionary<string, SceneType>(),
            [typeof(ShipClass)] = new Dictionary<string, ShipClass>(),
            [typeof(Sovereign)] = new Dictionary<string, Sovereign>(),
            [typeof(StationType)] = new Dictionary<string, StationType>(),
            [typeof(SystemType)] = new Dictionary<string, SystemType>(),
        };


        state = InitState.InitializePending;

        Debug.Print("TypeCollection created");

    }
    public TypeCollection(params string[] modules) : this() {
        LoadFile(modules);
    }
    public TypeCollection(params XElement[] modules) : this() {

        //We do two passes
        //The first pass creates DesignType references for each type and stores the source code
        foreach (var m in modules) {
            ProcessRoot("", m);
        }

        //The second pass initializes each type from the source code
        Initialize();
    }
    void Initialize() {
        state = InitState.Initializing;
        //We don't evaluate all sources; just the ones that are used by DesignTypes
        foreach (string key in all.Keys.ToList()) {
            DesignType type = all[key];
            XElement source = sources[key];
            type.Initialize(this, source);
        }
        state = InitState.Initialized;
    }
    public void LoadFile(params string[] modules) {
        foreach (var m in modules) {
            ProcessRoot(m, XElement.Parse(File.ReadAllText(m)));
        }
        if (state == InitState.InitializePending) {
            //We do two passes
            //The first pass creates DesignType references for each type and stores the source code

            //The second pass initializes each type from the source code
            Initialize();
        }
    }

    void ProcessRoot(string file, XElement root) {
        foreach (var element in root.Elements()) {
            ProcessElement(file, element);
        }
    }
    public void ProcessElement(string file, XElement element) {


        void ProcessSection(XElement e) => ProcessRoot(file, e);
        Action<XElement> a = element.Name.LocalName switch {
            "Module" => e => {
                var subfile = Path.Combine(Directory.GetParent(file).FullName, e.ExpectAtt("file"));
                XElement module = XDocument.Load(subfile).Root;
                ProcessRoot(file, module);
            }
            ,
            "Source" => AddSource,
            "Content" => ProcessSection,
            "Unused" => e => { },
            "Debug" => ProcessSection,
            "GenomeType" => AddType<GenomeType>,
            "ImageType" => AddType<ImageType>,
            "ItemType" => AddType<ItemType>,
            "PowerType" => AddType<PowerType>,
            "SceneType" => AddType<SceneType>,
            "ShipClass" => AddType<ShipClass>,
            "StationType" => AddType<StationType>,
            "Sovereign" => AddType<Sovereign>,
            "SystemType" => AddType<SystemType>,
            _ => throw new Exception($"Unknown element <{element.Name}>")

        };
        a(element);
    }
    void AddSource(XElement element) {
        if (!element.TryAtt("codename", out string type)) {
            throw new Exception("DesignType requires codename attribute");
        } else if (sources.ContainsKey(type)) {
            throw new Exception($"DesignType type conflict: {type}");
        }
        Debug.Print($"Created Source <{element.Name}> of type {type}");
        sources[type] = element;
    }

    public Dictionary<string, T> GetDict<T>() where T: DesignType =>
        (Dictionary<string, T>) dicts[typeof(T)];
    public Dictionary<string, T>.ValueCollection Get<T>() where T : DesignType =>
        GetDict<T>().Values;
    void AddType<T>(XElement element) where T : DesignType, new() {
        if (!element.TryAtt("codename", out string type)) {
            throw new Exception("DesignType requires codename attribute");
        } else if (sources.ContainsKey(type)) {
            throw new Exception($"DesignType type conflict: {type}");
        }
        Debug.Print($"Created <{element.Name}> of type {type}");
        sources[type] = element;
        T t = new();
        all[type] = t;
        ((Dictionary<string, T>)dicts[typeof(T)])[type] = t;
        Init:
        //If we're uninitialized, then we will definitely initialize later
        if (state != InitState.InitializePending) {
            //Otherwise, initialize now
            all[type].Initialize(this, sources[type]);
        }
    }
    public bool Lookup(string codename, out DesignType result) =>
        all.TryGetValue(codename, out result);

    public bool Lookup<T>(string type, out T result) where T : class, DesignType =>
        (result = Lookup<T>(type)) != null;
    public DesignType Lookup(string codename) {
        if (codename == null || codename.Trim().Length == 0) {
            throw new Exception($"Must specify a codename");
        }
        if(all.TryGetValue(codename, out var result)) {
            return result;
        }
        throw new Exception($"Unknown type {codename}");
    }
    public T Lookup<T>(string codename) where T : class, DesignType {
        var result = Lookup(codename);
        return result as T ??
            throw new Exception($"Type {codename} is <{result.GetType().Name}>, not <{nameof(T)}>");
    }
}
public interface DesignType {
    void Initialize(TypeCollection collection, XElement e);
}
public interface ITile {
    public ColoredGlyph Original { get; }
    void Update() { }
}
public record StaticTile() : ITile {
    [JsonProperty]
    public ColoredGlyph Original { get; set; }
    [JsonIgnore]
    public Color foreground => Original.Foreground;
    [JsonIgnore]
    public Color Background => Original.Background;
    [JsonIgnore]
    public int GlyphCharacter => Original.GlyphCharacter;
    public StaticTile(XElement e) : this() {
        char c = e.ExpectAttChar("glyph");
        Color foreground = e.TryAttColor("foreground", Color.White);
        Color background = e.TryAttColor("background", Color.Transparent);

        Original = new ColoredGlyph(foreground, background, c);
    }
    public StaticTile(ColoredGlyph Glyph) : this() => this.Original = Glyph;
    public StaticTile(char c) : this() {
        Original = new ColoredGlyph(Color.White, Color.Black, c);
    }
    public StaticTile(char c, string foreground, string background) : this() {
        var fore = (Color)typeof(Color).GetField(foreground).GetValue(null);
        var back = (Color)typeof(Color).GetField(background).GetValue(null);
        Original = new ColoredGlyph(fore, back, c);
    }
    public static implicit operator ColoredGlyph(StaticTile t) => t.Original;
    public static implicit operator StaticTile(ColoredGlyph cg) => new StaticTile(cg);
}

public record AlphaTile() : ITile {
    [JsonProperty]
    public ColoredGlyph Original { get; set; }
    [JsonIgnore]
    public ColoredGlyph Glyph => new ColoredGlyph(
        Original.Foreground.SetAlpha((byte)(
            Original.Foreground.A + alphaRange * Math.Sin(ticks * 2 * Math.PI / cycle))),
        Original.Background.SetAlpha((byte)(
            Original.Background.A + alphaRange * Math.Sin(ticks * 2 * Math.PI / cycle))),
        Original.Glyph);
    int cycle;
    int alphaRange;

    int ticks = 0;
    public AlphaTile(ColoredGlyph Glyph) : this() => Original = Glyph;
    public void Update() {
        ticks++;
    }
    public static implicit operator ColoredGlyph(AlphaTile t) => t.Original;
}
