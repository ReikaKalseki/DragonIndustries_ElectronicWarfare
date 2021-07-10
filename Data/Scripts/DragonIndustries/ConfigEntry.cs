using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;

using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities;

using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
		public class ConfigEntry {
		
        private static readonly Dictionary<string, Settings> settingsByName = new Dictionary<string, Settings>();
        private static readonly Dictionary<string, Settings> settingsByDesc = new Dictionary<string, Settings>();
	
		[XmlIgnore]
		public string ID;
		public string Description;
		public string Type;
		public string ValueAsString;
		
		[XmlIgnore]
		private object value;
		
		public ConfigEntry() { //deserial
			
		}
		
		public ConfigEntry(Settings s, string n, object val) {
			Description = n;
			value = val;
			ValueAsString = Convert.ToString(val);
			Type = "None";
			if (val is int)
				Type = "Int";
			else if (val is bool)
				Type = "Boolean";
			else if (val is float)
				Type = "Float";
			else if (val is string)
				Type = "String";
			ID = s.ToString();
			
			settingsByName[ID] = s;
			settingsByDesc[Description] = s;
			
			IO.log("Cached "+s+" as ID "+ID+" and desc "+Description);
		}
		
		public void loadValue() {
			value = parseType();
			Settings s;
			if (settingsByDesc.TryGetValue(Description, out s))
				ID = s.ToString();
		}
		
		private object parseType() {
			switch(Type) {
				case "Int":
					return Convert.ToInt32(ValueAsString);
				case "Float":
					return Convert.ToSingle(ValueAsString);
				case "Boolean":
					return Convert.ToBoolean(ValueAsString);
				case "String":
					return ValueAsString;
			}
			return null;
		}
		
		public int asInt() {
			return (int)value;
		}
		
		public float asFloat() {
			return (float)value;
		}
		
		public bool asBoolean() {
			return (bool)value;
		}
		
		public string asString() {
			return (string)value;
		}
		
		public bool hasValue() {
			return value != null;
		}
		
		public override string ToString() {
			return "Configuration "+ID+" with value "+value+" of type "+Type;
		}
	}
	
	public enum Settings {
		WARHEAD,
		SELFDAMAGE,
		HACKDAMAGE,
		HACKSCALE,
		HACKSPEED,
		COMPUTERCONVERT,
		ALLOWHACKSKIP,
		ALLOWHACKSTATION,
		HACKRETRY,
		CLOAKPOWERSMALL,
		CLOAKPOWERLARGE,
		CLOAKWEAPONPOWER,
		CLOAKRENDERPOWER,
		RADARPOWERSMALL,
		RADARPOWERLARGE,
	};
	
}