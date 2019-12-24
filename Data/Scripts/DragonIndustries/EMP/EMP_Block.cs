using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities;

using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

using System;
using System.Collections.Generic;
using System.Text;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;
using IMyShipConnector = Sandbox.ModAPI.IMyShipConnector;
using IMyLandingGear = SpaceEngineers.Game.ModAPI.IMyLandingGear;

using IMyDestroyableObject = VRage.Game.ModAPI.Interfaces.IMyDestroyableObject;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

using EMPReaction = DragonIndustries.EMPReaction;
using SavedTimedBlock = DragonIndustries.SavedTimedBlock;
using IO = DragonIndustries.IO;
using Configuration = DragonIndustries.Configuration;
using FX = DragonIndustries.FX;
using IMyUpgradeModule = Sandbox.ModAPI.IMyUpgradeModule;

using Sandbox.ModAPI.Interfaces.Terminal;

namespace DragonIndustries {
    
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "EMP_Large", "EMP_Small")]
	
    public class EMP : LogicCore {

        public enum EMPStates {
            OFFLINE,
            CHARGING,
            READY,
            FIRING,
            COOLDOWN
        };
		
		public static readonly List<SavedTimedBlock> blockReactivations = new List<SavedTimedBlock>();
		
        private BoundingBoxD scanRange;
        private BoundingBoxD scanArea;
		private float distanceMultiplier;
		
		private const int CHARGING_TIME = 9;//10; //in 100t cycles, = 15s
		private const int COOLDOWN_TIME = 36;//10; //in 100t cycles, = 1min
		private const int MAX_FIRE_COUNT = 25;
		
		public EMPStates state = EMPStates.OFFLINE;
		private EMPStates lastState = EMPStates.OFFLINE;
		
		private int chargingCycles = 0;
		private int cooldownCycles = 0;
		
		private int fireCount = 0;
		public int currentEmissive;
		
		private static readonly Color[] colorCycleA = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};
		private static readonly Color[] colorCycleB = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};
		private static readonly Color[] colorCycleC = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};

		public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
			doSetup("Utility", 6, 60, MyEntityUpdateEnum.EACH_100TH_FRAME);
			prepareEmissives("ColorBand", 4);
		
			distanceMultiplier = 1;
			
			if (thisGrid.GridSizeEnum == MyCubeSize.Large) {
				distanceMultiplier = thisGrid.IsStatic ? 20 : 4;
			}
            
			double maxd = 0;
			foreach (var entry in Configuration.reactionMap) {
				EMPReaction es = entry.Value;
				maxd = Math.Max(maxd, es.MaxDistance*es.SameGridBoost);
			}
			
            double d = maxd*distanceMultiplier;
            scanRange = new BoundingBoxD(new Vector3D(-d, -d, -d), new Vector3D(d, d, d));
        }
		
		/*
		private bool isEMPerConnected() {
			List<IMySlimBlock> list = new List<IMySlimBlock>();
			emp_grid.GetBlocks(list, b => b.FatBlock is IMyTerminalBlock);
			bool connected = false;
			foreach (IMySlimBlock block1 in list) {
				IMyTerminalBlock block = block1.FatBlock as IMyTerminalBlock;
				//MyAPIGateway.Utilities.ShowNotification(block.CustomName+" / "+(block is IMyLandingGear)+" / "+(block is IMyShipConnector));
				if (block is IMyLandingGear)
					if ((block as IMyLandingGear).IsLocked)
						return true;
				else if (block is IMyShipConnector)
					if ((block as IMyShipConnector).IsConnected)
						return true;
			}
			return false;
		}
		*/

        protected override bool shouldUsePower() {
			return (state == EMPStates.CHARGING || state == EMPStates.READY || state == EMPStates.FIRING) && thisBlock.IsFunctional;
        }
		
		private void damageOnlineShip() {
			List<IMySlimBlock> gridBlocks = new List<IMySlimBlock>();
			thisGrid.GetBlocks(gridBlocks, b => b.FatBlock is IMyFunctionalBlock && isValidForOnlineDamage(b.FatBlock as IMyFunctionalBlock));
			foreach (IMySlimBlock slim in gridBlocks) {
				IMyFunctionalBlock block = slim.FatBlock as IMyFunctionalBlock;
				empBlock(slim, block, 0, true, Configuration.getEMPReaction(block), true, true);
			}
		}
		
		private bool isValidForOnlineDamage(IMyFunctionalBlock block) {  //build list first, then start damage, or it might damage the power supply first and then nothing else
			if (block == thisBlock || block is IMyLandingGear) //landing gear cannot be shut down, nor requires power, so always would incur damage
				return false;
			if ((block as MyCubeBlock).BuiltBy != (thisBlock as MyCubeBlock).BuiltBy) //since if different players, must be a "foreign" EMP leeched onto a station/ship, not a "self EMP"; use build-by, not owner, so that can affect things like thrusters
				return false;
			return block.IsWorking && block.Enabled;
		}
		
		private void cycleChargeColors() {
			int band = currentEmissive%4;
			int index = currentEmissive/4;
			
			if (index >= colorCycleB.Length)
				return;
			
			setEmissiveChannel(band, colorCycleB[index], 1);
			
			band++;
			if (band > 4) {
				band = 1;
				index++;
			}
		}
		
		public override void UpdateAfterSimulation10() {
			base.UpdateAfterSimulation10();
			if (state == EMPStates.FIRING) {
				fireEMP();
			}
			applyState();
		}
        
        public override void UpdateAfterSimulation100() {
			//MyAPIGateway.Utilities.ShowNotification("Run tick, block enabled: "+thisBlock.IsWorking, 1000, MyFontEnum.Red);
			
			if ((!thisBlock.IsWorking || !thisBlock.Enabled) && state != EMPStates.COOLDOWN) {
				state = EMPStates.OFFLINE;
			}
			switch (state) {
				case EMPStates.CHARGING:
					chargingCycles++;
					cycleChargeColors();
					if (chargingCycles >= CHARGING_TIME) {
						state = EMPStates.READY;
					}
				break;
				case EMPStates.FIRING:
				
				break;
				case EMPStates.OFFLINE:
					if (thisBlock.Enabled) {
						state = EMPStates.CHARGING;
					}
				break;
				case EMPStates.COOLDOWN:
					cooldownCycles++;
					if (cooldownCycles >= COOLDOWN_TIME) {
						state = EMPStates.OFFLINE;
					}
				break;
			}
			applyState();
			
			bool changedState = lastState != state;
			
			FX.EMPFX.ambientFX(this, state == EMPStates.READY, state == EMPStates.COOLDOWN);
			if (state == EMPStates.CHARGING)
				FX.EMPFX.chargingFX(this, changedState);
			
			lastState = state;
			
			for (int i = blockReactivations.Count - 1; i >= 0; i--) {
				SavedTimedBlock entry = blockReactivations[i];
				entry.TimeUntilReset -= 100;
				if (entry.TimeUntilReset <= 0) {
					entry.reactivateBlockIfPossible();
					blockReactivations.RemoveAt(i);
				}
			}
        }
		
		private void fireEMP() {
			//bool connected = isEMPerConnected();
			
			//MyAPIGateway.Utilities.ShowNotification("Block enabled, can run: "+connected);    
			//IO.debug();
			//if (connected) {
				bool doneFiring = affectEnemyBlocks();
				//NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
				bool end = doneFiring || fireCount >= MAX_FIRE_COUNT;
				FX.EMPFX.fireFX(this, end, rand);
				if (Configuration.getSetting(Settings.SELFDAMAGE).asBoolean())
					damageOnlineShip();
				fireCount++;
				//MyAPIGateway.Utilities.ShowNotification("Pulsed EMP", 5000, MyFontEnum.Red);
				if (end) { //one-time "fire" action
					 //to help in case was accidentally left on even though power was cut
					state = EMPStates.COOLDOWN;
					//MyAPIGateway.Utilities.ShowNotification("Finished firing EMP", 5000, MyFontEnum.Red);
					//NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
				}
			//}
		}
		
		private void applyState() {
			NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
			switch(state) {
				case EMPStates.FIRING:
					NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
					thisBlock.ApplyAction("OnOff_On");
					cooldownCycles = 0;
				break;
				case EMPStates.COOLDOWN:
					thisBlock.ApplyAction("OnOff_Off");
					setEmissiveChannel(0, Color.Black, 0);
					chargingCycles = 0;
					fireCount = 0;
				break;
				case EMPStates.OFFLINE:
					setEmissiveChannel(0, Color.Black, 0);
					chargingCycles = 0;
					cooldownCycles = 0;
				break;
				case EMPStates.CHARGING:
				
				break;
			}
		}

        private bool affectEnemyBlocks() {	
			bool done = true;
			
            scanArea = scanRange.TransformSlow(thisBlock.WorldMatrix);            
            List<IMyEntity> entityList = null;

            lock (MyAPIGateway.Entities) {  // Scan for nearby entities (grids)
                entityList = MyAPIGateway.Entities.GetElementsInBox(ref scanArea);
            }                      

            if (entityList != null) {
                List<IMySlimBlock> gridBlocks = new List<IMySlimBlock>();
                foreach (IMyEntity entity in entityList) {
					try {
						if (entity is IMyCubeGrid) {
							IMyCubeGrid grid = entity as IMyCubeGrid;
							gridBlocks.Clear();
							grid.GetBlocks(gridBlocks, b => b.FatBlock is IMyTerminalBlock && (b.FatBlock as IMyTerminalBlock).IsWorking);
							//MyAPIGateway.Utilities.ShowNotification("Found grid #"+i+" named "+grid.Name+" & "+grid.GetFriendlyName()+", ID="+grid.EntityId+"; size = "+grid.GridSizeEnum+", owners = "+grid.SmallOwners.ToString()+", grid elements = "+gridBlocks.ToString(), 5000, MyFontEnum.Red);
							foreach (IMySlimBlock slim in gridBlocks) {
								IMyTerminalBlock block = slim.FatBlock as IMyTerminalBlock;
								EMPReaction reaction = Configuration.getEMPReaction(block);
								if (reaction != null) {
									if (block.GetUserRelationToOwner(thisBlock.OwnerId) == MyRelationsBetweenPlayerAndBlock.Enemies) {
										bool share = thisGrid == grid;
										if (rand.Next(100) >= (share ? reaction.ResistanceSameGrid : reaction.Resistance)) {
											bool inRange = reaction.InfRangeSharedGrid;
											double distance = reaction.InfRangeSharedGrid ? 0 : Vector3D.Distance(thisBlock.GetPosition(), block.GetPosition());
											if (!inRange) {
												double d = reaction.MaxDistance;
												if (share) {
													//MyAPIGateway.Utilities.ShowNotification("boosting range (from "+d+" by "+reaction.SameGridBoost+"x) due to grid sharing ("+emp_grid.EntityId+"/"+emp_grid.GridSizeEnum+" & "+grid.EntityId+"/"+grid.GridSizeEnum+") for block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
													d *= reaction.SameGridBoost;
												}
												else {
													//MyAPIGateway.Utilities.ShowNotification("Not boosting range (from "+d+", using native "+distanceMultiplier+"x instead); no grid sharing ("+emp_grid.EntityId+"/"+emp_grid.GridSizeEnum+" & "+grid.EntityId+"/"+grid.GridSizeEnum+") for block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
													d *= distanceMultiplier;
												}
												inRange = distance < d;
											}
											if (inRange) {
												done &= empBlock(slim, block, distance, share, reaction, !(block is IMyFunctionalBlock), false);
											}
											else {
												//MyAPIGateway.Utilities.ShowNotification("Not EMPing block "+block.CustomName+" @ "+distance+"; out of range", 5000, MyFontEnum.Red);
											}
										}
										else if (reaction.Resistance < 100) {
											done = false;
										}
									}
								}
							}
						}
					}
					catch (Exception ex) {
						//MyAPIGateway.Utilities.ShowNotification("Could not run EMP cycle: "+ex.ToString(), 5000, MyFontEnum.Red);
						IO.log("Could not run EMP cycle: "+ex.ToString());
					}
				}
            }
			return done;
        }
		
		private bool empBlock(IMySlimBlock slimBlock, IMyTerminalBlock block, double distance, bool sameGrid, EMPReaction reaction, bool forceDamage, bool forceDestroy) {
			/*
			if (reaction == null) {
				MyAPIGateway.Utilities.ShowNotification("Block "+block.CustomName+" pulled null reaction?!", 5000, MyFontEnum.Red);
				return false;
			}
			if (slimBlock == null) {
				MyAPIGateway.Utilities.ShowNotification("Block "+block.CustomName+" has null slimblock?!", 5000, MyFontEnum.Red);
				return false;
			}
			if (block == null) {
				MyAPIGateway.Utilities.ShowNotification("Block "+slimBlock.BlockDefinition+" has null terminal block?!", 5000, MyFontEnum.Red);
				return false;
			}*/
			try {
				bool disabled = false;
				if ((slimBlock is IMyDestroyableObject) && (forceDamage || rand.Next(5) == 0)) {
					disabled = damageBlock(slimBlock, block, distance, forceDestroy);
				}
				else {
					IMyFunctionalBlock func = block as IMyFunctionalBlock;
					//func.ApplyAction("OnOff_Off");
					func.Enabled = false;
					func.UpdateIsWorking();
					//MyAPIGateway.Utilities.ShowNotification("EMP'd (on/off) block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
					disabled = true; //always successfully handled in the first cycle
				}
				if (disabled && !sameGrid && reaction.MaxDowntimeIfRemote >= 0)
					blockReactivations.Add(new SavedTimedBlock(block, reaction));
				reaction.triggerEffect(block, rand);
				return disabled;
			}
			catch (Exception ex) {
				MyAPIGateway.Utilities.ShowNotification("Could not EMP block "+block.CustomName+": "+ex.ToString(), 5000, MyFontEnum.Red);
				IO.log("Threw exception EMPing block "+block.CustomName+": "+ex.ToString());
				return true; //shut down to avoid constantly throwing exceptions
			}
		}
		
		private bool damageBlock(IMySlimBlock slimBlock, IMyTerminalBlock block, double distance, bool forceDestroy) {
			IMyDestroyableObject obj = slimBlock as IMyDestroyableObject;
			if (obj != null) {
				float damage = forceDestroy ? obj.Integrity*rand.Next(60, 90)/100F : Math.Max(1, 9+2*(float)rand.Next(1, 3)-((float)distance/6F));
				if (slimBlock.DamageRatio > 0.5F)
					obj.DoDamage(damage, MyDamageType.Weapon, true);
				block.UpdateIsWorking();
				//MyAPIGateway.Utilities.ShowNotification("EMP'd (damage) block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
				return !block.IsWorking || !block.IsFunctional;
			}
			return true;
		}
		
		public void startFiring() {
			if (state != EMPStates.READY) {
				return;
			}
			state = EMPStates.FIRING;
		}
        
        protected override void doGuiInit() {
            Action<IMyTerminalBlock> fire = block => { if (block.GameLogic.GetAs<EMP>() != null) block.GameLogic.GetAs<EMP>().startFiring(); };
            string desc = "Fires the EMP. Be careful about destroying your own ship or blocks!";
            new ControlButton<IMyUpgradeModule, EMP>(this, "fire", "Fire", desc, fire).register();
            handleStateValidity();
        }

        private bool isReadyToFire(IMyTerminalBlock block) {
			return block is EMP && (block as EMP).state == EMPStates.READY;
        }
		
		private void handleStateValidity() {
            try {
	            List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
	            MyAPIGateway.TerminalControls.GetActions<IMyUpgradeModule>(out actions);
	            foreach (IMyTerminalAction action in actions) {
            		//IO.log("Checking action '"+action.Id);
	            	if (action.Id.ToString() == "fire")
	            		action.Enabled = isReadyToFire;
	            }
	
	            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            	MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
	            foreach (IMyTerminalControl action in controls) {
            		//IO.log("Checking control '"+action.Id);
	            	if (action.Id.ToString() == "fire")
	            		action.Enabled = isReadyToFire;
	            }
            }
            catch (Exception e) {
            	IO.log(e.ToString());
            }
        }
        
    }
}