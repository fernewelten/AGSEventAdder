using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgsEventAdder
{
	internal class Event
	{
		/// <summary>
		/// Thing ID, e.g. object ID
		/// </summary>
		public int ThingId;

		public EventId Id {  get; set; }
		
		/// <summary>
		/// The function in the grid
		/// </summary>
		public String CurrentFunction { get; set; }
		
		/// <summary>
		/// Whether a function named 'FunctionName' is defined in code
		/// </summary>
		public bool IsCurrentFunctionInCode { get; set; }

		/// <summary>
		/// Whether a function with the default name is defined in code
		/// </summary>
		public bool IsDefaultInCode { get; set; }

		public String ChangedFunction { get; set; }

		public bool AddFunctionToCode { get; set; }

		/// <summary>
		/// Whether a function stub should be added to the code
		/// </summary>
		public bool SetFunctionInGrid { get; set; }

	}

	public enum EventId
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
		AnyClickOn,
		ClickOn,
		MouseOver,
		OtherClickOn,
		StandingOn,
		WalkOff,
		WalkOnto,
	}

	internal class EventDesc
	{
		/// <summary>
		/// Cursor Mode, as given in the AGS file
		/// </summary>
		public EventId Id { get; set; } = EventId.None;
		public String Description { get; set; } = "";
		public String Ending { get; set; } = "";
		public String Signature { get; set; } = "";

		public EventDesc Copy() => this.MemberwiseClone() as EventDesc;
	};
}
