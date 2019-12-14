using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI;

using Sandbox.ModAPI.Interfaces.Terminal;

namespace DragonIndustries
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "RadarEmitter_Large", "RadarDevice_Small")]
    public class RadarEmitter : LogicCore {

        public static readonly float MW_PER_KM_SMALLGRID = Configuration.getSetting(Settings.RADARPOWERSMALL).asFloat();
        public static readonly float MW_PER_KM_LARGEGRID = Configuration.getSetting(Settings.RADARPOWERLARGE).asFloat();
        
        private float MW_PER_KM2;

        public long lastTick = 0;
        private BoundingBoxD scanRange;
        private BoundingBoxD scanArea;
        private float cachedRange;
        
        private readonly Dictionary<long, FoundGrid> grids = new Dictionary<long, FoundGrid>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {	            
        	doSetup("Defense", 0.001F, MyEntityUpdateEnum.EACH_100TH_FRAME);
        	MW_PER_KM2 = thisGrid.GridSizeEnum == MyCubeSize.Large ? MW_PER_KM_LARGEGRID : MW_PER_KM_SMALLGRID;
        	setRange(0);
            lastTick = DateTime.UtcNow.Ticks;
        }
        
        protected override float getRequiredPower() {
        	return MW_PER_KM2*getRange()*getRange();
        }
        
        private float getRange() { //ore detector stores meters, but we will use that as km
        	return (thisBlock as IMyOreDetector).Range;
        }
        
        private void setRange(float amt) { //ore detector stores meters, but we will use that as km
        	//(thisBlock as IMyOreDetector).Range = amt;
        	cachedRange = getRange();
        	updateRange();
        }
        
        private void updateRange() {
        	double d = this.cachedRange;
            scanRange = new BoundingBoxD(new Vector3D(-d, -d, -d), new Vector3D(d, d, d));
        }

        protected override void updateInfo(IMyTerminalBlock block, StringBuilder sb) {
        	if (thisGrid == null)
        		return;
        	sb.Append("Base power use is "+MW_PER_KM2+" MW/km^2");
        	sb.Append("\n");
        	sb.Append("Selected range is "+getRange()+" km");
        	sb.Append("\n");
        	sb.Append("Required power is "+getRequiredPower()+" MW");
        }

        public override void Close() {
            thisBlock.AppendingCustomInfo -= updateInfo;
            base.Close();
        }

        protected override bool shouldUsePower() {
        	return thisBlock.Enabled && thisBlock.IsFunctional;
        }

        public override void MarkForClose() {
        	removeEffect();
        }
        
        protected override void onEnergyLoss() {
        	removeEffect();
        }
        
        private void removeEffect() {
        	foreach (FoundGrid e in grids.Values) {
        		e.remove();
        	}
        	grids.Clear();
        }

        public override void UpdateAfterSimulation100() {            
            bool running = false;           
            
            /*if (!SwitchControl.Getter((IMyFunctionalBlock)Entity) && !isControlled()) {
            	((IMyFunctionalBlock)Entity).Enabled = false;
            }
            else */if (shouldUsePower()) {
                running = true;
            }
            
            //MyAPIGateway.Utilities.ShowNotification("Running: "+running);
            
            if (cachedRange != getRange()) {
            	setRange(getRange());
            }

            if (running) {
            	scanArea = scanRange.TransformSlow(thisBlock.WorldMatrix);            
	            List<IMyEntity> entityList = null;
	
	            lock (MyAPIGateway.Entities) {  // Scan for nearby entities (grids)
	                entityList = MyAPIGateway.Entities.GetElementsInBox(ref scanArea);
	            }                      
	
	            if (entityList != null) {
	                List<IMySlimBlock> gridBlocks = new List<IMySlimBlock>();
	                foreach (IMyEntity entity in entityList) {
						if (entity is IMyCubeGrid) {
							IMyCubeGrid grid = entity as IMyCubeGrid;
							handleGrid(grid);
	                	}
	                }
	            }
            }
            else {
            	removeEffect();
            }
            
            sync();
            thisBlock.RefreshCustomInfo();        
        }
        
        private void handleGrid(IMyCubeGrid grid) {
        	FoundGrid entry = getOrCreateEntry(grid);
        	entry.updateData();
        }
        
        private FoundGrid getOrCreateEntry(IMyCubeGrid grid) {
        	FoundGrid entry = null;
        	grids.TryGetValue(grid.EntityId, out entry);
        	if (entry == null) {
        		entry = new FoundGrid(grid);
        		grids.Add(grid.EntityId, entry);
        		MyAPIGateway.Utilities.ShowNotification("Found a new grid: "+entry.ToString());
        	}
        	return entry;
        }

        private bool isActualOreDetector(IMyTerminalBlock block) {
            return block.BlockDefinition.SubtypeName.Contains("OreDetector");
        }
        
        private void hideIrrelevantOreDetectorSettings() {
            try {
	            List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
	            MyAPIGateway.TerminalControls.GetActions<IMyOreDetector>(out actions);
	            foreach (IMyTerminalAction action in actions) {
            		//IO.log("Checking action '"+action.Id);
	            	if (action.Id.ToString() == "BroadcastUsingAntennas")
	            		action.Enabled = isActualOreDetector;
	            }
	
	            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            	MyAPIGateway.TerminalControls.GetControls<IMyOreDetector>(out controls);
	            foreach (IMyTerminalControl action in controls) {
            		//IO.log("Checking control '"+action.Id);
	            	if (action.Id.ToString() == "BroadcastUsingAntennas")
	            		action.Enabled = action.Visible = isActualOreDetector;
	            }
            }
            catch (Exception e) {
            	IO.log(e.ToString());
            }
        }
        
        protected override void doGuiInit() {
 			hideIrrelevantOreDetectorSettings();
 			/*
            Func<IMyTerminalBlock, bool> cur = block => block.GameLogic.GetAs<CloakingDevice>() != null && block.GameLogic.GetAs<CloakingDevice>().enableDerendering;
            Action<IMyTerminalBlock, bool> set = (block, flag) => { if (block.GameLogic.GetAs<CloakingDevice>() != null) block.GameLogic.GetAs<CloakingDevice>().setDerendering(flag); };
            string desc = "Whether the cloaking device not only scrambles turrets, but also makes the ship itself invisible.";
            new ToggleButton<IMyOreDetector, CloakingDevice>(this, "derender", "Make Grid Invisible", desc, cur, set).register();
            */
        }
    }
}
