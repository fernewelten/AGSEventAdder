using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AgsEventAdder.Properties
{
	public interface IAgsReader
	{
		public int Peek();
		public int Read();
	}

	/// <summary>
	/// A shim around StreamReader that implements IAgsReader,. 
	/// </summary>
	public class StreamReaderShim : IAgsReader
	{
		private readonly StreamReader _reader;

		public StreamReaderShim(StreamReader reader)
		{
			_reader = reader; 
		}

		public int Peek() => _reader.Peek();
		public int Read() => _reader.Read();
	}

}
