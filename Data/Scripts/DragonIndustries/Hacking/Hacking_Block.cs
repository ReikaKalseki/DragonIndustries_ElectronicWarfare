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

using IMyDestroyableObject = VRage.Game.ModAPI.Interfaces.IMyDestroyableObject;

namespace DragonIndustries
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "HackingBlock_Large", "HackingBlock_Small")]
    
    public class HackingBlock : LogicCore {

        public enum States {
            OFFLINE,
            NOWORK,
            RUNNING,
            FAIL,
            SUCCESS
        };
    	
    	public enum TargetCategories {
    		WEAPON, //turrets
    		POWER, //batteries, solar, reactors
    		CONTROL, //cockpit, gyro, remote, etc
    		CONVEYOR, //conveyor sorters, cargo containers, gas tanks, etc 
    		PRODUCTION, //assemblers, refineries, gas gens, etc
    		OTHER //medical room, projector, timer block, etc
    	};

        public States state = States.OFFLINE;
        
        public long targetID = 0;
        public float targetDifficulty = 0;
        public int targetTime = 0;
        public int cyclesUntilAttempt = 0;
        public bool isIdle = false;
        
        //Does not need sync; has no client effect
        private float retaliatoryDamage = 0;
        private HashSet<TargetCategories> activeCategories = new HashSet<TargetCategories>();

        private const string tickSound = "BlockTimerSignalA";
        private const string successSound = "BlockTimerSignalC";
        private const string failSound = "ArcParticleElectricalDischarge";

        public const int blinkLength = 1;
        
        public const int successDelay = 1; //in cycles
        public const int failDelay = 3; //in cycles
        
        public readonly int HACK_SPEED = Configuration.getSetting(Settings.HACKSPEED).asInt();

        private int blinkTime = blinkLength;
        private int hackTick = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {			
        	doSetup("Utility", 0.6F, MyEntityUpdateEnum.EACH_10TH_FRAME);
        	prepareEmissives("Status", 1);
        	
        	foreach (TargetCategories tg in Enum.GetValues(typeof(TargetCategories))) {
        		activeCategories.Add(tg);
        	}
        }

        public override void Close() {
            thisBlock.AppendingCustomInfo -= updateInfo;
            base.Close();
        }

        public override void UpdateAfterSimulation10() {
        	base.UpdateAfterSimulation10();
        	
        	if (blinkTime <= 0) {        		
                Color clr = Color.Black;
                switch (state) {
                    case States.OFFLINE:
                        clr = Color.Red;
                        break;
                    case States.NOWORK:
                        clr = Color.Yellow;
                        break;
                    case States.RUNNING:
                        clr = new Color(16, 127, 255);
                        break;
                    case States.FAIL:
                        clr = Color.Orange;
                        break;
                    case States.SUCCESS:
                        clr = Color.Green;
                        break;
                }
            	setEmissiveChannel(1, clr, 1);
        	}
        	else {
           		blinkTime--;
        	}
        	
        	if (hackTick >= HACK_SPEED)
        		tickHack();
        	else
        		hackTick++;
        }
        
        private void tickHack() {
        	hackTick = 0;
        	
			if (!MyAPIGateway.Multiplayer.IsServer) {
				//MyAPIGateway.Utilities.ShowNotification("Not running hack code; is client", 5000, MyFontEnum.Red);
                return;
        	}

            try {
            	//MyAPIGateway.Utilities.ShowNotification("Running Hack Cycle "+cyclesUntilAttempt, 5000, MyFontEnum.Red);
            	if (!isFunctioning() || thisBlock.OwnerId == 0) {
                    state = States.OFFLINE;
                }
                else {
                	if (cyclesUntilAttempt > 0) {
                		cyclesUntilAttempt--;
                		if (targetID != 0 && cyclesUntilAttempt == 0) {
                			isIdle = false;
                			attemptHack();
                		}
                		else {
                			sync();
                		}
                	}
                	else {
                		isIdle = false;
                		findTarget();
            			sync();
                	}
                }
            }
            catch (Exception e) {
            	IO.log("Threw exception running hack cycle: "+e.ToString());
            }
        }

        protected override bool shouldUsePower() {
            return thisBlock.Enabled && thisBlock.IsFunctional;
        }
        
        private void findTarget() {
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			
			HashSet<string> blacklist = new HashSet<string>();
			if (Configuration.getSetting(Settings.ALLOWHACKSKIP).asBoolean() && !String.IsNullOrEmpty(thisBlock.CustomData)) {
		       	string[] parts = thisBlock.CustomData.Split(";".ToCharArray());
		       	foreach (string part in parts) {
		       		blacklist.Add(part);
        		}
			}
				
			findTargets(thisGrid, blocks, blacklist);
			foreach (IMyCubeGrid g in getOwnChildGrids())
				findTargets(g, blocks, blacklist);
			
			//IO.log(IO.toUsefulString(blocks));
			
			if (blocks.Count > 0) {				
				int idx = rand.Next(blocks.Count);
				IMyTerminalBlock block = blocks[idx].FatBlock as IMyTerminalBlock;
				//IO.log("Found "+block+", with category "+getTargetCategory(block)+"; active = "+isHackingCategory(block)+" of "+IO.toUsefulString(activeCategories));
				setTarget(block);
				//MyAPIGateway.Utilities.ShowNotification("Found target "+block.CustomName+"; cycles="+cyclesUntilAttempt, 5000, MyFontEnum.Red);
				state = States.RUNNING;
			}
			else {
				setTarget(null);
				isIdle = true;
				state = States.NOWORK;
			}
        }
        
        private void findTargets(IMyCubeGrid grid, List<IMySlimBlock> blocks, HashSet<string> blacklist) {
        	grid.GetBlocks(blocks, b => b.FatBlock is IMyTerminalBlock && isHackable(b.FatBlock as IMyTerminalBlock) && isHackingCategory(b.FatBlock as IMyTerminalBlock) && !blacklist.Contains((b.FatBlock as IMyTerminalBlock).CustomName));
        }
        
        protected override void doGuiInit() {
        	if (Configuration.getSetting(Settings.ALLOWHACKSKIP).asBoolean()) {
	        	foreach (TargetCategories tg in Enum.GetValues(typeof(TargetCategories))) {        		
	        		Func<IMyTerminalBlock, bool> cur = block => block.GameLogic.GetAs<HackingBlock>() != null && block.GameLogic.GetAs<HackingBlock>().isHackingCategory(tg);
	        		Action<IMyTerminalBlock, bool> set = (block, flag) => { if (block.GameLogic.GetAs<HackingBlock>() != null) { block.GameLogic.GetAs<HackingBlock>().setHackingCategory(tg, flag); }};
		            string label = tg.ToString().ToLowerInvariant();
		            label = char.ToUpper(label[0])+label.Substring(1);
		            string desc = "Whether the hacking computer will attempt to hack blocks of type '"+label+"'";
		            new OnOffButton<IMyUpgradeModule, HackingBlock>(this, tg.ToString(), "Hack "+label+" Blocks", desc, cur, set).register();
	        	}
        	}
        }
        
        private bool isHackingCategory(IMyTerminalBlock block) {
        	return isHackingCategory(getTargetCategory(block));
        }
        
        private bool isHackingCategory(TargetCategories tg) {
        	if (!Configuration.getSetting(Settings.ALLOWHACKSKIP).asBoolean())
        		return true;
        	return activeCategories.Contains(tg);
        }
        
        private void setHackingCategory(TargetCategories tg, bool set) {
        	if (!Configuration.getSetting(Settings.ALLOWHACKSKIP).asBoolean())
        		return;
        	if (set)
        		activeCategories.Add(tg);
        	else
        		activeCategories.Remove(tg);
        	//IO.log(MyAPIGateway.Multiplayer.IsServer+" "+IO.toUsefulString(activeCategories));
        }
        
        private TargetCategories getTargetCategory(IMyTerminalBlock block) {
        	if (block is IMyLargeTurretBase) {
        		return TargetCategories.WEAPON;
        	}
        	if (block is IMyUserControllableGun) {
        		return TargetCategories.WEAPON;
        	}
        	if (block is IMyWarhead) {
        		return TargetCategories.WEAPON;
        	}
        	if (block is IMySolarPanel) {
        		return TargetCategories.POWER;
        	}
        	if (block is IMyBatteryBlock) {
        		return TargetCategories.POWER;
        	}
        	if (block is IMyPowerProducer) {
        		return TargetCategories.POWER;
        	}
        	if (block is IMyShipController) {
        		return TargetCategories.CONTROL;
        	}
        	if (block is IMyGyro) {
        		return TargetCategories.CONTROL;
        	}
        	if (block is IMyRadioAntenna || block is IMyLaserAntenna) {
        		return TargetCategories.CONTROL;
        	}
        	if (block is IMyCargoContainer) {
        		return TargetCategories.CONVEYOR;
        	}
        	if (block is IMyConveyorSorter) {
        		return TargetCategories.CONVEYOR;
        	}
        	if (block is IMyGasTank) {
        		return TargetCategories.CONVEYOR;
        	}
        	if (block is IMyProductionBlock) {
        		return TargetCategories.PRODUCTION;
        	}
        	if (block is IMyGasGenerator) {
        		return TargetCategories.PRODUCTION;
        	}
        	return TargetCategories.OTHER;
        }
        
        private void attemptHack() {
        	IMyEntity target;
        	MyAPIGateway.Entities.TryGetEntityById(targetID, out target);
        	if (target != null) {
	        	MyCubeBlock block = target as MyCubeBlock; //because MyTerminalBlock is prohibited?!
	        	//MyAPIGateway.Utilities.ShowNotification("Hacking target "+(target as IMyTerminalBlock).CustomName, 5000, MyFontEnum.Red);
				if (rand.NextDouble() <= 1/targetDifficulty) {
	        		doHack(block);
				}
				else {
					state = States.FAIL;
					cyclesUntilAttempt = failDelay;
					if (retaliatoryDamage > 0) {
						(thisBlock as IMyDestroyableObject).DoDamage(retaliatoryDamage, MyDamageType.Bolt, true, null, targetID);
					}
				}
	        	//MyAPIGateway.Utilities.ShowNotification("Success for target "+(target as IMyTerminalBlock).CustomName+": "+(state == States.SUCCESS), 5000, MyFontEnum.Red);
	        	setTarget(Configuration.getSetting(Settings.HACKRETRY).asBoolean() && state == States.FAIL ? block as IMyTerminalBlock : null);
            	sync();
                isIdle = true; //set AFTER sync so gets it next tick
	        }
        	else {
        		IO.log("Could not find target entity (ID="+targetID+") to complete hacking!");
        	}
        }
        
        private void doHack(MyCubeBlock block) {
        	block.ChangeOwner(0, MyOwnershipShareModeEnum.Faction);
			block.ChangeBlockOwnerRequest(block.GameLogic.GetAs<HackingBlock>() != null && Configuration.getSetting(Settings.COMPUTERCONVERT).asBoolean() ? thisBlock.OwnerId : 0, MyOwnershipShareModeEnum.Faction); //clear ownership, unless is another hacking computer (convert that)
			if (block is IMyShipConnector) {
				IMyShipConnector c = block as IMyShipConnector;
				//turn off trade mode, when possible
				//c.GetActionWithName("TradeMode").Apply(c);
			}
			state = States.SUCCESS;
			cyclesUntilAttempt = successDelay;
        }

        private bool isHackable(IMyTerminalBlock block) {
            return block.IsFunctional && block.GetUserRelationToOwner(thisBlock.OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public void updateRender() {
            try {
        		//IO.log(state.ToString());
        		if (!isIdle) {
                	blinkTime = blinkLength;
                	setEmissiveChannel(1, Color.White, 1);
        		}
                
                switch (state) {
                    case States.RUNNING:
                        soundSource.playSound(tickSound, 10, 2.5F);
                        break;
                    case States.FAIL:
                        if (!isIdle) {
	                        soundSource.playSound(failSound, 10, 1);
                        }
                        break;
                    case States.SUCCESS:
                        if (!isIdle) {
                        	soundSource.playSound(successSound, 10, 2.5F);
                        }
                        break;
                    case States.OFFLINE:
                    case States.NOWORK:
                        break;
                }

                thisBlock.RefreshCustomInfo();
            }
            catch (Exception e) {
                IO.log("Threw exception while updating hacking computer render: "+e);
            }
        }

        protected override void updateInfo(IMyTerminalBlock block, StringBuilder sb) {
			if (thisBlock.OwnerId == 0L) {
                sb.Append("Hacking computer has no owner!");
                return;
            }

            if (state == States.OFFLINE) {
                sb.Append("Hacking computer is offline!");
            }
        	else if (state == States.NOWORK) {
                sb.Append("No valid targets found.");
            }
        	else {
                string targetName = "[None]";
                IMyEntity target;

                if (targetID != 0 && MyAPIGateway.Entities.TryGetEntityById(targetID, out target)) {
                    if (target is IMyTerminalBlock) {
                        targetName = (target as IMyTerminalBlock).CustomName;
                    }

	                sb.Append("Hacking "+targetName+"...\n");
	                sb.Append("Attempt Progress: "+Math.Round((100-(100F*cyclesUntilAttempt/targetTime)))+"%\n");
	                sb.Append("Predicted Success Probability: "+Math.Round(100F/targetDifficulty, 3)+"%\n\n");
                }
                else {
	                if (state == States.SUCCESS)
	                    sb.Append("Hacking result: Success");
	                else if (state == States.FAIL)
	                    sb.Append("Hacking result: Failure");
                    
                 	sb.Append("\n\nSeeking new target...");
                }
        	}
        		
        	//MyAPIGateway.Utilities.ShowNotification("Attempting to display...");
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			thisGrid.GetBlocks(blocks, b => b.FatBlock is IMyTextPanel && (b.FatBlock as IMyTextPanel).CustomName.Contains("HackingOut"));
			foreach (IMySlimBlock panelslim in blocks) {
				IMyTextPanel panel = panelslim.FatBlock as IMyTextPanel;
				panel.WriteText(sb.ToString());
				panel.ShowPublicTextOnScreen();
			}
        }

        private void setTarget(IMyTerminalBlock block) {
        	if (block == null) {
        		targetID = 0;
        		return;
        	}
			targetID = block.EntityId;
        	HackingDifficulty entry = Configuration.getHackingDifficulty(block);
        	targetDifficulty = Math.Max(1, (float)(entry.DifficultyFactor*Configuration.getSetting(Settings.HACKSCALE).asFloat()*0.8*Math.Pow(getComputerCount(block.SlimBlock), 0.675)));
        	targetTime = entry.RequiredTime;
        	cyclesUntilAttempt = targetTime;
        	retaliatoryDamage = Configuration.getSetting(Settings.HACKDAMAGE).asBoolean() ? entry.Retaliation : 0;
        }

        private int getComputerCount(IMySlimBlock block) {
        	MyCubeBlockDefinition.Component[] components = MyDefinitionManager.Static.GetCubeBlockDefinition(block.GetObjectBuilder()).Components;
            int computers = 0;
            foreach (MyCubeBlockDefinition.Component c in components) {
                if (c.Definition.Id.SubtypeName == "Computer")
                    computers += c.Count;
            }
            return computers;
        }
    }
}
