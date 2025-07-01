using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// Event facts for Characters
	/// </summary>
	/// <param name="parent">The object that contains the table for this line</param>
	public class CharacterTableLine(TableOverviewItem parent) : TableLine(parent)
	{
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
		private string _name = string.Empty;

		public int Id
		{
			get => _id;

			set
			{ 
				if (value == _id) 
					return;

				_id = value;
				OnPropertyChanged(nameof(Id));
			}
		}
		private int _id;
	}

	/// <summary>
	/// Event facts for Inventory Items
	/// </summary>
	/// <param name="parent">The object that contains the table for this line</param>
	public class InvItemTableLine(TableOverviewItem parent) : TableLine(parent)
	{
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
		private string _name = string.Empty;

		public int Id
		{
			get => _id;

			set
			{ 
				if (value == _id) 
					return;

				_id = value;
				OnPropertyChanged(nameof(Id));
			}
		}
		private int _id;
	}

	/// <summary>
	/// Overview items that contain a table of a list of events per entity
	/// </summary>
	public abstract class TableOverviewItem(EventCarrier carrier, string name, string icon)
		: OverviewItem(carrier, name, icon)
	{
		public abstract void UpdateDiscrepancyCount();

		public abstract void UpdateChangesPending();

		public abstract IList Lines { get; }

		protected List<EventDesc> EventDescs { get; set; }

		// This field won't be displayed anywhere, no need to notify changes
		/// <summary>
		/// The code file associated with the table. Stubs will be written here
		/// </summary>
		protected string CodeFilePath { get; set; }

		// This field won't be displayed anywhere, no need to notify changes
		/// <summary>
		/// The names of the functions in the code file.
		/// </summary>
		protected HashSet<string> Functions { get; set; }

		public void InsertDefaultWhereverEmpty(IList list, int fact_col, bool add_to_code = false)
		{
			if (list is null ||  list.Count == 0)
				list = Lines;
			foreach (var item in list)
			{
				if (item is null || item is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				var facts = tline.Facts[fact_col];
				if (facts is null || !string.IsNullOrWhiteSpace(facts.NewInRoster))
					continue;

				facts.ChangeToDefault();
				if (add_to_code)
					facts.MustAddStubToCode = true;
			}
		}
		
		public void AddEventsToCodeWheneverMissing(IList list, int fact_col)
		{
			if (list is null || list.Count == 0)
				list = Lines;
			foreach (var item in list)
			{
				if (item is null || item is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				var facts = tline.Facts[fact_col];
				if (string.IsNullOrWhiteSpace(facts?.NewInRoster))
					continue;

				facts.MustAddStubToCode = !facts.NewIsInCode;
			}
		}


		public void ClearEvents(IList list, int fact_col, bool only_those_not_in_code)
		{
			if (list is null || list.Count == 0)
				list = Lines;
			foreach (var item in list)
			{
				if (item is null || item is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				var facts = tline.Facts[fact_col];
				if (string.IsNullOrWhiteSpace(facts?.NewInRoster))
					continue;

				if (!only_those_not_in_code || !facts.NewIsInCode)
					facts.NewInRoster = "";
			}
		}

		
		public void CancelAllPendingChanges(IList list, int fact_col)
		{
			if (list is null || list.Count == 0)
				list = Lines;
			foreach (var line in list)
			{
				if (line is null || line is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				var facts = tline.Facts[fact_col];
				facts?.DiscardPendingChanges();
			}
		}

		public void DiscardPendingChanges()
		{
			if (ChangesPending == 0)
				return; // nothing to do

			foreach (var line in Lines)
			{
				if (line is null || line is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				foreach (var facts in tline.Facts)
					facts?.DiscardPendingChanges();
			}
		}
		public void UpdateToTreeWhenPending()
		{
			if (ChangesPending == 0)
				return; // nothing to do

			foreach (var line in Lines)
			{
				if (line is null || line is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				foreach (var facts in tline.Facts)
					facts?.UpdateTreeElementWhenPending();
			}
		}

		public void UpdateCodeWhenPending()
		{
			if (ChangesPending == 0)
				return; // nothing to do

			using StreamWriter code_writer = new(path: CodeFilePath, append: true);

			foreach (var line in Lines)
			{
				if (line is null || line is not TableLine tline)
					continue;
				if (tline.IsFolder)
					continue;

				for (int idx = 0; idx < tline.Facts.Count; idx++)
				{
					EventDesc desc = EventDescs[idx];
					EventFacts facts = tline.Facts[idx];
					facts?.UpdateCodeWhenPending(code_writer, desc, Functions);
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

		public bool IsFolder
		{
			get => _isFolder;
			set
			{
				if (_isFolder == value)
					return;

				_isFolder = value;
				OnPropertyChanged(nameof(IsFolder));
			}
		}
		private bool _isFolder;

		public int Indent
		{
			get => _indent;
			set
			{
				if (_indent == value)
					return;

				_indent = value;
				OnPropertyChanged(nameof(Indent));
			}
		}
		private int _indent;

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

	public class CharacterTable : TableOverviewItem
	{
		public override IList Lines => _lines;
		public ObservableCollection<CharacterTableLine> ChLines => _lines;
		private ObservableCollection<CharacterTableLine> _lines = [];

		public CharacterTable(EventDescs descs, string code_file, HashSet<string> functions)
			: base(carrier: EventCarrier.Characters, name: "Character events", icon: "🧑‍🤝‍🧑")
		{
			EventDescs = descs.CharacterEvents;
			CodeFilePath = code_file;
			Functions = functions;
		}

		public CharacterTable Init(XDocument tree)
		{
			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("Characters")
				.ElementOrThrow("CharacterFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessInvItemFolder(xfolder: xfolder, indent: 0);
			return this;

			void ProcessInvItemFolder(XElement xfolder, int indent)
			{
				var xsubfolders = xfolder.ElementOrThrow("SubFolders");
				int subfolder_indent = indent + 1;
				foreach (XElement xsubfolder in xsubfolders.Elements())
				{
					if (xsubfolder.Name != "CharacterFolder")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
							xsubfolder);
					CharacterTableLine ctl = new(this)
					{
						Facts = [],
						Id = -77,
						Indent = indent,
						IsFolder = true,
						Name = xsubfolder.Attribute("Name").Value,
					};
					Lines.Add(ctl);
					ProcessInvItemFolder(xsubfolder, subfolder_indent);
				}

				var xcharacters = xfolder.ElementOrThrow("Characters");
				foreach (XElement xcharacter in xcharacters.Elements())
				{
					if (xcharacter.Name != "Character")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xcharacter.Name}>' within <Characters>",
							xcharacter);
					int id = xcharacter.IntElementOrThrow("ID");
					XElement script_name_el = xcharacter.ElementOrThrow("ScriptName");
					string name = script_name_el.Value;
					if (string.IsNullOrWhiteSpace(name))
						name = "";

					CharacterTableLine ctl = new(this) 
					{ 
						Name = name,
						Id = id, 
						Facts = [],
						Indent = indent,
					};
					Lines.Add(ctl);
					var xinteractions = xcharacter.ElementOrThrow("Interactions");
					foreach (XElement xevent in xinteractions.Elements())
					{
						var index = xevent.IntAttributeValueOrThrow("Index");

						while (ctl.Facts.Count <= index)
						{
							var ev = new EventFacts(parent: ctl, functions: Functions);
							ctl.Facts.Add(ev);
						}
						ctl.Facts[index].NewInRoster = ctl.Facts[index].CurrentInRoster = xevent.Value;
						ctl.Facts[index].DefaultName = (name == "") ? "" : $"{name}_{EventDescs[index].Ending}";
						ctl.Facts[index].TreeElement = xevent;
					}
				}
			}
		}

		public override void UpdateDiscrepancyCount() =>
			DiscrepancyCount = ChLines.Sum(line => line.DiscrepancyCount);

		public override void UpdateChangesPending() =>
			ChangesPending = ChLines.Sum(line => line.ChangesPending);
	}


	public class InvItemTable : TableOverviewItem
	{
		public override IList Lines => _lines;
		public ObservableCollection<InvItemTableLine> IiLines => _lines;
		private ObservableCollection<InvItemTableLine> _lines = [];

		public InvItemTable(EventDescs descs, string code_file, HashSet<string> functions)
			: base(carrier: EventCarrier.InvItems, name: "InvItem events", icon: "🗝️")
		{
			EventDescs = descs.InvItemEvents;
			CodeFilePath = code_file;
			Functions = functions;
		}

		public InvItemTable Init(XDocument tree)
		{
			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("InventoryItems")
				.ElementOrThrow("InventoryItemFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessFolder(xfolder, 0);
			return this;

			void ProcessFolder(XElement xfolder, int indent)
			{
				var xsubfolders = xfolder.ElementOrThrow("SubFolders");
				int subfolder_indent = indent + 1;
				foreach (XElement xsubfolder in xsubfolders.Elements())
				{
					if (xsubfolder.Name != "InventoryItemFolder")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
							xsubfolder);

					InvItemTableLine ctl = new(this)
					{
						Facts = [],
						Id = -77,
						Indent = indent,
						IsFolder = true,
						Name = xsubfolder.Attribute("Name").Value,
					};
					Lines.Add(ctl);
					ProcessFolder(xsubfolder, subfolder_indent);
				}

				var xitems = xfolder.ElementOrThrow("InventoryItems");
				foreach (XElement xitem in xitems.Elements())
				{
					if (xitem.Name != "InventoryItem")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xitem.Name}>' within <InventoryItems>",
							xitem);
					int id = xitem.IntElementOrThrow("ID");
					XElement item_name_el = xitem.ElementOrThrow("Name");
					string name = item_name_el.Value;
					if (string.IsNullOrWhiteSpace(name))
						name = "";

					InvItemTableLine ctl = new(this)
					{
						Name = name,
						Id = id,
						Facts = [],
						Indent = indent,
					};
					Lines.Add(ctl);
					var xinteractions = xitem.ElementOrThrow("Interactions");
					foreach (XElement xevent in xinteractions.Elements())
					{
						var index = xevent.IntAttributeValueOrThrow("Index");

						while (ctl.Facts.Count <= index)
						{
							var ev = new EventFacts(parent: ctl, functions: Functions);
							ctl.Facts.Add(ev);
						}
						ctl.Facts[index].NewInRoster = ctl.Facts[index].CurrentInRoster = xevent.Value;
						ctl.Facts[index].DefaultName = (name == "") ? "" : $"{name}_{EventDescs[index].Ending}";
						ctl.Facts[index].TreeElement = xevent;
					}
				}
			}
		}

		public override void UpdateDiscrepancyCount() =>
				DiscrepancyCount = IiLines.Sum(line => line.DiscrepancyCount);

		public override void UpdateChangesPending() =>
			ChangesPending = IiLines.Sum(line => line.ChangesPending);
	}

}
