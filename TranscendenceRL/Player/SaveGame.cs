﻿using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TranscendenceRL {

    //https://stackoverflow.com/a/18548894
    class WritablePropertiesOnlyResolver : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
            IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
            return props.Where(p => p.Writable).ToList();
        }
    }

    interface SaveGame {
        public static void PrepareConvert() {
        }
        public static string Serialize(object o) {
            PrepareConvert();
            return JsonConvert.SerializeObject(o, settings);
        }
        public static T Deserialize<T>(string s) {
            PrepareConvert();
            return JsonConvert.DeserializeObject<T>(s, settings);
        }
        public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            ContractResolver = new WritablePropertiesOnlyResolver()
        };
    }
    class LiveGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
    }
    class DeadGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
        public Epitaph epitaph;
    }
}
