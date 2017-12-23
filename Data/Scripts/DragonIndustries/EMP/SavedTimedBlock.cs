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
    public class SavedTimedBlock {
	
		public long GridID;
		public Vector3I Position;
        public int TimeUntilReset;
        public int TotalTime;
		
		public SavedTimedBlock() : this(null, -1) {
		
		}
		
		public SavedTimedBlock(IMyTerminalBlock block, EMPReaction er) : this(block, er.MaxDowntimeIfRemote) {
			
		}
		
		public SavedTimedBlock(IMyTerminalBlock block, int time) {
			Position = block != null ? block.Position : new Vector3I(0, 0, 0);
			
			time *= 60; //because 60 ticks per second, and this will be called every 100 ticks, with a -= 100 arg
			
			TotalTime = time;
			TimeUntilReset = time;
			GridID = block != null ? ((block.CubeGrid as IMyCubeGrid).EntityId) : -1;
		}
		
		public void reactivateBlockIfPossible() {
			if (GridID < 0)
				return;
		/*
			IMyEntity entity;
			entity = MyAPIGateway.Entities.TryGetEntityById(GridID, entity);
			if (entity != null && entity is IMyCubeGrid) {
				IMyCubeGrid grid = entity as IMyCubeGrid;
				MyAPIGateway.Utilities.ShowNotification("ID "+GridID+" > "+grid, 5000, MyFontEnum.Red);
				IMySlimBlock slim = grid.GetCubeBlock(Position);
				MyAPIGateway.Utilities.ShowNotification("Pos "+Position+" > "+slim, 5000, MyFontEnum.Red);
				if (slim != null) {
					IMyTerminalBlock block = slim.FatBlock as IMyTerminalBlock;
					if (block != null && !block.IsWorking) {
						block.ApplyAction("OnOff_On");
					}
				}
			}*/
		}
    }
	
}