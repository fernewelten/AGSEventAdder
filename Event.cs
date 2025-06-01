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

	public class EventFacts(TableLine parent, HashSet<string> global_funcs) : INotifyPropertyChanged
	{
		private EventType _event_type = EventType.None;
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

		private string _current_in_roster;
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

		private string _new_in_roster;
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

		private string _default_name;
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

		private bool _current_is_in_code;
		public bool CurrentIsInCode
		{
			get => _current_is_in_code;
			private set
			{
				if (_current_is_in_code == value)
					return;

				_current_is_in_code = value;
				OnPropertyChanged(nameof(CurrentIsInCode));
			}
		}

		private bool _new_is_in_code;
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

		private bool _default_is_in_code;
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

		private bool _add_stub_to_code;
		public bool AddStubToCode
		{
			get => _add_stub_to_code;
			set
			{
				if (_add_stub_to_code == value)
					return;

				_add_stub_to_code = value;
				OnPropertyChanged(nameof(AddStubToCode));
				UpdateDependentFields();
			}
		}

		private string _stub_to_add;
		public string StubToAdd
		{
			get => _stub_to_add;
			set
			{
				if (_stub_to_add == value)
					return;

				_stub_to_add = value;
				OnPropertyChanged(nameof(StubToAdd));
			}
		}

		private bool _has_discrepancy;
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

		private bool _has_pending_changes;
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
			CurrentIsInCode = global_funcs.Contains(CurrentInRoster);
			NewIsInCode = global_funcs.Contains(NewInRoster);
			DefaultIsInCode = global_funcs.Contains(DefaultName);

			var change_roster =
				!String.IsNullOrWhiteSpace(NewInRoster) &&
				CurrentInRoster != NewInRoster;
			HasPendingChanges = AddStubToCode || change_roster;

			var in_code = change_roster ? NewIsInCode : CurrentIsInCode;

			bool has_discrepancy = !in_code;
			if (!change_roster &&
				String.IsNullOrWhiteSpace(CurrentInRoster) &&
				DefaultIsInCode)
				has_discrepancy = true;
			HasDiscrepancy = has_discrepancy;
		}
	}
}
