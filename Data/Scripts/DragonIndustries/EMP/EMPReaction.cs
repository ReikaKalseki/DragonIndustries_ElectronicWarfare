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
    public class EMPReaction {
	
        public string BlockType;
        public int Resistance;
        public int ResistanceSameGrid;
        public double MaxDistance;
		public float SameGridBoost;
		public bool InfRangeSharedGrid;
		public int MaxDowntimeIfRemote;
		
		private Action<IMyTerminalBlock> runEffect;
		private int effectChance;
		
		public EMPReaction() : this("", 0, 0) { //for deserialization
		
		}
		
		public EMPReaction(string type, int res, double dist) : this(type, res, res, dist) {
			
		}
		
		public EMPReaction(string type, int res, int res2, double dist) : this(type, res, res2, dist, 1) {
			
		}
		
		public EMPReaction(string type, int res, int res2, double dist, float boost) : this(type, res, res2, dist, boost, -1) {
		
		}
		
		public EMPReaction(string type, int res, int res2, double dist, float boost, int time) {
			BlockType = type;
			Resistance = res;
			ResistanceSameGrid = res2;
			MaxDistance = dist;
			SameGridBoost = Math.Abs(boost);
			InfRangeSharedGrid = boost < 0;
			MaxDowntimeIfRemote = time;
		}
		
		public EMPReaction addEffect(Action<IMyTerminalBlock> effect, int chance) {
			runEffect = effect;
			this.effectChance = chance;
			return this;
		}
		
		public void triggerEffect(IMyTerminalBlock block, Random rand) {
			if (runEffect != null && rand.Next(100) <= effectChance)
				runEffect(block);
		}
		
		public override string ToString() {
        	return "EMP Reaction for "+BlockType+" has "+Resistance+"/"+ResistanceSameGrid+"% resistances with range "+MaxDistance+"m x"+SameGridBoost+" for share ("+InfRangeSharedGrid+"), for "+MaxDowntimeIfRemote+" s";
		}
    }
		
	/* f---ing C# does not allow this
	public enum DefaultReactions {
		ANTENNA("RadioAntenna",			80, 	95,		40, 	0.8F	),
		LASERANTENNA("LaserAntenna",	60, 			25				),
		BATTERY("Battery",				100,	95, 	1.25,	10F		),
		SMALLREACTOR("SmallGenerator",	75, 	40,		1.25,	24F		),
		LARGEREACTOR("LargeGenerator",	90,		60,		1.75,	24F		),
		COCKPIT("Cockpit",				80,		50,		10,		6		),
		GYRO("Gyro",					100,	25,		15,		8F		),
		REMOTE("RemoteControl",			90,		40,		20,		6F		),
		GATLING("GatlingTurret",		50,		25,		15,		1.5F	),
		INTERIORTURRET("InteriorTurret",100,	80,		2.5,	2F		),
		MISSILE("MissileTurret",		50,		20,		15,		1.5F	),
		ROCKET("MissileLauncher",		75,		60,		10,		2F		),
		ASSEMBLER("Assembler",			10,		0,		40,		10F		),
		REFINERY("Refinery",			10,		0,		40,		10F		),
		ARCFURNACE("BlastFurnace",		10,		0,		40,		10F		),
		TEXT("TextPanel",				100,	100,	0				),
		LCD("LCDPanel",					100,	100,	0				),
		WIDELCD("LCDPanelWide",			100,	100,	0				),
		SMALLCARGO("SmallContainer",	100,	100,	2.5				),
		LARGECARGO("LargeContainer",	100,	100,	2.5				),
		SORTER("ConveyorSorter",		20,		0,		30,		10F		);
	} */
	
}