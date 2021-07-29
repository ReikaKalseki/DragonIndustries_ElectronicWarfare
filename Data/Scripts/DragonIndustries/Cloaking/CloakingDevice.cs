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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_OreDetector), false, "CloakingDevice_Large", "CloakingDevice_Small")]
    public class CloakingDevice : LogicCore
    {
        private static readonly HashSet<long> cloakedGrids = new HashSet<long>();

        public static readonly float MW_PER_TONNE_SMALLGRID = Configuration.getSetting(Settings.CLOAKPOWERSMALL).asFloat();
        public static readonly float MW_PER_TONNE_LARGEGRID = Configuration.getSetting(Settings.CLOAKPOWERLARGE).asFloat();
        public static readonly float WEAPON_USE_MULTIPLIER = Configuration.getSetting(Settings.CLOAKWEAPONPOWER).asFloat();
        public static readonly float DERENDER_MULTIPLIER = Configuration.getSetting(Settings.CLOAKRENDERPOWER).asFloat();
        
        private float MW_PER_TONNE;

        public long lastTick = 0;
        
        public bool enableDerendering = false;
        private readonly List<IMySlimBlock> turrets = new List<IMySlimBlock>();
        private readonly List<IMySlimBlock> connections = new List<IMySlimBlock>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {	            
        	doSetup("Defense", 0.001F, MyEntityUpdateEnum.EACH_100TH_FRAME);
        	MW_PER_TONNE = thisGrid.GridSizeEnum == MyCubeSize.Large ? MW_PER_TONNE_LARGEGRID : MW_PER_TONNE_SMALLGRID;
            
            lastTick = DateTime.UtcNow.Ticks;
        }

        protected override void updateInfo(IMyTerminalBlock block, StringBuilder sb) {
        	if (thisGrid == null || thisGrid.Physics == null)
        		return;
        	sb.Append("Ship mass is "+thisGrid.Physics.Mass+" kg. Base power use is "+MW_PER_TONNE+" kW/kg");
        	sb.Append("\n\n");
        	sb.Append("Weapons (x"+turrets.Count+") Active: "+isShooting());
        	if (isShooting()) {
        		sb.Append(" (energy use x"+WEAPON_USE_MULTIPLIER+")");
        	}
        	sb.Append("\n");
        	sb.Append("Invisibility Enabled: "+shouldHideRender());
        	if (shouldHideRender()) {
        		sb.Append(" (energy use x"+DERENDER_MULTIPLIER+")");
        	}
        	sb.Append("\n\n");
        	sb.Append("Required power is "+getRequiredPower()+" MW");
        	sb.Append("\n\n");
        	sb.Append("Cloaking "+connections+" grids");
        }

        public override void Close() {
            thisBlock.AppendingCustomInfo -= updateInfo;
            base.Close();
        }
        
        public static void loadCloakedGridsFromFile(List<long> li) {
        	cloakedGrids.Clear();
        	cloakedGrids.UnionWith(li);
        }
        
        public static bool isGridCloaked(IMyCubeGrid grid) {
        	return cloakedGrids.Contains(grid.EntityId);
        }
        
        public static List<long> getCloakingList() {
        	return new List<long>(cloakedGrids);
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
            thisGrid.Visible = true;
        	if (thisGrid.Render != null) {
                thisGrid.Render.Visible = true;
            }
        }

        public override void UpdateAfterSimulation100() {            
            bool running = false;           
            
            /*if (!SwitchControl.Getter((IMyFunctionalBlock)Entity) && !isControlled()) {
            	((IMyFunctionalBlock)Entity).Enabled = false;
            }
            else */if (shouldUsePower()) {
                running = true;
            }
            
            MyAPIGateway.Utilities.ShowNotification("Running: "+running);
            
            HashSet<IMyCubeGrid> grids = new HashSet<IMyCubeGrid>();
            grids.Add(thisGrid);
            
            connections.Clear();
            thisGrid.GetBlocks(connections, b => b.FatBlock is IMyMechanicalConnectionBlock);
            foreach (IMySlimBlock b in connections) {
            	IMyCubeGrid connection = ((IMyMechanicalConnectionBlock)b.FatBlock).TopGrid;
            	if (connection != null) {
            		grids.Add(connection);
            	}
            }

            if (running) {
            	foreach (IMyCubeGrid id in grids) {
                	cloakedGrids.Add(id.EntityId);
	                if (shouldHideRender()) {
	                	setGridVisible(id, false);
	                }
	                else {
		                setGridVisible(id, true);
	                }
            	}
            }
            else {
            	foreach (IMyCubeGrid id in grids) {
                	cloakedGrids.Remove(id.EntityId);
                	setGridVisible(id, true);
            	}
            }
            
            sync();
            thisBlock.RefreshCustomInfo();        
        }
        
        private void setGridVisible(IMyCubeGrid grid, bool visible) {
        	grid.Render.Visible = true;
	        grid.Visible = visible;
	                
	        if (visible) {
	        	grid.Render.UpdateRenderObject(true, true);
	        }
	        else {
	        	grid.Render.RemoveRenderObjects();
     			grid.Render.AddRenderObjects();
	        	grid.Render.UpdateRenderObject(true, true);
	        	grid.Render.UpdateRenderObject(false, true);
	        }
	        
	        foreach (IMyCubeGrid g2 in getChildGridsOf(grid)) {
	        	setGridVisible(g2, visible);
	        }
	                //thisGrid.Render.RemoveRenderObjects();
	                
	                //thisGrid.Render.UpdateRenderObject(true);
	        //thisGrid.Render.UpdateRenderObject(false);
        }

        protected override float getRequiredPower() {
            float neededPower = 0;
            if (thisGrid.Physics != null) { //this is null on stations
                //isShooting();
                float f = isShooting() ? WEAPON_USE_MULTIPLIER : 1;
                f *= shouldHideRender() ? DERENDER_MULTIPLIER : 1;
                neededPower = thisGrid.Physics.Mass/1000F*MW_PER_TONNE*f;
            }
            lastTick = DateTime.UtcNow.Ticks;
            //MyAPIGateway.Utilities.ShowNotification("Required power for mass "+thisGrid.Physics.Mass+" kg : "+neededPower+" MW; shooting: "+isShooting()+", derender: "+shouldHideRender());
            return neededPower;
        }
        
        private bool shouldHideRender() {
        	return enableDerendering;
        }

        private bool isShooting() {            
        	turrets.Clear();
            thisGrid.GetBlocks(turrets, b => b.FatBlock is IMyUserControllableGun || b.FatBlock is IMyLargeTurretBase);
            
            foreach (IMySlimBlock turret in turrets) {
            	MyObjectBuilder_CubeBlock obj = null;
            	bool f1 = false;
            	bool f2 = false;
            	bool f3 = false;
            	bool f4 = false;
            	try {
	                obj = ((MyObjectBuilder_UserControllableGun)turret.FatBlock.GetObjectBuilderCubeBlock());
	                f1 = true;
	                if (obj is MyObjectBuilder_UserControllableGun) {
	                	MyObjectBuilder_UserControllableGun gun = obj as MyObjectBuilder_UserControllableGun;
	                	f2 = true;
	                	if (gun.IsShooting || gun.IsShootingFromTerminal) {
	                		f3 = true;
	                		return true;
	                	}
	                }
	                else if ((obj as MyObjectBuilder_TurretBase).GunBase != null && (obj as MyObjectBuilder_TurretBase).GunBase.LastShootTime > lastTick) {
	                	f4 = true;
	                	return true;
	                }
            	}
            	catch (Exception e) {
            		//MyAPIGateway.Utilities.ShowNotification("Threw exception: "+e.ToString()+" on block "+turret+" with "+obj);
            		IO.log("Threw exception checking if cloaked ship is shooting: "+e.ToString()+" on block "+turret+" with "+obj+" & flags "+f1+", "+f2+", "+f3+", "+f4);
            	}
            }
            
            return false;

        }
        
        public void setDerendering(bool flag) {
        	enableDerendering = flag;
        	thisBlock.UpdateVisual();
        	thisBlock.RefreshCustomInfo();
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
	            	if (action.Id.ToString() == "Range")
	            		action.Enabled = action.Visible = isActualOreDetector;
	            }
            }
            catch (Exception e) {
            	IO.log(e.ToString());
            }
        }
        
        protected override void doGuiInit() {
 			hideIrrelevantOreDetectorSettings();
            Func<IMyTerminalBlock, bool> cur = block => block.GameLogic.GetAs<CloakingDevice>() != null && block.GameLogic.GetAs<CloakingDevice>().enableDerendering;
            Action<IMyTerminalBlock, bool> set = (block, flag) => { if (block.GameLogic.GetAs<CloakingDevice>() != null) block.GameLogic.GetAs<CloakingDevice>().setDerendering(flag); };
            string desc = "Whether the cloaking device not only scrambles turrets, but also makes the ship itself invisible.";
            new ToggleButton<IMyOreDetector, CloakingDevice>(this, "derender", "Make Grid Invisible", desc, cur, set).register();
        }
    }
}
