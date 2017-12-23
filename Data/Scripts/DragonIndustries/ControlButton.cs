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
using VRage.Utils;

using System.IO;

using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;

using MyEntity = VRage.Game.Entity.MyEntity;

using Sandbox.ModAPI.Interfaces.Terminal;

using MyObjectBuilder_UpgradeModule = Sandbox.Common.ObjectBuilders.MyObjectBuilder_UpgradeModule;

namespace DragonIndustries {
	
	public abstract class Control<T, B> where T: IMyTerminalBlock where B: IMyTerminalControl { //WHY C#, WHY?!
        
		public readonly string id;
		public readonly string displayName;
		public readonly string tooltip;
		
		protected B button;
		protected IMyTerminalAction hotbar;
  
        protected Control(string name, string label, string tip) {
			id = name;
			displayName = label;
			tooltip = tip;
        }
		
		public void register() {
        	button = MyAPIGateway.TerminalControls.CreateControl<B, T>(id);
        	
        	hotbar = MyAPIGateway.TerminalControls.CreateAction<T>(id);
        	hotbar.Name = new StringBuilder().Append(displayName);
        	
        	populate();
        	
        	MyAPIGateway.TerminalControls.AddAction<T>(hotbar);
        	MyAPIGateway.TerminalControls.AddControl<T>(button);
		}
		
		protected virtual void populate() {
			
		}
	}
	
	/** Single-button text-label type */
	public class ControlButton<T> : Control<T, IMyTerminalControlButton> where T: IMyTerminalBlock {
		
		private readonly Action<IMyTerminalBlock> effect;
         
        public ControlButton(string name, string label, string tip, Action<IMyTerminalBlock> ef) : base(name, label, tip) {
			effect = ef;
        }
		
		protected override void populate() {
			button.Action = effect;
        	button.Title = MyStringId.GetOrCompute(displayName);
        	button.Tooltip = MyStringId.GetOrCompute(tooltip);
        	
        	hotbar.Action = effect;
		}
	}
	
	/** Radio-button-like appearance, checkbox like behavior */
	public class ToggleButton<T> : Control<T, IMyTerminalControlCheckbox> where T: IMyTerminalBlock {
		
		private readonly Func<IMyTerminalBlock, bool> getCurrentValue;
		private readonly Action<IMyTerminalBlock, bool> setValue;
		
		public ToggleButton(string name, string label, string tip, Func<IMyTerminalBlock, bool> cv, Action<IMyTerminalBlock, bool> set) : base(name, label, tip) {
			getCurrentValue = cv;
			setValue = set;
		}
		
		protected override void populate() {
			button.Getter = getCurrentValue;
			button.Setter = setValue;
			
        	button.Title = MyStringId.GetOrCompute(displayName);
        	button.Tooltip = MyStringId.GetOrCompute(tooltip);
		}
		
	}
	
	public class OnOffButton<T> : Control<T, IMyTerminalControlOnOffSwitch> where T: IMyTerminalBlock {
		
		private readonly Func<IMyTerminalBlock, bool> getCurrentValue;
		private readonly Action<IMyTerminalBlock, bool> setValue;
		
		public OnOffButton(string name, string label, string tip, Func<IMyTerminalBlock, bool> cv, Action<IMyTerminalBlock, bool> set) : base(name, label, tip) {
			getCurrentValue = cv;
			setValue = set;
		}
		
		protected override void populate() {
			button.Getter = getCurrentValue;
			button.Setter = setValue;
			
        	button.Title = MyStringId.GetOrCompute(displayName);
        	button.Tooltip = MyStringId.GetOrCompute(tooltip);
        	
        	button.OffText = MyStringId.GetOrCompute("Off");
        	button.OnText = MyStringId.GetOrCompute("On");
		}
		
	}
	
	/** Radio-button-like appearance, checkbox like behavior */
	public class Slider<T> : Control<T, IMyTerminalControlSlider> where T: IMyTerminalBlock {
		
		private readonly Func<IMyTerminalBlock, float> getCurrentValue;
		private readonly Action<IMyTerminalBlock, float> setValue;
		
		private readonly Action<IMyTerminalBlock, StringBuilder> displayText;
		
		private readonly float minValue;
		private readonly float maxValue;
		
		public Slider(string name, string label, string tip, float min, float max, Func<IMyTerminalBlock, float> cv, Action<IMyTerminalBlock, float> set, Action<IMyTerminalBlock, StringBuilder> disp) : base(name, label, tip) {
			getCurrentValue = cv;
			setValue = set;
			
			displayText = disp;
			
			minValue = min;
			maxValue = max;
		}
		
		protected override void populate() {
			button.Getter = getCurrentValue;
			button.Setter = setValue;
			
			button.SetLimits(minValue, maxValue);
			button.Writer = displayText;
			
        	button.Title = MyStringId.GetOrCompute(displayName);
        	button.Tooltip = MyStringId.GetOrCompute(tooltip);
		}
		
	}
}