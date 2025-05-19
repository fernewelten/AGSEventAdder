using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgsEventAdder
{
	public interface IAgsReader
	{
		public int Peek();
		public int Read();
	}

	/// <summary>
	/// A shim around StreamReader that implements IAgsReader,. 
	/// </summary>
	internal class StreamReaderShim(StreamReader reader) : IAgsReader
	{
		private readonly StreamReader _reader = reader;

		public int Peek() => _reader.Peek();
		public int Read() => _reader.Read();
	}
}
