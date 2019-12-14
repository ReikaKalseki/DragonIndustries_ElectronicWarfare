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

namespace DragonIndustries
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "Sniffer_Large", "Sniffer_Small")]
    public class DeviceSniffer : LogicCore {
     
        public long lastTick = 0;
        private HashSet<BlocksToFind> activeCategories = new HashSet<BlocksToFind>();
        private Dictionary<BlocksToFind, int> counts = new Dictionary<BlocksToFind, int>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {	            
        	doSetup("Utility", 0.12F, MyEntityUpdateEnum.EACH_100TH_FRAME);
            
            lastTick = DateTime.UtcNow.Ticks;
        }

        protected override void updateInfo(IMyTerminalBlock block, StringBuilder sb) {
        	if (thisGrid == null)
        		return;
        	foreach (BlocksToFind tg in Enum.GetValues(typeof(BlocksToFind))) {
        		if (activeCategories.Contains(tg)) {
        		    int found = 0;
        		    counts.TryGetValue(tg, out found);
        			sb.Append("Found "+found+" "+tg);
        		}
        	}
        	sb.Append("\n\n");
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

            if (running) {
            	counts.Clear();
            	List<IMySlimBlock> li = new List<IMySlimBlock>();
            	thisGrid.GetBlocks(li, b => b.FatBlock is IMyTerminalBlock);
            	foreach (IMySlimBlock b in li) {
            		IMyTerminalBlock tb = b.FatBlock as IMyTerminalBlock;
            		BlocksToFind get = getSeekCategory(tb);
            		if (get != BlocksToFind.NONE && this.isSeekCategory(get)) {
            			highlightBlock(get, tb);
            		}
            	}
            }
            else {
            	
            }
            
            sync();
            thisBlock.RefreshCustomInfo();        
        }
        
        private void highlightBlock(BlocksToFind type, IMyTerminalBlock b) {
        	int has = 0;
        	counts.TryGetValue(type, out has);
        	has++;
        	counts[type] = has;
        	b.ShowInTerminal = true;
        	b.ShowOnHUD = true;
        	b.Visible = true;
        }
        
        protected override void doGuiInit() {
	        foreach (BlocksToFind tg in Enum.GetValues(typeof(BlocksToFind))) {        		
	        	Func<IMyTerminalBlock, bool> cur = block => block.GameLogic.GetAs<DeviceSniffer>() != null && block.GameLogic.GetAs<DeviceSniffer>().isSeekCategory(tg);
	        	Action<IMyTerminalBlock, bool> set = (block, flag) => { if (block.GameLogic.GetAs<DeviceSniffer>() != null) { block.GameLogic.GetAs<DeviceSniffer>().setSeekCategory(tg, flag); }};
		        string label = tg.ToString().ToLowerInvariant();
		        label = char.ToUpper(label[0])+label.Substring(1);
		        string desc = "Whether the sniffer will seek and highlight '"+label+"'";
		        new OnOffButton<IMyUpgradeModule, DeviceSniffer>(this, tg.ToString(), "Find "+label+" Blocks", desc, cur, set).register();
	        }
        }
        
        private bool isSeekCategory(IMyTerminalBlock block) {
        	return isSeekCategory(getSeekCategory(block));
        }
        
        private bool isSeekCategory(BlocksToFind tg) {
        	return activeCategories.Contains(tg);
        }
        
        private void setSeekCategory(BlocksToFind tg, bool set) {
        	if (set)
        		activeCategories.Add(tg);
        	else
        		activeCategories.Remove(tg);
        	//IO.log(MyAPIGateway.Multiplayer.IsServer+" "+IO.toUsefulString(activeCategories));
        }
        
        private BlocksToFind getSeekCategory(IMyTerminalBlock block) {
        	if (block is IMySolarPanel) {
        		return BlocksToFind.POWER;
        	}
        	if (block is IMyBatteryBlock) {
        		return BlocksToFind.POWER;
        	}
        	if (block is IMyReactor) {
        		return BlocksToFind.POWER;
        	}
        	if (block is IMyRemoteControl) {
        		return BlocksToFind.REMOTES;
        	}
        	if (block is IMyCockpit) {
        		return BlocksToFind.COCKPIT;
        	}
        	if (block is IMyProgrammableBlock) {
        		return BlocksToFind.PROGRAM;
        	}
        	if (block is IMyTimerBlock) {
        		return BlocksToFind.PROGRAM;
        	}
        	if (block is IMyCargoContainer) {
        		return BlocksToFind.CARGO;
        	}
        	if (block is IMyGasTank) {
        		return BlocksToFind.CARGO;
        	}
        	return BlocksToFind.NONE;
        }
    	
    	public enum BlocksToFind {
    		REMOTES,
    		COCKPIT,
    		POWER, //batteries, solar, reactors
    		PROGRAM, //programmable blocks, timers
    		CARGO,
    		NONE
    	};
    }
}
