using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgsEventAdder
{
	public class Room
	{
		public const int kNone = -1;

		public String Nesting { get; set; }
		public int Id { get; set; }
		public String Description { get; set; }
	}
}
