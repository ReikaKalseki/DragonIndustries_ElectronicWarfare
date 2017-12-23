﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;

using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.Entities;

using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Utils;

using System.IO;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
	public static class FX {
		
		public static class HackingFX {
			
		}
		
		public static class EMPFX {
			
			public static void chargingFX(EMP emp, bool justOn, bool charged, bool justCharged) {
				if (justOn) {
					emp.getSounds().playSound("ShipJumpDriveCharging", 30, 2);
				}
				if (justCharged) {
					emp.getSounds().playSound("ArcDroneLoopSmall", 30, 4);
				}
			}
			
			public static void fireFX(EMP emp, bool done, Random rand) {
				if (done) {
					emp.getSounds().playSound("ArcBlockEject", 30, 4);
					emp.getSounds().stopSound("ArcDroneLoopSmall");
				}
				emp.getSounds().playSound("ShipJumpDriveJumpOut", 30, 4);
				
				/*
				Vector3D m_center = emp.WorldAABB.Center;			
				Vector4 color = Color.LightBlue.ToVector4();			
				for (int cnt = 0; cnt < 50; cnt++) {
					Vector3D norm = MyUtils.GetRandomVector3Normalized();
					Vector3D point = m_center + Vector3D.Multiply(norm, 2+rand.NextDouble()*5);
					//IO.log("Drawing line from "+m_center.ToString()+" to "+point.ToString());
					MySimpleObjectDraw.DrawLine(m_center, point, MyStringId.GetOrCompute("particle_laser"), ref color, 0.5F);
				}
				*/
			}
		}
	}
}