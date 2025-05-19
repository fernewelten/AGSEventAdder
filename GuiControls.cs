using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace AgsEventAdder
{
	internal class GuiControls
	{
		/// <summary>
		/// All the functions in GlobalScript that have been defined with body
		/// </summary>
		private readonly HashSet<string> _functions;

		/// <summary>
		/// Guis/Guicontrols for use in the application
		/// </summary>
		public List<GuiControl> List { get; set; }

		public GuiControls(in XDocument tree, in HashSet<string> functions)
		{
			_functions = functions;

			XElement folder =
				tree.Root.ElementOrThrow("Game").ElementOrThrow("GUIs").ElementOrThrow("GUIFolder");

			var subfolders = folder.ElementOrThrow("SubFolders").Elements();
			foreach (var subfolder in subfolders)
				InitFolder(
					prefix: "",
					lead_in: "",
					folder: subfolder);

			var gui_elements = folder.ElementOrThrow("GUIs").Elements();
			foreach (var gui_el in gui_elements)
				InitGui(
					prefix: "",
					lead_in: "",
					gui_el: gui_el);
		}

		private void InitFolder(in string prefix, in string lead_in, in XElement folder)
		{
			GuiControl gc = new()
			{
				Nesting = prefix + lead_in,
				GuiControlName = folder.Attribute("GuiControlName")?.Value ?? "",
				Type = GuiControlType.Folder,
			};
			List.Add(gc);

			var sf = folder.ElementOrThrow("SubFolders");
			var subfolder_elements = sf.Elements();
			var subfolder_count = subfolder_elements.Count();

			var ge = folder.ElementOrThrow("GUIs");
			var gui_elements = ge.Elements();
			var gui_count = gui_elements.Count();
			int last_idx = subfolder_count + gui_count - 1;

			int idx = 0;
			string new_prefix = (lead_in == "") ? "" : NestingStrings.TopDown;
			foreach (var sf_el in subfolder_elements)
			{
				string new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitFolder(
					prefix: new_prefix,
					lead_in: new_lead_in,
					folder: sf_el);
				idx++;
			}

			foreach(var gui_el in gui_elements)
			{
				string new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;

				InitGui(
					prefix: new_prefix,
					lead_in: new_lead_in,
					gui_el: gui_el);
				idx++;
			}
		}
		
		private void InitGui(in string prefix, in string lead_in, in XElement gui_el)
		{
			var main_el = gui_el.ElementOrThrow("GUIMain");

			var ng_el = main_el.Element("NormalGui");
			if (ng_el is null)
				return; // Don't process TextGuis: They don't have events

			var name_el = ng_el.ElementOrThrow("Name");
			var gci = ng_el.IntElementOrThrow("ID");
			var on_click_el = ng_el.ElementOrThrow("OnClick");
			var roster_entry = on_click_el.Value;

			GuiControl gui = new()
			{
				Nesting = prefix + lead_in,
				GuiControlName = name_el.Value,
				GuiControlId = gci,
				Type = GuiControlType.Gui,
				EventName = "OnClick", 
				EventType = EventType.OnClick,
				CurrentInRoster = roster_entry, 
				NewInRoster = roster_entry,
				IsInCode = _functions.Contains(roster_entry),
				AddStubToCode = false,
			};
			List.Add(gui);

			var gc_elements = gui_el.ElementOrThrow("Controls").Elements();
			int idx = 0, last_idx = gc_elements.Count() - 1;
			foreach (var gc_el in gc_elements)
			{
				string new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitGuiControl(
					prefix: prefix,
					lead_in: new_lead_in,
					gc_el: gc_el);
				idx++;
			}
		}

		private void InitGuiControl(in string prefix, in string lead_in, in XElement gc_el)
		{
			var gci = gc_el.IntElementOrThrow("ID");
			var name_el = gc_el.ElementOrThrow("Name");

			GuiControl gc = new()
			{
				Nesting = prefix + lead_in,
				GuiControlName = name_el.Value,
				GuiControlId = gci,
				AddStubToCode = false,
			};

			if (gc.GuiControlName == "GUIButton")
			{
				gc.Type = GuiControlType.Button;
				gc.EventType = EventType.OnClick;
			}
			else if (gc.GuiControlName == "GUIListBox")
			{
				gc.Type = GuiControlType.ListBox;
				gc.EventType = EventType.OnSelectionChanged;
			}
			else if (gc.GuiControlName == "GUISlider")
			{
				gc.Type = GuiControlType.Slider;
				gc.EventType = EventType.OnChange;
			}
			else if (gc.GuiControlName == "GUITextBox")
			{
				gc.Type = GuiControlType.TextBox;
				gc.EventType = EventType.OnActivate;
			}
			else
			{
				// Others don't have an event
				return;
			}

			gc.EventName = gc.EventType.ToString("G");
			var event_el = gc_el.ElementOrThrow(gc.EventName);
			var roster_entry = event_el.Value;
			gc.CurrentInRoster = roster_entry;
			gc.NewInRoster = roster_entry;
			gc.IsInCode = _functions.Contains(roster_entry);
			List.Add(gc);
		}
	}
}
