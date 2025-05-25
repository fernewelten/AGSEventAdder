using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// A component (leaf or folder) of the overview tree
	/// </summary>
	public abstract class OverviewCompo
	{
		public EventCarrier EventCarrier { get; set; }
		public string Name { get; set; }
		public OverviewCompo Parent { get; set; } = null;

		/// <summary>
		/// Sum of all discrepancies of direct or indirect items
		/// </summary>
		public int TotalDiscrepancyCount { get; protected set; } = 0;

		private int _discrepancy_count = 0;
		public int DiscrepancyCount
		{
			get => _discrepancy_count;
			set
			{
				int diff = value - _discrepancy_count;
				_discrepancy_count = value;
				for (OverviewCompo here = this; here != null; here = here.Parent)
					here.TotalDiscrepancyCount += diff;
			}
		}
			
		public bool UnsavedChanges { get; set; } = false;

		public void AddDiscrepancy(in int count = 1) => DiscrepancyCount += count;
	}


	public class OverviewFolder : OverviewCompo
	{
		public ObservableCollection<OverviewCompo> Items { get; set; } = [];

		public OverviewFolder(in string name)
		{
			Name = name;
		}

		public void AddItem(in OverviewCompo o)
		{
			Items.Add(o);
			o.Parent = this;
		}
	}

	public class OverviewRoom : OverviewFolder
	{
		public int Number { get; set; }

		public OverviewRoom(in string desc, in int number)
			: base(name: desc) => Number = number;
	}

	public class OverviewItem : OverviewCompo
	{
		public string Icon { get; set; } = null;

		public OverviewItem(in EventCarrier c, in string name, in string icon = null)
		{
			EventCarrier = c;
			Name = name;
			Icon = icon;
		}
	}

	public class Overview
	{

		public OverviewFolder Root { get; set; }

		public Overview(in XDocument tree)
		{
			Root = new("Game");
			OverviewFolder gi = new("Global Items");
			gi.AddItem(new OverviewItem(EventCarrier.Characters, "Character events", icon: "🧑"));
			gi.AddItem(new OverviewItem(EventCarrier.InvItems, "Inventory events", icon: "☕"));
			gi.AddItem(new OverviewItem(EventCarrier.Guis, "GUI and GUIComponent events", icon: "🖥️"));
			Root.AddItem(gi);

			OverviewFolder rooms = new("Rooms");

			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("Rooms")
				.ElementOrThrow("UnloadedRoomFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessRoomFolder(xfolder, rooms);
			Root.AddItem(rooms);
		}

		private void ProcessRoomFolder(in XElement xfolder, in OverviewFolder ov_folder)
		{
			var xsubfolders = xfolder.ElementOrThrow("SubFolders");
			foreach (XElement xsubfolder in xsubfolders.Elements())
			{
				if (xsubfolder.Name != "UnloadedRoomFolder")
					throw new AgsXmlParsingException(
						$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
						xsubfolder);
				OverviewFolder ov_subf = new(xsubfolder.Attribute("Name").Value);
				ov_folder.AddItem(ov_subf);
				ProcessRoomFolder(xsubfolder, ov_subf);
			}

			var xrooms = xfolder.ElementOrThrow("UnloadedRooms");
			foreach (XElement xroom in xrooms.Elements())
			{
				if (xroom.Name != "UnloadedRoom")
					throw new AgsXmlParsingException(
						$"'Found unexpected sub-element <{xroom.Name}>' within <UnloadedRooms>",
						xroom);
				int number = xroom.IntElementOrThrow("Number");
				XElement desc_el = xroom.ElementOrThrow("Description");
				string desc = desc_el.Value;
				if (string.IsNullOrWhiteSpace(desc))
					desc = "((No description))";

				OverviewRoom ov_room = new(number: number, desc: desc);
				ov_room.AddItem(new OverviewItem(EventCarrier.Rooms, "Room events", icon: "🏠"));
				ov_room.AddItem(new OverviewItem(EventCarrier.Objects, "Object events", icon: "🧳"));
				ov_room.AddItem(new OverviewItem(EventCarrier.Hotspots, "Hotspot events", icon: "🔥"));
				ov_room.AddItem(new OverviewItem(EventCarrier.Regions, "Region events", icon: "☁️"));
				ov_folder.AddItem(ov_room);
			}
		}
	}
}
