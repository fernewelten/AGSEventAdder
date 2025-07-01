using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static AgsEventAdder.EventFacts;

namespace AgsEventAdder
{
	public enum EventCarrier
	{
		None = 0,

		Characters,
		Guis,
		Hotspots,
		InvItems,
		Objects,
		Regions,
		Rooms,
	}

	/// <summary>
	/// 
	/// </summary>
	public enum EventType
	{
		None = -1,

		// Mouse modes - numbers fixed and assigned by AGS
		// Keep these sorted by ascending numbers
		WalkTo = 0,
		LookAt = 1,
		InteractWith = 2,
		TalkTo = 3,
		UseInventoryOn = 4,
		PickUp = 5,
		Pointer = 6,
		Wait = 7,
		Mode8On = 8,
		Mode9On = 9,

		// Additional events
		// Keep these sorted alphabetically
		AfterFadeIn,
		AnyClickOn,
		ClickOn,
		FirstLoad,
		Leave,
		LeaveBottom,
		LeaveLeft,
		LeaveRight,
		LeaveTop,
		Load,
		MouseOver,
		OnActivate,
		OnChange,
		OnClick,
		OnSelectionChanged,
		OtherClickOn,
		RepExec,
		StandingOn,
		WalkOff,
		WalkOnto,
	}

	public class EventDesc
	{
		/// <summary>
		/// Cursor Mode, as given in the AGS file
		/// </summary>
		public EventType Type { get; set; } = EventType.None;

		public string Name { get; set; }
		
		/// <summary>
		/// Default functions end on this, e.g., "_Look"
		/// </summary>
		public String Ending { get; set; } = "";
		
		/// <summary>
		/// Signature of the function that implements the event
		/// </summary>
		public String Signature { get; set; } = "";

		public String ReturnType { get; set; } = "";

		public EventDesc Copy() => this.MemberwiseClone() as EventDesc;
	};

	public class EventFacts(TableLine parent, HashSet<string> functions) : INotifyPropertyChanged
	{
		public EventType EventType
		{
			get => _event_type;
			set
			{
				if (_event_type == value)
					return;

				_event_type = value;
				OnPropertyChanged(nameof(EventType));
			}
		}
		private EventType _event_type = EventType.None;

		public string CurrentInRoster
		{
			get => _current_in_roster;
			set
			{
				if (_current_in_roster == value)
					return;

				_current_in_roster = value;
				OnPropertyChanged(nameof(CurrentInRoster));
				UpdateDependentFields();
			}
		}
		private string _current_in_roster;

		public String NewInRoster
		{
			get => _new_in_roster;
			set
			{
				if (_new_in_roster == value)
					return;

				_new_in_roster = value;
				OnPropertyChanged(nameof(NewInRoster));
				UpdateDependentFields();
			}
		}
		private string _new_in_roster;

		public string DefaultName
		{
			get => _default_name;
			set
			{
				if (_default_name == value)
					return;

				_default_name = value;
				OnPropertyChanged(nameof(DefaultName));
				UpdateDependentFields();
			}
		}
		private string _default_name;

		public bool NewIsInCode
		{
			get => _new_is_in_code;
			private set
			{
				if (_new_is_in_code == value)
					return;

				_new_is_in_code = value;
				OnPropertyChanged(nameof(NewIsInCode));
			}
		}
		private bool _new_is_in_code;

		public bool DefaultIsInCode
		{
			get => _default_is_in_code;
			private set
			{
				if (_default_is_in_code == value)
					return;

				_default_is_in_code = value;
				OnPropertyChanged(nameof(DefaultIsInCode));
			}
		}
		private bool _default_is_in_code;

		public bool MustAddStubToCode
		{
			get => _must_add_stub_to_code;
			set
			{
				if (_must_add_stub_to_code == value)
					return;

				_must_add_stub_to_code = value;
				OnPropertyChanged(nameof(MustAddStubToCode));
				UpdateDependentFields();
			}
		}
		private bool _must_add_stub_to_code;

		public bool HasDiscrepancy
		{
			get => _has_discrepancy;
			set
			{
				if (_has_discrepancy == value)
					return;

				_has_discrepancy = value;
				OnPropertyChanged(nameof(HasDiscrepancy));
				parent?.UpdateDiscrepancyCount();
			}
		}
		private bool _has_discrepancy;

		public bool HasPendingChanges
		{
			get => _has_pending_changes;
			set
			{
				if (_has_pending_changes == value)
					return;

				_has_pending_changes = value;

				OnPropertyChanged(nameof(HasPendingChanges));
				parent?.UpdateChangesPending();
			}
		}
		private bool _has_pending_changes;

		/// <summary>
		/// The element of the Tree that is associated with this Facts
		/// (for writing back the changes)
		/// </summary>
		public XElement TreeElement { get; set; }

		/// <summary>
		/// Notify whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void UpdateDependentFields()
		{
			NewIsInCode = functions.Contains(NewInRoster);
			DefaultIsInCode = functions.Contains(DefaultName);

			HasPendingChanges = MustAddStubToCode || CurrentInRoster != NewInRoster;

			HasDiscrepancy = !MustAddStubToCode &&
				!String.IsNullOrWhiteSpace(NewInRoster) &&
				!NewIsInCode;
		}

		public void AddStubToCode()
		{
			MustAddStubToCode = true;
		}

		public void DontAddToCode()
		{
			MustAddStubToCode = false;
		}

		public void ChangeToDefault()
		{
			NewInRoster = DefaultName;
		}

		public void ChangeToCurrent()
		{
			NewInRoster = CurrentInRoster;
		}

		public void ClearEvent()
		{
			NewInRoster = "";
		}

		public void DiscardPendingChanges()
		{
			ChangeToCurrent();
			DontAddToCode();
		}

		public void UpdateTreeElementWhenPending()
		{
			TreeElement.Value = CurrentInRoster = NewInRoster.Trim();
		}

		public void UpdateCodeWhenPending(StreamWriter code, EventDesc desc, HashSet<string> functions)
		{
			if (!MustAddStubToCode)
				return; // Nothing to do

			string new_name = NewInRoster?.Trim();
			if (string.IsNullOrEmpty(new_name))
				return; // Nothing in roster

			bool stub_is_actually_missing = functions.Add(new_name);

			// Note: May only do this _after_ functions have been extended
			MustAddStubToCode = false;
			if (!stub_is_actually_missing)
				return; 

			// Add the stub
			string ret_type = string.IsNullOrWhiteSpace(desc.ReturnType) ?
				"void" : desc.ReturnType.Trim();
			string signature = string.IsNullOrWhiteSpace(desc.Signature) ?
				"()" : desc.Signature;

			code.WriteLine("");
			code.WriteLine($"{ret_type} {new_name}{signature}");
			code.WriteLine("{");
			code.WriteLine("");
			code.WriteLine("}");
		}
	}
}
