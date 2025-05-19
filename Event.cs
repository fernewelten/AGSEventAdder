using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// Weist den verschiedenen Events eine Nummer zu.
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

	internal class EventDesc
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

	internal class EventFacts
	{
		public String CurrentInRoster { get; set; }
		public String NewInRoster { get; set; }
		public bool CurrentIsInCode { get; set; }
		public bool NewIsInCode { get; set; }
		public bool AddStubToCode { get; set;}
		public String StubToAdd { get; set;}
	}
}
