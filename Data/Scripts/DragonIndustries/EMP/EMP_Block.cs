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

namespace DragonIndustries {
    
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "EMP_Large", "EMP_Small")]
	
    public class EMP : LogicCore {
		
        public static List<SavedTimedBlock> blockReactivations;
		
        private BoundingBoxD scanRange;
        private BoundingBoxD scanArea;
		private float distanceMultiplier;
		
		private const int CHARGING_TIME = 10; //in seconds
		private const int FIRE_DELAY = 4; //in 100t cycles
		private const int MAX_FIRE_COUNT = 25; //in 100t cycles
		
		public int cyclesOn = 0;
		public int cyclesUntilFire = FIRE_DELAY;
		public bool readyToFire = false;
		private int fireCount = 0;
		public int currentEmissive;
		
		private static readonly Color[] colorCycleA = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};
		private static readonly Color[] colorCycleB = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};
		private static readonly Color[] colorCycleC = {new Color(8, 0, 153), new Color(0, 58, 204), new Color(0, 142, 230), new Color(34, 170, 255), new Color(102, 196, 255), new Color(153, 216, 255), new Color(204, 235, 255)};

		public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
			doSetup("Utility", 3, MyEntityUpdateEnum.EACH_100TH_FRAME);
			prepareEmissives("ColorBand", 4);
		
			distanceMultiplier = 1;
			
			if (thisGrid.GridSizeEnum == MyCubeSize.Large) {
				distanceMultiplier = thisGrid.IsStatic ? 20 : 4;
			}
			
			blockReactivations = new List<SavedTimedBlock>();
			
			//Configuration.load();
            
			double maxd = 0;
			foreach (var entry in Configuration.reactionMap) {
				EMPReaction es = entry.Value;
				maxd = Math.Max(maxd, es.MaxDistance*es.SameGridBoost);
			}
			
            double d = maxd*distanceMultiplier;
            scanRange = new BoundingBoxD(new Vector3D(-d, -d, -d), new Vector3D(d, d, d));
        }
        
        //public override void UpdateAfterSimulation() {
        //	FX.renderLineFX();
        //}
		
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
			return (thisBlock.Enabled || readyToFire) && thisBlock.IsFunctional;
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
		
		public override void UpdateAfterSimulation10() {
			base.UpdateAfterSimulation10();
			
			if (cyclesOn > 0) {
				thisBlock.ApplyAction("OnOff_On"); //force to keep on once charging begins
				
				cycleChargeColors();
			}
			if (readyToFire && cyclesUntilFire <= 0) {
				fireEMP();
			}
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
        
        public override void UpdateAfterSimulation100() {
			//MyAPIGateway.Utilities.ShowNotification("Run tick, block enabled: "+thisBlock.IsWorking, 1000, MyFontEnum.Red);
			if (cyclesOn > 0 || (thisBlock.IsWorking && (thisBlock as IMyFunctionalBlock).Enabled)) {
				cyclesOn++;
				thisBlock.ApplyAction("OnOff_On"); //force to keep on once charging begins
				if (readyToFire) {
					FX.EMPFX.chargingFX(this, false, true, false);
				}
				else {
					readyToFire = cyclesOn >= CHARGING_TIME*3/5F; //each 5 seconds is 3 cycles
					if (readyToFire) {
						NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
						//MyAPIGateway.Utilities.ShowNotification("Activated EMP", 5000, MyFontEnum.Red);
						FX.EMPFX.chargingFX(this, false, true, true);
					}
					else {
						FX.EMPFX.chargingFX(this, cyclesOn == 1, false, false);
					}
				}
            }
			else {
				//cyclesOn = 0;
				setEmissiveChannel(0, Color.Black, 0);
			}
			
			if (readyToFire) {
				cyclesUntilFire--;
			}
			
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
			
			//if (connected) {
				bool doneFiring = affectEnemyBlocks();
				//NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
				FX.EMPFX.fireFX(this, doneFiring, rand);
				if (Configuration.getSetting(Settings.SELFDAMAGE).asBoolean())
					damageOnlineShip();
				fireCount++;
				//MyAPIGateway.Utilities.ShowNotification("Pulsed EMP", 5000, MyFontEnum.Red);
				if (doneFiring || fireCount >= MAX_FIRE_COUNT) { //one-time "fire" action
					thisBlock.ApplyAction("OnOff_Off"); //to help in case was accidentally left on even though power was cut
					readyToFire = false;
					cyclesUntilFire = FIRE_DELAY;
					setEmissiveChannel(0, Color.Black, 0);
					cyclesOn = 0;
					fireCount = 0;
					//MyAPIGateway.Utilities.ShowNotification("Finished firing EMP", 5000, MyFontEnum.Red);
					NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
					//NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
				}
			//}
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
												done &= empBlock(slim, block, distance, share, reaction, false, false);
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
			try {
				bool disabled = false;
				if ((slimBlock is IMyDestroyableObject) && (forceDamage || rand.Next(5) == 0)) {
					IMyDestroyableObject obj = slimBlock as IMyDestroyableObject;
					if (obj != null) {
						float damage = forceDestroy ? obj.Integrity*rand.Next(60, 90)/100F : Math.Max(1, 9+2*(float)rand.Next(1, 3)-((float)distance/6F));
						if (slimBlock.DamageRatio > 0.5F)
							obj.DoDamage(damage, MyDamageType.Weapon, true);
						block.UpdateIsWorking();
						disabled = !block.IsWorking || !block.IsFunctional;
					}
					//MyAPIGateway.Utilities.ShowNotification("EMP'd (damage) block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
				}
				else {
					block.ApplyAction("OnOff_Off");
					block.UpdateIsWorking();
					//MyAPIGateway.Utilities.ShowNotification("EMP'd (on/off) block "+block.CustomName+" @ "+distance, 5000, MyFontEnum.Red);
					disabled = true; //always successfully handled in the first cycle
				}
				if (disabled && !sameGrid && reaction.MaxDowntimeIfRemote >= 0)
					blockReactivations.Add(new SavedTimedBlock(block, reaction));
				reaction.triggerEffect(block, rand);
				return disabled;
			}
			catch (Exception ex) {
				//MyAPIGateway.Utilities.ShowNotification("Could not EMP block "+block.CustomName+": "+ex.ToString(), 5000, MyFontEnum.Red);
				IO.log("Threw exception EMPing block "+block.CustomName+": "+ex.ToString());
				return true; //shut down to avoid constantly throwing exceptions
			}
		}
        
    }
}