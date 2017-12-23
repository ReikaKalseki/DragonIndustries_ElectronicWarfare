using System;
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

using System.IO;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

using MyEntity = VRage.Game.Entity.MyEntity;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
	public class MultiSoundSource {
		
        private static readonly Dictionary<string, MySoundPair> soundSet = new Dictionary<string, MySoundPair>();
        
        private readonly Dictionary<string, MyEntity3DSoundEmitter> playingSounds = new Dictionary<string, MyEntity3DSoundEmitter>();
        
        private readonly Vector3D position;
        private readonly MyEntity entity;
        
        public MultiSoundSource(IMyCubeBlock block) : this(block.WorldAABB.Center) {
        	entity = block as MyEntity;
        }
        
        public MultiSoundSource(Vector3D pos) {
        	position = pos;
        }
        
        private MyEntity3DSoundEmitter getOrCreateEmitter(string snd) {
        	MyEntity3DSoundEmitter em = null;
			playingSounds.TryGetValue(snd, out em);
			if (em == null) {
				em = new MyEntity3DSoundEmitter(entity);
				if (entity == null) {
					em.SetPosition(position);
					em.SetVelocity(Vector3.Zero);
				}
				playingSounds.Add(snd, em);
			}
			return em;
        }
        
        private static MySoundPair getOrCreateSound(string snd) {
        	MySoundPair sp = null;
			soundSet.TryGetValue(snd, out sp);
			if (sp == null) {
				sp = new MySoundPair(snd);
				soundSet.Add(snd, sp);
			}
			return sp;
        }
        
        public void playSound(string snd, float maxd = 10, float vol = 1) {
        	MyEntity3DSoundEmitter emitter = getOrCreateEmitter(snd);
			MySoundPair sound = getOrCreateSound(snd);
			emitter.CustomMaxDistance = maxd;
			emitter.CustomVolume = vol;
			emitter.PlaySound(sound, true);
			//MyAPIGateway.Utilities.ShowNotification("Playing sound "+snd);
        }
        
        public void stopSound(string snd) {
        	MyEntity3DSoundEmitter play = null;
        	playingSounds.TryGetValue(snd, out play);
        	if (play != null) {
        		play.StopSound(true);
        	}
        }
        
        public void stopAllSounds(bool clearMap = true) {
        	foreach (MyEntity3DSoundEmitter play in playingSounds.Values) {
        		play.StopSound(true);
        	}
        	if (clearMap)
        		playingSounds.Clear();
        }
	}
}