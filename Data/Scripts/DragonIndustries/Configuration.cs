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

using TextWriter = System.IO.TextWriter;
using TextReader = System.IO.TextReader;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyWarhead = Sandbox.ModAPI.IMyWarhead;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
	public static class Configuration {
		
        public static readonly Dictionary<string, ConfigEntry> settings = new Dictionary<string, ConfigEntry>(); 
		
        public static readonly Dictionary<string, EMPReaction> reactionMap = new Dictionary<string, EMPReaction>();
        public static readonly Dictionary<string, HackingDifficulty> hackMap = new Dictionary<string, HackingDifficulty>();
        
		private static readonly EMPReaction defaultReaction = new EMPReaction("default", 0, 20); //generic fallback for ones without hardcoded resistances or reactions
		private static readonly HackingDifficulty defaultDifficulty = new HackingDifficulty("default", 1, 1); //generic fallback for ones without hardcoded resistances or reactions
		
		public static void load() {
			IO.loadSavedData();
			
            addDefaultSettings();
            addDefaultReactions();
            addDefaultHackingDifficulties();
            
            IO.writeConfigs();
            
            if (getSetting(Settings.WARHEAD).asBoolean())
            	getEMPReaction("Warhead").addEffect(block => (block as IMyWarhead).Detonate(), 20);
		}
		
		public static void unload() {
			IO.savePersistentData();
		}

        private static void addDefaultSettings() {
            List<ConfigEntry> defaultSettings = new List<ConfigEntry>();
            
            defaultSettings.Add(new ConfigEntry(Settings.WARHEAD, "EMP Can Detonate Warheads", true));
            defaultSettings.Add(new ConfigEntry(Settings.SELFDAMAGE, "EMP Damages Firing Ship If Powered", true));
            defaultSettings.Add(new ConfigEntry(Settings.HACKDAMAGE, "Allow Hacking Computer to Receive Retaliatory Damage", true));
            defaultSettings.Add(new ConfigEntry(Settings.HACKSPEED, "Hacking Computer Speed (Cycle Time In Sixths of A Second)", 3));
            defaultSettings.Add(new ConfigEntry(Settings.COMPUTERCONVERT, "Hacking Computers Convert Each Other", true));
            defaultSettings.Add(new ConfigEntry(Settings.ALLOWHACKSKIP, "Allow Hacking Computers To Ignore User Specified Blocks", true));
            defaultSettings.Add(new ConfigEntry(Settings.HACKRETRY, "Hacking Computers Lock To Target Until Success", false));
            defaultSettings.Add(new ConfigEntry(Settings.CLOAKPOWERSMALL, "Cloaking Device Base Power Consumption (MW) Per Tonne - Small Grid", 0.5F));
            defaultSettings.Add(new ConfigEntry(Settings.CLOAKPOWERLARGE, "Cloaking Device Base Power Consumption (MW) Per Tonne - Large Grid", 0.016F));
            defaultSettings.Add(new ConfigEntry(Settings.CLOAKWEAPONPOWER, "Cloaking Device Power Consumption Factor During Weapon Usage", 2.4F));
            defaultSettings.Add(new ConfigEntry(Settings.CLOAKRENDERPOWER, "Cloaking Device Power Consumption Factor During Invisibility", 1.6F));
            defaultSettings.Add(new ConfigEntry(Settings.RADARPOWERSMALL, "Radar Scanner Base Power Consumption (MW) Per km2 Range - Small Grid", 0.4F));
            defaultSettings.Add(new ConfigEntry(Settings.RADARPOWERLARGE, "Radar Scanner Base Power Consumption (MW) Per km2 Range - Large Grid", 0.15F)); 
            			
            foreach (ConfigEntry val in defaultSettings) {
                if (!settings.ContainsKey(val.ID))
                    settings.Add(val.ID, val);
			}
        }
		
		private static void addDefaultHackingDifficulties() {
            List<HackingDifficulty> defaultDifficulties = new List<HackingDifficulty>();
            
			defaultDifficulties.Add(new HackingDifficulty("HydrogenTank", 1, 0.25F));
			defaultDifficulties.Add(new HackingDifficulty("OxygenTank", 1, 0.25F));
			
			defaultDifficulties.Add(new HackingDifficulty("MissileTurret", 5, 4F));
			defaultDifficulties.Add(new HackingDifficulty("GatlingTurret", 4, 3F));
			defaultDifficulties.Add(new HackingDifficulty("InteriorTurret", 3, 3F));
			
			defaultDifficulties.Add(new HackingDifficulty("Cockpit", 3, 0.1F)); //a hundred computers but nonetheless trivial to hack, since it is useless without you next to it
			defaultDifficulties.Add(new HackingDifficulty("Control Station", 3, 0.1F));
			defaultDifficulties.Add(new HackingDifficulty("Flight Seat", 3, 0.1F));
			defaultDifficulties.Add(new HackingDifficulty("RemoteControl", 5, 10F, 0.01F)); //but this is quite the opposite (glares at Atmospheric Encounters)
			
			defaultDifficulties.Add(new HackingDifficulty("Battery", 5, 3F)); //to avoid trivial salvage captures by way of hacking the power
			defaultDifficulties.Add(new HackingDifficulty("Generator", 5, 2.5F));
			defaultDifficulties.Add(new HackingDifficulty("SolarPanel", 5, 6F));
			
			defaultDifficulties.Add(new HackingDifficulty("JumpDrive", 2, 0.5F));
			
			defaultDifficulties.Add(new HackingDifficulty("Hacking", 10, 1F, 0.15F));
            
           	foreach (HackingDifficulty val in defaultDifficulties) {
                if (!hackMap.ContainsKey(val.BlockType))
                    hackMap.Add(val.BlockType, val);
			}
		}
		
		private static void addDefaultReactions() {
            List<EMPReaction> defaultReactions = new List<EMPReaction>();
            
			defaultReactions.Add(new EMPReaction("RadioAntenna",	0, 		75,		40, 	0.8F		));
            defaultReactions.Add(new EMPReaction("LaserAntenna",	60, 			25					));
            
            defaultReactions.Add(new EMPReaction("Battery",			100,	95, 	2,		10F,	30	));
            defaultReactions.Add(new EMPReaction("SmallGenerator",	75, 	40,		2,		24F,	30	));
            defaultReactions.Add(new EMPReaction("LargeGenerator",	90,		60,		3.75,	24F,	30	));
            
            defaultReactions.Add(new EMPReaction("Cockpit",			80,		50,		10,		6			));
            defaultReactions.Add(new EMPReaction("Control Station",	80,		50,		10,		6			));
            defaultReactions.Add(new EMPReaction("Flight Seat",		80,		50,		10,		6			));
            
            defaultReactions.Add(new EMPReaction("Gyro",			100,	25,		15,		8F			));
            defaultReactions.Add(new EMPReaction("RemoteControl",	90,		40,		20,		-1F			));
            
            defaultReactions.Add(new EMPReaction("GatlingTurret",	50,		25,		15,		1.5F,	60	));
            defaultReactions.Add(new EMPReaction("InteriorTurret",	90,		80,		5,		4F,		90	));
            defaultReactions.Add(new EMPReaction("MissileTurret",	50,		20,		15,		1.5F,	90	));
            defaultReactions.Add(new EMPReaction("MissileLauncher",	75,		60,		10,		2F			));
            
            defaultReactions.Add(new EMPReaction("Assembler",		10,		0,		40,		10F			));
            defaultReactions.Add(new EMPReaction("Refinery",		10,		0,		40,		10F			));
            defaultReactions.Add(new EMPReaction("BlastFurnace",	10,		0,		40,		10F			));
            
            defaultReactions.Add(new EMPReaction("TextPanel",		100,	100,	0					));
            defaultReactions.Add(new EMPReaction("LCDPanel",		100,	100,	0					));
            
            defaultReactions.Add(new EMPReaction("SmallContainer",	100,	100,	2.5					));
            defaultReactions.Add(new EMPReaction("LargeContainer",	100,	100,	2.5					));
            defaultReactions.Add(new EMPReaction("HydrogenTank",	100,	100,	2.5					));
            defaultReactions.Add(new EMPReaction("OxygenTank",		100,	100,	2.5					));
            
            defaultReactions.Add(new EMPReaction("ConveyorSorter",	20,		0,		30,		-1F			));
            defaultReactions.Add(new EMPReaction("GravityGenerator",25,		5,		10,		-1F			));
            
            defaultReactions.Add(new EMPReaction("JumpDrive",		90,		10,		5,		-1F,	120	));
            
            defaultReactions.Add(new EMPReaction("Warhead",			25,		0,		30,		-1F			));
            
           	foreach (EMPReaction val in defaultReactions) {
                if (!reactionMap.ContainsKey(val.BlockType))
                    reactionMap.Add(val.BlockType, val);
			}
		}
        
        public static ConfigEntry getSetting(Settings s) {
			if (settings.Count == 0) {
				load();
			}
        	return settings[s.ToString()];
        }

        public static HackingDifficulty getHackingDifficulty(IMyTerminalBlock block) {
			return getHackingDifficulty(block.BlockDefinition.ToString());
        }

        public static HackingDifficulty getHackingDifficulty(string def) {
			HackingDifficulty get = null;
			hackMap.TryGetValue(def, out get);
			if (get != null)
				return get;
            foreach (var entry in hackMap) {
				HackingDifficulty reaction = entry.Value;
                if (def.Contains(reaction.BlockType)) {
					hackMap.Add(def, reaction);
					return reaction;
				}
			}
			hackMap.Add(def, defaultDifficulty);
            return defaultDifficulty;  
        }

        public static EMPReaction getEMPReaction(IMyTerminalBlock block) {
			return getEMPReaction(block.BlockDefinition.ToString());
        }

        public static EMPReaction getEMPReaction(string def) {
			EMPReaction get = null;
			reactionMap.TryGetValue(def, out get);
			if (get != null)
				return get;
            foreach (var entry in reactionMap) {
				EMPReaction reaction = entry.Value;
                if (def.Contains(reaction.BlockType)) {
					reactionMap.Add(def, reaction);
					return reaction;
				}
			}
			reactionMap.Add(def, defaultReaction);
            return defaultReaction;            
        }
	}
}