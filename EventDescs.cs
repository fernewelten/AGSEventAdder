using AgsEventAdder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// Welche Events zu den großen Fallgruppen gehören, 
	/// welche Eigenschaften diese Events haben
	/// </summary>
	public class EventDescs
	{
		private Dictionary<EventType, EventDesc> _mouseModes;
		private Dictionary<EventType, EventDesc> _defaultEvents;

		public List<EventDesc> CharacterEvents { get; private set; }

		public List<EventDesc> GuiEvents { get; private set; }

		public List<EventDesc> GuiButtonEvents { get; private set; }

		public List<EventDesc> GuiListBoxEvents { get; private set; }

		public List<EventDesc> GuiSliderEvents { get; private set; }

		public List<EventDesc> GuiTextBoxEvents { get; private set; }
		
		public List<EventDesc> HotspotEvents { get; private set; }

		public List<EventDesc> InvItemEvents { get; private set; }

		public List<EventDesc> ObjectEvents { get; private set; }

		public List<EventDesc> RegionEvents { get; private set; }

		public List<EventDesc> RoomEvents { get; private set; }



		public EventDescs(XDocument tree)
		{
			InitMouseModes(tree);
			InitDefaultEvents();

			// Note: The sequence of events isn't arbitrary, it must match the
			// sequence used in the AGS editor because event functions are
			// saved to the game file as '<Event Index="…">func_name</Event>',
			// and the 'Index' value must match the index in this list. 
			CharacterEvents =
				[_mouseModes[EventType.LookAt].Copy(),
				_mouseModes[EventType.InteractWith].Copy(),
				_mouseModes[EventType.TalkTo].Copy(),
				_mouseModes[EventType.UseInventoryOn].Copy(),
				_defaultEvents[EventType.AnyClickOn].Copy(),
				_mouseModes[EventType.PickUp].Copy(),
				_mouseModes[EventType.Mode8On].Copy(),
				_mouseModes[EventType.Mode9On].Copy(),
				];
			foreach (var ev in CharacterEvents)
			{ 
				ev.Signature = "(Character *theCharacter, CursorMode mode)";
				ev.ReturnType = "void";
			}

			GuiEvents =
				[_defaultEvents[EventType.OnClick].Copy()];
			foreach (var ev in GuiEvents)
			{
				ev.Signature = "(GUI *theGui, MouseButton button)";
				ev.ReturnType = "void";
			}

			GuiButtonEvents =
				[_defaultEvents[EventType.OnClick].Copy()];
			foreach (var ev in GuiButtonEvents)
			{
				ev.Signature = "(GUIControl *control, MouseButton button)";
				ev.ReturnType = "void";
			}

			GuiListBoxEvents =
				[_defaultEvents[EventType.OnSelectionChanged].Copy()];
			foreach (var ev in GuiListBoxEvents)
			{
				ev.Signature = "(GUIControl *control, MouseButton button)";
				ev.ReturnType = "void";
			}

			GuiSliderEvents =
				[_defaultEvents[EventType.OnChange].Copy()];
			foreach (var ev in GuiSliderEvents)
			{
				ev.Signature = "(GUIControl *control, MouseButton button)";
				ev.ReturnType = "void";
			}

			GuiTextBoxEvents =
				[_defaultEvents[EventType.OnActivate].Copy()];
			foreach (var ev in GuiTextBoxEvents)
			{
				ev.Signature = "(GUIControl *control, MouseButton button)";
				ev.ReturnType = "void";
			}

			HotspotEvents =
				[_mouseModes[EventType.LookAt].Copy(),
				_mouseModes[EventType.InteractWith].Copy(),
				_mouseModes[EventType.TalkTo].Copy(),
				_mouseModes[EventType.UseInventoryOn].Copy(),
				_mouseModes[EventType.PickUp].Copy(),
				_mouseModes[EventType.Mode8On].Copy(),
				_mouseModes[EventType.Mode9On].Copy(),
				_defaultEvents[EventType.AnyClickOn].Copy(),
				_defaultEvents[EventType.WalkOnto].Copy(),
				_defaultEvents[EventType.MouseOver].Copy(),
				];
			foreach (var ev in HotspotEvents)
			{
				ev.Signature = "(Hotspot *theHotspot, CursorMode mode)";
				ev.ReturnType = "void";
			}

			InvItemEvents =
				[_mouseModes[EventType.LookAt].Copy(),
				_mouseModes[EventType.InteractWith].Copy(),
				_mouseModes[EventType.TalkTo].Copy(),
				_mouseModes[EventType.UseInventoryOn].Copy(),
				_defaultEvents[EventType.OtherClickOn].Copy(),
				];
			foreach (var ev in InvItemEvents)
			{
				ev.Signature = "(InvItem *theItem, CursorMode mode)";
				ev.ReturnType = "void";
			}

			ObjectEvents =
				[_mouseModes[EventType.LookAt].Copy(),
				_mouseModes[EventType.InteractWith].Copy(),
				_mouseModes[EventType.TalkTo].Copy(),
				_mouseModes[EventType.UseInventoryOn].Copy(),
				_mouseModes[EventType.PickUp].Copy(),
				_mouseModes[EventType.Mode8On].Copy(),
				_mouseModes[EventType.Mode9On].Copy(),
				_defaultEvents[EventType.AnyClickOn].Copy(),
				];
			foreach (var ev in ObjectEvents)
			{
				ev.Signature = "(Object *theObject, CursorMode mode)";
				ev.ReturnType = "void";
			}

			RegionEvents =
				[_defaultEvents[EventType.StandingOn].Copy(),
				_defaultEvents[EventType.WalkOnto].Copy(),
				_defaultEvents[EventType.WalkOff].Copy(),
				];
			foreach (var ev in RegionEvents)
			{
				ev.Signature = "(Region *theRegion)";
				ev.ReturnType = "void";
			}

			RoomEvents =
				[_defaultEvents[EventType.AfterFadeIn].Copy(),
				_defaultEvents[EventType.Load].Copy(),
				_defaultEvents[EventType.FirstLoad].Copy(),
				_defaultEvents[EventType.Leave].Copy(),
				_defaultEvents[EventType.RepExec].Copy(),
				_defaultEvents[EventType.LeaveBottom].Copy(),
				_defaultEvents[EventType.LeaveLeft].Copy(),
				_defaultEvents[EventType.LeaveRight].Copy(),
				_defaultEvents[EventType.LeaveTop].Copy(),
				];
			foreach (var ev in RoomEvents)
			{
				ev.Signature = "()";
				ev.ReturnType = "void";
			}
		}

		private void InitMouseModes(XDocument tree)
		{
			Dictionary<String, String> name_replacements = new()
			{
				["Use Inv"] = "Use inventory on",
				["Mode8"] = "Usermode 1",
				["Mode9"] = "Usermode 2",
			};
			Dictionary<String, String> ending_replacements = new()
			{
				["LookAt"] = "Look",
				["TalkTo"] = "Talk",
				// With a default cockpit, the modes are called, 
				// "Usermode 1" or "Usermode 2". So this would translate
				// to "_Usermode1" etc., but this is NOT wanted, the
				// default modes should be "Mode8" and "Mode9"
				["Usermode1"] = "Mode8",
				["Usermode2"] = "Mode9"
			};

			XElement folder =
				tree.Root.ElementOrThrow("Game").ElementOrThrow("Cursors");

			_mouseModes = [];
			foreach (var cursor_el in folder.Elements("MouseCursor"))
			{
				var name = cursor_el.ElementOrThrow("Name").Value;
				// The default ending is the name with each word capitalized,
				// where spaces are deleted
				var name_parts = name.Split(' ');
				string ending = "";
				foreach (var name_part in name_parts)
					if (!string.IsNullOrEmpty(name_part))
						ending +=
							name_part[0].ToString().ToUpper() +
							name_part[1..];

				var id = (EventType)cursor_el.IntElementOrThrow("ID");
				if (string.IsNullOrEmpty(ending))
					ending = "Mode" + ((int)id).ToString();

				if (name_replacements.TryGetValue(name, out string? name_value))
					name = name_value;
				if (ending_replacements.TryGetValue(ending, out string? ending_value))
					ending = ending_value;

				_mouseModes.Add(
					id, 
					new EventDesc()
					{
						Type = id,
						Name = name,
						Ending = ending,
					});
			}
		}

		private void InitDefaultEvents()
		{
			_defaultEvents = new()
			{
				[EventType.AfterFadeIn] = new EventDesc()
					{ Name = "Enters room after fade-in", Ending = "AfterFadeIn", },
				[EventType.AnyClickOn] = new EventDesc()
					{ Name = "Any click on", Ending = "AnyClick", },
				[EventType.ClickOn] = new EventDesc()
					{ Name = "Click on", Ending = "Click", },
				[EventType.FirstLoad] = new EventDesc()
					{ Name = "First time enters room", Ending = "FirstLoad", },
				[EventType.Leave] = new EventDesc()
					{ Name = "Leaves room", Ending = "Leave", },
				[EventType.LeaveBottom] = new EventDesc()
					{ Name = "Walks off bottom edge", Ending = "LeaveBottom" },
				[EventType.LeaveLeft] = new EventDesc()
					{ Name = "Walks off left edge", Ending = "LeaveLeft" },
				[EventType.LeaveRight] = new EventDesc()
					{ Name = "Walks off right edge", Ending = "LeaveRight" },
				[EventType.LeaveTop] = new EventDesc()
					{ Name = "Walks off top edge", Ending = "LeaveTop" },
				[EventType.Load] = new EventDesc()
					{ Name = "Enters room before fade-in", Ending = "Load" },
				[EventType.MouseOver] = new EventDesc()
					{ Name = "Mouse moves over", Ending = "MouseMove" },
				[EventType.OnActivate] = new EventDesc()
					{ Name = "When activated", Ending = "OnActivate" },
				[EventType.OnChange] = new EventDesc()
					{ Name = "When changed", Ending = "OnChange" },
				[EventType.OnClick] = new EventDesc()
					{ Name = "When clicked on", Ending = "OnClick" },
				[EventType.OnSelectionChanged] = new EventDesc()
					{ Name = "When selection changes", Ending = "OnSelectionChanged" },
				[EventType.OtherClickOn] = new EventDesc()
					{ Name = "Other click on", Ending = "OtherClick" },
				[EventType.RepExec] = new EventDesc()
					{ Name = "Repeatedly execute", Ending = "RepExec" },
				[EventType.StandingOn] = new EventDesc()
					{ Name = "While standing on", Ending = "Standing" },
				[EventType.WalkOff] = new EventDesc()
					{ Name = "Walk off", Ending = "WalksOff" },
				[EventType.WalkOnto] = new EventDesc()
					{ Name = "Walk onto", Ending = "WalksOnto" },
			};
			foreach (var ev in _defaultEvents)
				ev.Value.Type = ev.Key;
		}
	}
}
