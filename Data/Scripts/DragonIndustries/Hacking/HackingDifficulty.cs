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

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
	[Serializable]
    public class HackingDifficulty {
	
        public string BlockType;
        public int RequiredTime; //in 100t cycles
        public float DifficultyFactor;
        public float Retaliation; //as a % damage to the computer
		
		public HackingDifficulty() : this("", 1, 1) { //for deserialization
		
		}
		
        public HackingDifficulty(string type, int time, float fac) : this(type, time, fac, 0) {
			
		}
		
        public HackingDifficulty(string type, int time, float fac, float ret) {
			BlockType = type;
			RequiredTime = time;
			DifficultyFactor = fac;
			Retaliation = ret;
		}
		
		public override string ToString() {
        	return "Hacking Difficulty for "+BlockType+" takes "+RequiredTime+" cycles with difficulty x"+DifficultyFactor+", dmg = "+Retaliation;
		}
    }
	
}