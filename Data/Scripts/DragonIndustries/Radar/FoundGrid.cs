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
	    
    public class FoundGrid {
		
		private readonly IMyCubeGrid grid;
		private readonly string type;
		private readonly string owner;
		
		private readonly long gpsID;
		private readonly IMyGps gpsValue;
		
		private string position;

		public FoundGrid(IMyCubeGrid g) {
			grid = g;
			type = g.Physics == null ? "Station" : g.GridSizeEnum == MyCubeSize.Large ? "Large Ship" : "Small Ship";
			owner = calculateOwner();
			
			gpsValue = MyAPIGateway.Session.GPS.Create(ToString(), "", grid.GetPosition(), true);
			gpsID = gpsValue.Hash | ~(((long)gpsValue.Hash) << 32);
		}
		
		private string calculateOwner() {
			string ret = "[";
			List<IMyPlayer> li = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(li);
			foreach (long id in grid.BigOwners) {
				string name = null;
				IMyFaction fac = MyAPIGateway.Session.Factions.TryGetFactionById(id);
				if (fac != null) {
					name = fac.Name;
				}
				else {
					foreach (IMyPlayer ep in li) {
						if (ep.Identity.IdentityId == id) {
							name = ep.DisplayName;
							break;
						}
					}
				}
				if (name != null)
					ret = ret+name+", ";			
			}
			ret = ret+"]";
			return ret;
		}
		
		public void updateData() {
			Vector3D pos = grid.GetPosition();
			position = pos.X+", "+pos.Y+", "+pos.Z;
			gpsValue.Coords = pos;
			gpsValue.Name = ToString();
			MyAPIGateway.Session.GPS.ModifyGps(gpsID, gpsValue);
		}
		
		internal void remove() {
			MyAPIGateway.Session.GPS.RemoveGps(gpsID, gpsValue);
		}
		
		public string ToString() {
			return grid.DisplayName+" type "+type+" @ "+position+", owned by: "+owner;
		}
    }
}


