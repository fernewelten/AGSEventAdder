using AgsEventAdder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Xml.Linq;
using System.Globalization;
using System.Windows.Controls;
using System.Management.Instrumentation;
using System.ComponentModel;

namespace AgsEventAdder
{
	internal class Events
	{
		private Dictionary<EventId, EventDesc> _mouseModes;
		private Dictionary<EventId, EventDesc> _defaultEvents;

		public List<EventDesc> CharacterEvents { get; private set; }
		public List<EventDesc> GuiControlEvents { get; private set; }
		public List<EventDesc> GuiEvents { get; private set; }
		public List<EventDesc> InvItemEvents { get; private set; }

		public List<EventDesc> HotspotEvents { get; private set; }

		public List<EventDesc> ObjectEvents { get; private set; }

		public List<EventDesc> RegionEvents { get; private set; }

		public Events(in XDocument tree)
		{
			XElement folder =
				tree.Root.Element("Game").Element("Cursors");

			InitMouseModes(folder);
			InitDefaultEvents();

			CharacterEvents =
				[_mouseModes[EventId.LookAt].Copy(),
				_mouseModes[EventId.InteractWith].Copy(),
				_mouseModes[EventId.TalkTo].Copy(),
				_mouseModes[EventId.UseInventoryOn].Copy(),
				_mouseModes[EventId.PickUp].Copy(),
				_defaultEvents[EventId.AnyClickOn].Copy(),
				_mouseModes[EventId.Mode8On].Copy(),
				_mouseModes[EventId.Mode9On].Copy(),
				];
			foreach (var ev in CharacterEvents)
			{
				ev.Description += " {char}";
				ev.Signature = "()";
			}

			GuiControlEvents = 
				[_defaultEvents[EventId.ClickOn].Copy(),];
			foreach (var ev in GuiControlEvents)
			{
				ev.Description += " {control}";
				ev.Signature = "()";
			}

			GuiEvents = 
				[_defaultEvents[EventId.ClickOn].Copy(),];
			foreach (var ev in GuiEvents)
			{
				ev.Description += " {gui}";
				ev.Signature = "()";
			}

			HotspotEvents =
				[_mouseModes[EventId.LookAt].Copy(),
				_mouseModes[EventId.InteractWith].Copy(),
				_mouseModes[EventId.TalkTo].Copy(),
				_mouseModes[EventId.UseInventoryOn].Copy(),
				_defaultEvents[EventId.AnyClickOn].Copy(),
				_mouseModes[EventId.PickUp].Copy(),
				_mouseModes[EventId.Mode8On].Copy(),
				_mouseModes[EventId.Mode9On].Copy(),
				_defaultEvents[EventId.WalkOnto].Copy(),
				_defaultEvents[EventId.MouseOver].Copy(),
				];
			foreach (var ev in HotspotEvents)
			{
				ev.Description += " {Hotspot}";
				ev.Signature = "()";
			}

			InvItemEvents =
				[_mouseModes[EventId.LookAt].Copy(),
				_mouseModes[EventId.InteractWith].Copy(),
				_mouseModes[EventId.TalkTo].Copy(),
				_mouseModes[EventId.UseInventoryOn].Copy(),
				_defaultEvents[EventId.OtherClickOn].Copy(),
				];
			foreach (var ev in InvItemEvents)
			{
				ev.Description += " {Iitem}";
				ev.Signature = "()";
			}

			ObjectEvents =
				[_mouseModes[EventId.LookAt].Copy(),
				_mouseModes[EventId.InteractWith].Copy(),
				_mouseModes[EventId.TalkTo].Copy(),
				_mouseModes[EventId.UseInventoryOn].Copy(),
				_defaultEvents[EventId.AnyClickOn].Copy(),
				_mouseModes[EventId.PickUp].Copy(),
				_mouseModes[EventId.Mode8On].Copy(),
				_mouseModes[EventId.Mode9On].Copy(),
				];
			foreach (var ev in ObjectEvents)
			{
				ev.Description += " {Object}";
				ev.Signature = "()";
			}

			RegionEvents =
				[_defaultEvents[EventId.StandingOn].Copy(),
				_defaultEvents[EventId.WalkOnto].Copy(),
				_defaultEvents[EventId.WalkOff].Copy(),
				];
			foreach (var ev in RegionEvents)
			{
				ev.Description += " {Region}";
				ev.Signature = "()";
			}
		}

		private void InitMouseModes(in XElement folder)
		{
			Dictionary<String, String> name_replacements = new()
			{
				["Use Inv"] = "Use inventory on",
			};
			Dictionary<String, String> ending_replacements = new()
			{
				["LookAt"] = "Look",
				["TalkTo"] = "Talk",
				["Usermode1"] = "Mode8",
				["Usermode2"] = "Mode9",
			};

			_mouseModes = [];
			foreach (var cursor_el in folder.Elements())
			{
				var mouse_cursor_el = cursor_el.Element("MouseCursor");
				EventId id = (EventId)int.Parse(cursor_el.Element("ID").Value);
				var name = mouse_cursor_el.Element("Name").Value;
				// The default ending is the name with each word capitalized,
				// where spaces are deleted
				var name_parts = name.Split(' ');
				string ending = "";
				foreach (var name_part in name_parts)
					if (!string.IsNullOrEmpty(name_part))
						ending +=
							name_part[0].ToString().ToUpper() +
							name_part.Substring(1);

				if (string.IsNullOrEmpty(ending))
					ending = "Mode" + ((int)id).ToString();

				if (name_replacements.ContainsKey(name))
					name = name_replacements[name];
				if (ending_replacements.ContainsKey(ending))
					ending = ending_replacements[ending];

				_mouseModes.Add(id, new EventDesc()
				{
					Id = id,
					Ending = ending,
					Description = name,
				});
			}
		}

		private void InitDefaultEvents()
		{
			_defaultEvents = new()
			{
				[EventId.AnyClickOn] = new EventDesc()
					{ Description = "Any click on", Ending = "AnyClick" },
				[EventId.ClickOn] = new EventDesc()
					{ Description = "Click on", Ending = "Click" },
				[EventId.MouseOver] = new EventDesc()
					{ Description = "Mouse moves over", Ending = "MouseMove" },
				[EventId.OtherClickOn] = new EventDesc()
					{ Description = "Other click on", Ending = "OtherClick" },
				[EventId.StandingOn] = new EventDesc()
					{ Description = "While standing on", Ending = "Standing" },
				[EventId.WalkOff] = new EventDesc()
					{ Description = "Walk off", Ending = "WalksOff" },
				[EventId.WalkOnto] = new EventDesc()
					{ Description = "Walk onto", Ending = "WalksOnto" },
			};
			foreach (var ev in _defaultEvents)
				ev.Value.Id = ev.Key;
		}
		
	}
}
