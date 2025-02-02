using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Utilities;
using LibGamer;
using TileTuple = (uint Foreground, uint Background, int Glyph);
namespace LibGamer;
//https://stackoverflow.com/a/57319194
public static class STypeConverter {
	public static void PrepareConvert () {
		//https://stackoverflow.com/a/57319194
		TypeDescriptor.AddAttributes(typeof((int, int)), new TypeConverterAttribute(typeof(Int2Converter)));
		TypeDescriptor.AddAttributes(typeof((uint, uint)), new TypeConverterAttribute(typeof(UInt2Converter)));
		TypeDescriptor.AddAttributes(typeof(TileTuple), new TypeConverterAttribute(typeof(TileTupleConverter)));
		//TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorConverter)));
	}
}
public static class ImageLoader {
	public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
		PreserveReferencesHandling = PreserveReferencesHandling.All,
		TypeNameHandling = TypeNameHandling.All,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
	};
	public static Dictionary<(int X, int Y), TileTuple> LoadTile (string path) {
		Console.WriteLine($"Reading {path}");
		return ReadTile(File.ReadAllText(path));
	}
	public static Dictionary<(int X, int Y), TileTuple> ReadTile (string data) {
		var f = (string t) => {
			var p = (string s) => ABGR.FromRGBA(uint.Parse(s, NumberStyles.HexNumber));
			var parts = t.Split(" ");
			return (
				(int.Parse(parts[0]), int.Parse(parts[1])),
				(p(parts[2]), p(parts[3]), int.Parse(parts[4]))
				);
		};
		return (from t in DeserializeObject<HashSet<string>>(data) select f(t)).ToDictionary();
	}

	public static Dictionary<(int X, int Y), Tile> Adjust(this Dictionary<(int X, int Y), Tile> img) {
		var xMin = img.Min(pair => pair.Key.X);
		var yMin = img.Min(pair => pair.Key.Y);
		return img.Select(pair => ((pair.Key.X - xMin, pair.Key.Y - yMin), pair.Value)).ToDictionary();
	}
	public static T DeserializeObject<T> (string s) {
		
		STypeConverter.PrepareConvert();
		return JsonConvert.DeserializeObject<T>(s, settings);
	}
	public static string SerializeObject (object o) {
		STypeConverter.PrepareConvert();
		return JsonConvert.SerializeObject(o, settings);
	}
}
public class Int2Converter : TypeConverter {
	public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
		return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	}
	public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
		var elements = Convert.ToString(value).Trim('(').Trim(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		return (int.Parse(elements.First()), int.Parse(elements.Last()));
	}
}
public class UInt2Converter : TypeConverter {
	public override bool CanConvertTo (ITypeDescriptorContext context, [NotNullWhen(true)] Type destinationType) {
		return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
	}
	public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
		var v = ((uint Front, uint Back, int Glyph))value;
		return $"({v.Front}, {v.Back}, {v.Glyph})";
	}
	public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
		return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	}
	public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
		var elements = Convert.ToString(value).Trim('(').Trim(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		return (uint.Parse(elements.First()), uint.Parse(elements.Last()));
	}
}
public class TileTupleConverter : TypeConverter {
	public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
		return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	}
	public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
		var elements = Convert.ToString(value).Trim('(', ')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
		return (uint.Parse(elements[0]), uint.Parse(elements[1]), int.Parse(elements[2]));
	}
}
