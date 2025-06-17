using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgsEventAdder
{
	internal class GuiControl
	{
		public string Nesting { get; set; }
		public string GuiControlName { get; set; }
		public int GuiControlId { get; set; }
		public GuiControlType Type { get; set; }
		public string EventName { get; set; }
		public EventFacts Facts { get; set; }
	}

	enum GuiControlType
	{
		Folder = -1,
		None = 0,
		Button,
		Gui,
		Inventory,
		Label,
		ListBox,
		Slider,
		TextBox,
	};
}
