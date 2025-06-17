using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// A component (leaf or folder) of the overview tree
	/// </summary>
	public abstract class OverviewCompo : INotifyPropertyChanged
	{
		public EventCarrier EventCarrier 
		{ 
			get => _event_carrier;
			set
			{
				if (_event_carrier == value)
					return;

				_event_carrier = value;
				OnPropertyChanged(nameof(EventCarrier));
			}
		}
		private EventCarrier _event_carrier;

		public string Name
		{
			get => _name;

			set
			{
				if (_name == value)
					return;

				_name = value;
				OnPropertyChanged(nameof(Name));
			}
		}
		private string _name;

		public OverviewFolder Parent { 
			get => _parent;
			set
			{
				if (_parent == value)
					return;

				_parent = value;
				OnPropertyChanged(nameof(Parent));
			}
		}
		private OverviewFolder _parent = null;

		public int DiscrepancyCount
		{
			get => _discrepancy_count;
			set
			{
				if (_discrepancy_count == value)
					return;
				_discrepancy_count = value;
				Parent?.UpdateDiscrepancyCount();
				OnPropertyChanged(nameof(DiscrepancyCount));
			}
		}
		private int _discrepancy_count = 0;

		public int ChangesPending 
		{ 
			get => _changes_pending;
			set 
			{
				if (_changes_pending == value)
					return;

				_changes_pending = value;
				Parent?.UpdateChangesPending();
				OnPropertyChanged(nameof(ChangesPending));
			}  
		}
		private int _changes_pending = 0;

		/// <summary>
		/// Notify UI elements whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}


	public class OverviewFolder : OverviewCompo
	{
		public ObservableCollection<OverviewCompo> Items 
		{ 
			get => _items;
			set
			{
				if (value == _items)
					return;

				_items = value;
				OnPropertyChanged(nameof(Items));
			}	
		} 
		private ObservableCollection<OverviewCompo> _items = [];

		public OverviewFolder(in string name)
		{
			Name = name;
		}

		public void AddItem(OverviewCompo o)
		{
			Items.Add(o);
			o.Parent = this;
		}

		public void UpdateDiscrepancyCount() 
			=> DiscrepancyCount = Items.Sum(item => item.DiscrepancyCount);
		public void UpdateChangesPending() => ChangesPending = Items.Sum(item => item.ChangesPending);
	}

	public class OverviewRoom(in string desc, in int number) : OverviewFolder(name: desc)
	{
		public int Number
		{
			get => _number;
			set
			{
				if (_number == value)
					return;

				_number = value;
				OnPropertyChanged(nameof(Number));
			}
		}
		int _number = number;
	}

	public class OverviewItem : OverviewCompo
	{
		public string Icon 
		{
			get => _icon;
			set
			{ 
				if (_icon == value) 
					return;

				_icon = value;
				OnPropertyChanged(nameof(Icon));
			}
		}
		private string _icon = null;

		public OverviewItem(EventCarrier carrier, string name, string icon = null)
		{
			EventCarrier = carrier;
			Name = name;
			Icon = icon;
		}
	}

	public class Overview
	{
		public OverviewFolder Root { get; set; }

		public Overview(AgsGame game)
		{
			Root = new("Game");
			OverviewFolder gi = new("Global Items");
			Root.AddItem(gi);
			gi.AddItem(new CharacterTable().Init(game.Tree, game.EventDescs, game.GlobalFunctions));
			gi.AddItem(new OverviewItem(EventCarrier.InvItems, "Inventory events", icon: "☕"));
			gi.AddItem(new OverviewItem(EventCarrier.Guis, "GUI and GUIComponent events", icon: "🖥️"));
			// The stats that have been found during creation of the items haven't been
			// propagated to 'gi' yet because their 'Parent' is only set after creation.
			// So we must update them now.
			gi.UpdateChangesPending();
			gi.UpdateDiscrepancyCount();


			OverviewFolder rooms = new("Rooms");
			Root.AddItem(rooms);
			var xfolder = game.Tree.Root
				.ElementOrThrow("Game")
				.ElementOrThrow("Rooms")
				.ElementOrThrow("UnloadedRoomFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessRoomFolder(xfolder, rooms);

			static void ProcessRoomFolder(in XElement xfolder, in OverviewFolder ov_folder)
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
					ov_folder.AddItem(ov_room);
					ov_room.AddItem(new OverviewItem(EventCarrier.Rooms, "Room events", icon: "🏠"));
					ov_room.AddItem(new OverviewItem(EventCarrier.Objects, "Object events", icon: "🧳"));
					ov_room.AddItem(new OverviewItem(EventCarrier.Hotspots, "Hotspot events", icon: "🔥"));
					ov_room.AddItem(new OverviewItem(EventCarrier.Regions, "Region events", icon: "☁️"));
					// The stats that have been found during creation of the items haven't been
					// propagated to 'ov_room' yet because their 'Parent' is only set after creation.
					// So we must update now.
					ov_room.UpdateChangesPending();
					ov_room.UpdateDiscrepancyCount();
				}
			}
		}
	}

	/// <summary>
	/// A sequence of facts of events.
	/// The specific table line will add key fields that specify to which
	/// entity these events pertain, e.g., the character for character events.
	/// </summary>
	/// <param name="parent">The object that contains the table</param>
	public abstract class TableLine(TableOverviewItem parent) : INotifyPropertyChanged
	{
		public List<EventFacts> Facts 
		{
			get => _facts; 
			set
			{
				if (_facts == value) 
					return;

				_facts = value;
				OnPropertyChanged(nameof(Facts));
			}
		}
		private List<EventFacts> _facts;

		/// <summary>
		/// The object that contains the table for this line
		/// </summary>
		public TableOverviewItem Parent => _parent;
		private readonly TableOverviewItem _parent = parent;

		/// <summary>
		/// The number of table fields that contain changes 
		/// that haven't been written to file yet
		/// </summary>
		public int ChangesPending
		{
			get => _changes_pending;
			set
			{
				if (_changes_pending == value)
					return;

				_changes_pending = value;
				_parent?.UpdateChangesPending();
				OnPropertyChanged(nameof(ChangesPending));
			}
		}
		private int _changes_pending;

		/// <summary>
		/// The number of table fields that contain discrepancies
		/// </summary>
		public int DiscrepancyCount
		{
			get => _discrepancy_count;
			set
			{
				if (value == _discrepancy_count)
					return;

				_discrepancy_count = value;
				_parent?.UpdateDiscrepancyCount();
				OnPropertyChanged(nameof(DiscrepancyCount));
			}
		}
		int _discrepancy_count;

		/// <summary>
		/// Must be called whenever an element of Facts has changes to their field 'HasPendingChanges'
		/// </summary>
		public void UpdateChangesPending() => ChangesPending = Facts.Sum(fact => Convert.ToInt32(fact.HasPendingChanges));

		/// <summary>
		/// Must be called whenever an element of Facts has changes to their field, 'HasDiscrepancy'
		/// </summary>
		public void UpdateDiscrepancyCount() => DiscrepancyCount = Facts.Sum(fact => Convert.ToInt32(fact.HasDiscrepancy));

		/// <summary>
		/// Notify whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
