using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRage.Game.ModAPI;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using System.Text;
using VRage.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Entity;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;

using Sandbox.ModAPI.Interfaces.Terminal;

namespace DragonIndustries {
	
    public abstract class LogicCore : MyGameLogicComponent {
		
		private const bool EMISSIVES_ENABLED = false; //needs compatible models
    	
        protected readonly Random rand = new Random();
        
        protected IMyFunctionalBlock thisBlock;
        protected IMyCubeGrid thisGrid;
        
        protected MultiSoundSource soundSource;
        
        private MyResourceSinkComponent energySink = null;
        
        private static readonly HashSet<Type> initializedGUIs = new HashSet<Type>();
        
        private string[] emissiveNames;
        
        protected void doSetup(string powerPriority, float maxPowerInMW, params MyEntityUpdateEnum[] updateCycles) {
        	doSetup(powerPriority, maxPowerInMW, maxPowerInMW, updateCycles);
        }
        
        protected void doSetup(string powerPriority, float smallGridMW, float largeGridMW, params MyEntityUpdateEnum[] updateCycles) {
			NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            
            if (updateCycles.Length == 0) {
            	IO.log("WARNING: "+this+" has no update cycles set!");
            }
            
            foreach (MyEntityUpdateEnum e in updateCycles) {
	            NeedsUpdate |= e;
	            Entity.NeedsUpdate |= e;
            }
            
        	thisBlock = Container.Entity as IMyFunctionalBlock;
			thisGrid = thisBlock.CubeGrid as IMyCubeGrid;
			
			thisBlock.Enabled = false;
			Sandbox.ModAPI.Ingame.TerminalBlockExtentions.ApplyAction(thisBlock, "OnOff_Off");
			
            energySink = new MyResourceSinkComponent(1);
            energySink.Init(MyStringHash.GetOrCompute(powerPriority), thisGrid.GridSizeEnum == MyCubeSize.Small ? smallGridMW : largeGridMW, calcRequiredPower);
            if (thisBlock.Components.Contains(typeof(MyResourceSinkComponent))) {
              	IO.log("Power sinks already present in "+this+" #"+Entity.EntityId+":");
              	List<MyResourceSinkComponent> li = new List<MyResourceSinkComponent>();
              	Dictionary<Type, MyComponentBase>.ValueCollection.Enumerator e = thisBlock.Components.GetEnumerator();
              	while (e.MoveNext()) {
              		if (e.Current is MyResourceSinkComponent) {
              			MyResourceSinkComponent req = e.Current as MyResourceSinkComponent;
              			IO.log(">> "+req.GetType()+" needing "+req.MaxRequiredInput+", of "+req.AcceptedResources.ToString());
              		}
              	}
                thisBlock.Components.Remove<MyResourceSinkComponent>();
            }
            thisBlock.Components.Add(energySink);
            energySink.Update();
            
            thisBlock.IsWorkingChanged += onWorkingChanged;
            thisBlock.AppendingCustomInfo += updateInfo;
            MyAPIGateway.TerminalControls.CustomControlGetter += filterControls;

            soundSource = new MultiSoundSource(thisBlock);
			
			IO.log("Loaded logic script "+this+" for block '"+thisBlock.CustomName+"' / '"+Entity.DisplayName+"' #"+Entity.EntityId+"; update rate = "+NeedsUpdate);
        }
        
		public sealed override void UpdateOnceBeforeFrame() {
			thisBlock.Enabled = false;
			Sandbox.ModAPI.Ingame.TerminalBlockExtentions.ApplyAction(thisBlock, "OnOff_Off");
		}
        
        protected void prepareEmissives(string prefix, int count) {
        	if (!EMISSIVES_ENABLED)
        		return;
        	emissiveNames = new string[count];
        	if (count == 1) {
        		emissiveNames[0] = prefix;
        	}
        	else {
	        	for (int i = 1; i <= count; i++) {
	        		emissiveNames[i] = prefix+i;
	        	}
        	}
        }
        
        /** Pass in a channel "0" to set all. */
        public void setEmissiveChannel(int channel, Color clr, float value) {
        	if (!EMISSIVES_ENABLED)
        		return;
        	if (channel == 0) {
        		foreach (string em in emissiveNames) {
        			thisBlock.SetEmissiveParts(em, clr, value);
        		}
        	}
        	else {
        		thisBlock.SetEmissiveParts(emissiveNames[emissiveNames.Length == 1 ? 0 : channel-1], clr, value);
        	}
        }

        protected virtual void updateInfo(IMyTerminalBlock block, StringBuilder sb) {
            
        }

        private void filterControls(IMyTerminalBlock block, List<IMyTerminalControl> controls) {
        	foreach (IMyTerminalControl control in new List<IMyTerminalControl>(controls)) {
        		//MyAPIGateway.Utilities.ShowNotification(block.CustomName+" check "+control.Id+" > '"+lgc+"' & "+GetType());
        		if (!ControlFuncs.isOptionApplicable(block, control, this)) {
			        controls.Remove(control);
			    }
			}
        }

        protected virtual void onWorkingChanged(IMyCubeBlock block) {
        	energySink.Update();
        }

        public override void Close() {
            soundSource.stopAllSounds();
            base.Close();
        }
        
	    protected void sync() {
	      	Sync.sendSyncData(this);
	    }
        
        protected abstract bool shouldUsePower();
        
        private float calcRequiredPower() {
        	return shouldUsePower() ? getRequiredPower() : 0;
        }
        
        protected virtual float getRequiredPower() {
        	return energySink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
        }

        public bool isFunctioning() {
            return energySink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && shouldUsePower();
        }
        
        public override void UpdateAfterSimulation10() {           	
        	if (MyAPIGateway.Multiplayer.IsServer) {
           		initializeGUI();
           		
        		energySink.Update();
        		
        		//MyAPIGateway.Utilities.ShowNotification(calcRequiredPower()+" for "+this);
        		if (!energySink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, calcRequiredPower())) {
        			onEnergyLoss();
	        	}
        	}
        }

        private void initializeGUI() {
        	if (initializedGUIs.Contains(GetType()))
            	return;
        	initializedGUIs.Add(GetType());
            
            IO.log("Initializing "+this+" GUI Hooks");        	
            doGuiInit();
        }
        
        protected virtual void doGuiInit() {
        	
        }
        
        protected virtual void onEnergyLoss() {
        	
        }
        
        protected List<IMyCubeGrid> getOwnChildGrids(bool recurse = true) {
        	return getChildGridsOf(thisGrid);
        }
        
        protected static List<IMyCubeGrid> getChildGridsOf(IMyCubeGrid grid) {
        	List<IMyCubeGrid> ret = new List<IMyCubeGrid>();
        	List<IMySlimBlock> li = new List<IMySlimBlock>();
        	grid.GetBlocks(li);
        	foreach (IMySlimBlock b in li) {
        		IMyCubeBlock bk = b.FatBlock;
        		if (bk is IMyMechanicalConnectionBlock) {
        			//MyAPIGateway.Utilities.ShowNotification("Found a connection "+bk.DefinitionDisplayNameText);
        			IMyAttachableTopBlock con = (bk as IMyMechanicalConnectionBlock).Top;
        			if (con != null && con.CubeGrid != null && con.CubeGrid != grid && !ret.Contains(con.CubeGrid)) {
        				//MyAPIGateway.Utilities.ShowNotification("Found grid ID "+con.CubeGrid.EntityId+" as a child of "+grid.EntityId);
          				ret.Add(con.CubeGrid);
        			}
        		}
        	}
        	return ret;
        }
        
        public MultiSoundSource getSounds() {
        	return soundSource;
        }
    }
}