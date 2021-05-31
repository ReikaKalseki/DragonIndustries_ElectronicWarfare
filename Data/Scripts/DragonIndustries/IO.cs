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

using System.IO;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

using MyLog = VRage.Utils.MyLog;

namespace DragonIndustries {
	
	public static class IO {
		
        private static List<EMPReaction> reactionFileData = new List<EMPReaction>();
        private static List<HackingDifficulty> hackFileData = new List<HackingDifficulty>();
		
		public static void log(string s) {
        	MyLog.Default.WriteLineAndConsole("  DragonIndustries Electronic Warfare: "+s);
		}
			
		private static void loadConfigs() {
        	log("Loading configs.");
	        //Configuration.settings.Clear();
			try {
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config.cfg", typeof(ConfigEntry))) {
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("config.cfg", typeof(ConfigEntry));
					string xmlText = reader.ReadToEnd();
					reader.Close();
                    List<ConfigEntry> li = MyAPIGateway.Utilities.SerializeFromXML<List<ConfigEntry>>(xmlText);
                    if (li != null) {
	                    foreach (ConfigEntry entry in li) {
	                    	entry.loadValue();
	                    	if (entry.ID == null) {
	                    		log("Could not parse config entry "+entry.Description+"; no ID!");
	                    		continue;
	                    	}
	                    	if (!entry.hasValue()) {
	                    		log("Could not parse config entry "+entry.Description+"!");
	                    		continue;
	                    	}
	                    	log("Loaded config entry "+entry.ID+" / "+entry.Description+" with "+entry.ValueAsString);
	                    	Configuration.settings[entry.ID] = entry;
	                    }
                    }
                }
        		IO.log("Loaded general config. Data = "+toUsefulString(Configuration.settings));
			}
			catch (Exception ex) {
				log("Threw exception reading general config: "+ex.ToString());
			}
			
			try {
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config_reactions.cfg", typeof(EMPReaction))) {
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("config_reactions.cfg", typeof(EMPReaction));
					string xmlText = reader.ReadToEnd();
					reader.Close();
                    reactionFileData = MyAPIGateway.Utilities.SerializeFromXML<List<EMPReaction>>(xmlText);
                    foreach (EMPReaction entry in reactionFileData) {
                       Configuration.reactionMap[entry.BlockType] = entry;
                    }
                }
	        	IO.log("Loaded reaction config. Data = "+toUsefulString(Configuration.reactionMap));
			}
			catch (Exception ex) {
				log("Threw exception reading reaction config: "+ex.ToString());
			}
			
			try {
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config_hack.cfg", typeof(HackingDifficulty))) {
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("config_hack.cfg", typeof(HackingDifficulty));
					string xmlText = reader.ReadToEnd();
					reader.Close();
                    hackFileData = MyAPIGateway.Utilities.SerializeFromXML<List<HackingDifficulty>>(xmlText);
                    foreach (HackingDifficulty entry in hackFileData) {
                       Configuration.hackMap[entry.BlockType] = entry;
                    }
                }
	        	IO.log("Loaded hack config. Data = "+toUsefulString(Configuration.hackMap));
			}
			catch (Exception ex) {
				log("Threw exception reading reaction config: "+ex.ToString());
			}
		}

		public static void loadSavedData() {
			loadConfigs();
			
			string world = MyAPIGateway.Session.Name;
			try {
				EMP.blockReactivations.Clear();
				
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("blocks_to_reactivate_"+world+".cfg", typeof(SavedTimedBlock))) {
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("blocks_to_reactivate_"+world+".cfg", typeof(SavedTimedBlock));
					string xmlText = reader.ReadToEnd();
					reader.Close();
                    List<SavedTimedBlock> li = MyAPIGateway.Utilities.SerializeFromXML<List<SavedTimedBlock>>(xmlText);
                    if (li != null) {
                    	EMP.blockReactivations.AddList(li);
                    }
                }
        		IO.log("Loaded reactivation list. Data = "+toUsefulString(EMP.blockReactivations));
			}
			catch (Exception ex) {
				log("Threw exception reading reactivation list: "+ex.ToString());
			}
			
			try {
				List<long> li = new List<long>();
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("cloaked_grids_"+world+".cfg", typeof(long))) {
					TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("cloaked_grids_"+world+".cfg", typeof(long));
					string xmlText = reader.ReadToEnd();
					reader.Close();
					li = MyAPIGateway.Utilities.SerializeFromXML<List<long>>(xmlText);
					CloakingDevice.loadCloakedGridsFromFile(li);
                }
				IO.log("Loaded cloaking list. Data = "+toUsefulString(li));
			}
			catch (Exception ex) {
				log("Threw exception reading cloaking list: "+ex.ToString());
			}
        }

		public static void savePersistentData() {
            try {				
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("blocks_to_reactivate.dat", typeof(ConfigEntry)))
        			MyAPIGateway.Utilities.DeleteFileInWorldStorage("blocks_to_reactivate.dat", typeof(ConfigEntry));
			
				TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("blocks_to_reactivate.dat", typeof(SavedTimedBlock));
				try {
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(EMP.blockReactivations));
				}
				catch (Exception ex) {
					writer.Write("Error while writing reactivation cache for world "+MyAPIGateway.Session.Name+".");
					//writer.Write("\n\nData: "+EMP.blockReactivations.ToString());
					writer.Write("\n\nException: "+ex.ToString());
				}
				writer.Flush();
				writer.Close();
				
				IO.log("Saved reactivation list. Data = "+toUsefulString(EMP.blockReactivations));
				
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage("cloaked_grids.dat", typeof(long)))
        			MyAPIGateway.Utilities.DeleteFileInWorldStorage("cloaked_grids.dat", typeof(long));
			
				writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("cloaked_grids.dat", typeof(long));
				try {
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(CloakingDevice.getCloakingList()));
				}
				catch (Exception ex) {
					writer.Write("Error while writing cloaking cache for world "+MyAPIGateway.Session.Name+".");
					writer.Write("\n\nException: "+ex.ToString());
				}
				writer.Flush();
				writer.Close();
				
				IO.log("Saved cloaking list.");
			}
			catch (Exception ex2) {
				log("Threw exception writing cloaking list: "+ex2.ToString());
			}
        }

		public static void writeConfigs() {
        	List<ConfigEntry> li = new List<ConfigEntry>();
        	
        	log("Writing general config. Data = "+toUsefulString(Configuration.settings));
        	
        	if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config.cfg", typeof(ConfigEntry)))
        		MyAPIGateway.Utilities.DeleteFileInWorldStorage("config.cfg", typeof(ConfigEntry));
			
			TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("config.cfg", typeof(ConfigEntry));
			foreach (var entry in Configuration.settings) {
				li.Add(entry.Value);
			}
			
            try {
				try {
					//writer.Write("Serializing a list of size "+Configuration.settings.Count+";");
					//IO.log("Serializing a list of size "+Configuration.settings.Count+";");
					//writer.Write("Serializes to:");
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(li));
				}
				catch (Exception ex) {
					writer.Write("Error while writing config.");
					writer.Write("\n\nSize: "+Configuration.settings.Count);
					writer.Write("\n\nException: "+ex);
				}
				writer.Flush();
				writer.Close();
			}
			catch (Exception ex2) {
				log("Threw exception writing general config: "+ex2.ToString());
			}
			
			reactionFileData = new List<EMPReaction>();
			if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config_reactions.cfg", typeof(EMPReaction)))
        		MyAPIGateway.Utilities.DeleteFileInWorldStorage("config_reactions.cfg", typeof(EMPReaction));
        		
			writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("config_reactions.cfg", typeof(EMPReaction));
			foreach (var entry in Configuration.reactionMap) {
				reactionFileData.Add(entry.Value);
			}
			
			hackFileData = new List<HackingDifficulty>();
			if (MyAPIGateway.Utilities.FileExistsInWorldStorage("config_hack.cfg", typeof(HackingDifficulty)))
        		MyAPIGateway.Utilities.DeleteFileInWorldStorage("config_hack.cfg", typeof(HackingDifficulty));
        		
			writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("config_hack.cfg", typeof(HackingDifficulty));
			foreach (var entry in Configuration.hackMap) {
				hackFileData.Add(entry.Value);
			}
			
            try {
				try {
					//writer.Write("Serializing a list of size "+fileData.Count+"; sample entry: "+fileData[0]+" for "+fileData[0].BlockType+" with "+fileData[0].Resistance+" @ "+fileData[0].MaxDistance);
					//writer.Write("Serializes to:");
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(reactionFileData));
				}
				catch (Exception ex) {
					writer.Write("Error while writing reaction config.");
					writer.Write("\n\nData: "+reactionFileData);
					writer.Write("\n\nException: "+ex);
				}
				writer.Flush();
				writer.Close();
			}
			catch (Exception ex2) {
				log("Threw exception writing reaction config: "+ex2.ToString());
			}
			
            try {
				try {
					//writer.Write("Serializing a list of size "+fileData.Count+"; sample entry: "+fileData[0]+" for "+fileData[0].BlockType+" with "+fileData[0].Resistance+" @ "+fileData[0].MaxDistance);
					//writer.Write("Serializes to:");
					writer.Write(MyAPIGateway.Utilities.SerializeToXML(hackFileData));
				}
				catch (Exception ex) {
					writer.Write("Error while writing hack config.");
					writer.Write("\n\nData: "+hackFileData);
					writer.Write("\n\nException: "+ex);
				}
				writer.Flush();
				writer.Close();
			}
			catch (Exception ex2) {
				log("Threw exception writing hack config: "+ex2.ToString());
			}
        }
        
        public static string toUsefulString<K, V>(Dictionary<K, V> dic) {
        	StringBuilder sb = new StringBuilder();
        	sb.Append(dic.Count+":"+"{");
        	foreach (var entry in dic) {
        		sb.Append("["+entry.Key+" = "+entry.Value+"], ");
        	}
        	sb.Append("}");
        	return sb.ToString();
        }
        
        public static string toUsefulString<E>(IEnumerable<E> li) {
        	StringBuilder sb = new StringBuilder();
        	string size = "";
        	if (li is ICollection<E>) {
        		size = (li as ICollection<E>).Count+":";
        	}
        	sb.Append(size+"[");
        	foreach (E entry in li) {
        		sb.Append(entry.ToString()+", ");
        	}
        	sb.Append("]");
        	return sb.ToString();
        }
	}
}