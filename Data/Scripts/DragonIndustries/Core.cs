using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace DragonIndustries {
	
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    
    public class Core : MySessionComponentBase {
    	
        private bool initialized = false;
        
        public override void UpdateBeforeSimulation() {
            if (!initialized) {
                initialized = true;
                Sync.initialize();
                Configuration.load();
            }
        }

        protected override void UnloadData() {
            Sync.unload();
            Configuration.unload();
            initialized = false;
        }
    }
}