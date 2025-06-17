using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
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
	/// Overview items that contain a table of a list of events per entity
	/// </summary>
	public abstract class TableOverviewItem(EventCarrier carrier, string name, string icon) 
		: OverviewItem(carrier, name, icon)
	{
		public abstract void UpdateDiscrepancyCount();

		public abstract void UpdateChangesPending();

		public abstract IList GetLines();


		public void InsertDefaultWhereverEmpty(IList list, int fact_col, bool add_to_code = false)
		{
			if (list is null ||  list.Count == 0)
				list = GetLines();
			foreach (var item in list)
			{
				if (item is null or not TableLine)
					continue;

				var facts = (item as TableLine).Facts[fact_col];
				if (!string.IsNullOrWhiteSpace(facts?.NewInRoster))
					continue;

				facts.ChangeToDefault();
				if (add_to_code)
					facts.MustAddStubToCode = true;
			}
		}
		
		public void AddEventsToCodeWheneverMissing(IList list, int fact_col)
		{
			if (list is null || list.Count == 0)
				list = GetLines();
			foreach (var item in list)
			{
				if (item is null or not TableLine)
					continue;

				var facts = (item as TableLine).Facts[fact_col];
				if (string.IsNullOrWhiteSpace(facts?.NewInRoster))
					continue;

				facts.MustAddStubToCode = !facts.NewIsInCode;
			}
		}

		
		public void ClearEventsWithoutCode(IList list, int fact_col)
		{
			if (list is null || list.Count == 0)
				list = GetLines();
			foreach (var item in list)
			{
				if (item is null or not TableLine)
					continue;

				var facts = (item as TableLine).Facts[fact_col];
				if (string.IsNullOrWhiteSpace(facts?.NewInRoster))
					continue;

				if (!facts.NewIsInCode)
					facts.NewInRoster = "";
			}
		}

		
		public void CancelAllPendingChanges(IList list, int fact_col)
		{
			if (list is null || list.Count == 0)
				list = GetLines();
			foreach (var item in list)
			{
				if (item is null or not TableLine)
					continue;

				var facts = (item as TableLine).Facts[fact_col];
				if (facts is null)
					continue;

				facts.CancelPendingChanges();
			}
		}		
	}

	public class CharacterTable : TableOverviewItem
	{
		public ObservableCollection<CharacterTableLine> Lines { get; set; }

		public CharacterTable()
			: base(carrier: EventCarrier.Characters, name: "Character events", icon: "🧑")
		{
			Lines = [];
		}

		public override void UpdateDiscrepancyCount() => 
			DiscrepancyCount = Lines.Sum(line => line.DiscrepancyCount);

		public override void UpdateChangesPending() => 
			ChangesPending = Lines.Sum(line => line.ChangesPending);

		public override IList GetLines() => Lines;


		public CharacterTable Init(XDocument tree, EventDescs descs, HashSet<string> global_funcs)
		{
			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("Characters")
				.ElementOrThrow("CharacterFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessCharacterFolder(xfolder, descs.CharacterEvents, global_funcs);
			return this;

			void ProcessCharacterFolder(
				XElement xfolder, List<EventDesc> descs, HashSet<string> global_funcs)
			{
				var xsubfolders = xfolder.ElementOrThrow("SubFolders");
				foreach (XElement xsubfolder in xsubfolders.Elements())
				{
					if (xsubfolder.Name != "CharacterFolder")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
							xsubfolder);
					ProcessCharacterFolder(xsubfolder, descs, global_funcs);
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

					CharacterTableLine ctl = new(this) { Name = name, Id = id, Facts = [] };
					Lines.Add(ctl);
					var xinteractions = xcharacter.ElementOrThrow("Interactions");
					foreach (XElement xevent in xinteractions.Elements())
					{
						var index = xevent.IntAttributeValueOrThrow("Index");

						while (ctl.Facts.Count <= index)
						{
							var ev = new EventFacts(parent: ctl, global_funcs: global_funcs);
							ctl.Facts.Add(ev);
						}
						ctl.Facts[index].NewInRoster = ctl.Facts[index].CurrentInRoster = xevent.Value;
						ctl.Facts[index].DefaultName = (name == "") ? "" : $"{name}_{descs[index].Ending}";
					}
				}
			}
		}
	}
}
