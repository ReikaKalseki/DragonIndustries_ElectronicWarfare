using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Collections;

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
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;


namespace DragonIndustries {
	
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class GatlingScrambler : TurretScrambler {
	    	
	}
	
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false)]
    public class MissileScrambler : TurretScrambler {
	    	
	}
	
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    public class InteriorScrambler : TurretScrambler {
	    	
	}
	    
    public abstract class TurretScrambler : MyGameLogicComponent {
		
		private IMyLargeTurretBase turret;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
			turret = Entity as IMyLargeTurretBase;
			NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation() {
			IMyEntity target = turret.Target;
            if (target != null) {
				if (target is IMyCubeBlock) {
	                try {
	                    IMyCubeGrid targetGrid = ((IMyCubeBlock)target).CubeGrid;
	                    if (CloakingDevice.isGridCloaked(targetGrid)) {
	                        turret.ResetTargetingToDefault();
							//MyAPIGateway.Utilities.ShowNotification("Scrambling turret "+turret.CustomName+", as it was targeting a hidden grid block "+target.DisplayName);
	                    }
	                    else {
							//MyAPIGateway.Utilities.ShowNotification("Not scrambling turret "+turret.CustomName+", as it was targeting a nonhidden grid block "+target.DisplayName);
	                    }
	                }
	            	catch (Exception e) {
	            		IO.log("Could not scramble turret "+turret.CustomName+" #"+turret.EntityId+"! "+e.ToString());
	                }
				}
				else {
					//MyAPIGateway.Utilities.ShowNotification("Not scrambling turret "+turret.CustomName+", as its target was not a block.");
				}
            }
			else {
				//MyAPIGateway.Utilities.ShowNotification("Not scrambling turret "+turret.CustomName+", as it had no target.");
			}
        }
    }
}


