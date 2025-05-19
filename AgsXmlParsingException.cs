using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AgsEventAdder
{
	internal class AgsXmlParsingException : Exception
	{

		public XElement Element { get; private set; }


		public AgsXmlParsingException(XElement element = null)
			=> Element = element;

		public AgsXmlParsingException(string message, XElement element = null)
			: base(message) => Element = element;

		public AgsXmlParsingException(string message, Exception innerException, XElement element = null)
			: base(message, innerException) => Element = element;

		public int GetLineNumber()
		{
			var li = Element as IXmlLineInfo;
			return li.HasLineInfo() ? li.LineNumber : 0;
		}
	}
}
