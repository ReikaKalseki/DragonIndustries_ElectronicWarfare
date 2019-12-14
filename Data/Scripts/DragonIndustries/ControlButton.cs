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
	
	public static class ControlFuncs {	
	
		public static bool isOptionApplicable(IMyTerminalBlock block, IMyTerminalControl control, LogicCore caller) {
	        var lgc = block.GameLogic.GetAs<LogicCore>();
	        string type = lgc != null ? lgc.GetType().ToString() : null;
	        string seek = getBlockTypeFilter(control);
	        //if (seek != null)
	       // MyAPIGateway.Utilities.ShowNotification(block.CustomName+" check "+control.Id+" > looking for '"+seek+"', have '"+type);
	        return seek == null || type == seek;
		}
	        
	    private static string getBlockTypeFilter(IMyTerminalControl control) {
	    	return control.Id.Contains("[BLOCKFILTER=") ? control.Id.Replace("BLOCKFILTER=", "").Split('[', ']')[1] : null;
	    }
	}
	
	public abstract class Control<T, S, B> where T: IMyTerminalBlock where S: LogicCore where B: IMyTerminalControl { //WHY C#, WHY?!
        
		public readonly string id;
		public readonly string displayName;
		public readonly string tooltip;
		
		protected B button;
		protected IMyTerminalAction hotbar;
  
        protected Control(LogicCore type, string name, string label, string tip) {
			id = "[BLOCKFILTER="+type.GetType().ToString()+"]"+name;
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
	public class ControlButton<T, S> : Control<T, S, IMyTerminalControlButton> where T: IMyTerminalBlock where S: LogicCore {
		
		private readonly Action<IMyTerminalBlock> effect;
         
        public ControlButton(LogicCore type, string name, string label, string tip, Action<IMyTerminalBlock> ef) : base(type, name, label, tip) {
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
	public class ToggleButton<T, S> : Control<T, S, IMyTerminalControlCheckbox> where T: IMyTerminalBlock where S: LogicCore {
		
		private readonly Func<IMyTerminalBlock, bool> getCurrentValue;
		private readonly Action<IMyTerminalBlock, bool> setValue;
		
		public ToggleButton(LogicCore type, string name, string label, string tip, Func<IMyTerminalBlock, bool> cv, Action<IMyTerminalBlock, bool> set) : base(type, name, label, tip) {
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
	
	public class OnOffButton<T, S> : Control<T, S, IMyTerminalControlOnOffSwitch> where T: IMyTerminalBlock where S: LogicCore {
		
		private readonly Func<IMyTerminalBlock, bool> getCurrentValue;
		private readonly Action<IMyTerminalBlock, bool> setValue;
		
		public OnOffButton(LogicCore type, string name, string label, string tip, Func<IMyTerminalBlock, bool> cv, Action<IMyTerminalBlock, bool> set) : base(type, name, label, tip) {
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
	public class Slider<T, S> : Control<T, S, IMyTerminalControlSlider> where T: IMyTerminalBlock where S: LogicCore {
		
		private readonly Func<IMyTerminalBlock, float> getCurrentValue;
		private readonly Action<IMyTerminalBlock, float> setValue;
		
		private readonly Action<IMyTerminalBlock, StringBuilder> displayText;
		
		private readonly float minValue;
		private readonly float maxValue;
		
		public Slider(LogicCore type, string name, string label, string tip, float min, float max, Func<IMyTerminalBlock, float> cv, Action<IMyTerminalBlock, float> set, Action<IMyTerminalBlock, StringBuilder> disp) : base(type, name, label, tip) {
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