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
				if (item is null or not TableLine)
					continue;

				var facts = (item as TableLine).Facts[fact_col];
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
				list = Lines;
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
				list = Lines;
			foreach (var line in list)
			{
				if (line is null || line is not TableLine tline)
					continue;

				var facts = tline.Facts[fact_col];
				facts?.CancelPendingChanges();
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

				for (int idx = 0; idx < tline.Facts.Count; idx++)
				{
					EventDesc desc = EventDescs[idx];
					EventFacts facts = tline.Facts[idx];
					facts?.UpdateCodeWhenPending(code_writer, desc, Functions);
				}
			}
		}
	}

	public class CharacterTable : TableOverviewItem
	{
		public override IList Lines => _lines;
		public ObservableCollection<CharacterTableLine> ChLines => _lines;
		private ObservableCollection<CharacterTableLine> _lines = [];

		public CharacterTable(EventDescs descs, string code_file, HashSet<string> functions)
			: base(carrier: EventCarrier.Characters, name: "Character events", icon: "🧑")
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
			ProcessCharacterFolder(xfolder);
			return this;

			void ProcessCharacterFolder(XElement xfolder)
			{
				var xsubfolders = xfolder.ElementOrThrow("SubFolders");
				foreach (XElement xsubfolder in xsubfolders.Elements())
				{
					if (xsubfolder.Name != "CharacterFolder")
						throw new AgsXmlParsingException(
							$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
							xsubfolder);
					ProcessCharacterFolder(xsubfolder);
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
}
