using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Xml.Linq;

namespace AgsEventAdder
{
	internal static class XmlExtensions
	{
		/// <summary>
		/// Find '.Element(name)'; if not there, throw exception
		/// </summary>
		/// <param name="el">Parent element</param>
		/// <param name="name">Name of child element</param>
		/// <returns>The child element</returns>
		/// <exception cref="AgsXmlParsingException"></exception>
		public static XElement ElementOrThrow(this XElement el, XName name) =>
			el.Element(name) ??
				throw new AgsXmlParsingException(
					$"Sub-element '<{name}>' not found within <{el.Name}>", 
					el);

		/// <summary>
		/// Check that the element 'el' has an attribute 'attrib' that has the value 'value'
		/// </summary>
		/// <param name="el">The element</param>
		/// <param name="attrib">The attribute of the element</param>
		/// <param name="value">The value of the attribute</param>
		/// <returns>The element</returns>
		/// <exception cref="AgsXmlParsingException"></exception>
		public static XElement AttributeOrThrow(this XElement el, XName attrib, string value)
		{
			var att = el.Attribute(attrib) ?? throw new AgsXmlParsingException(
					$"Element '<{el.Name}>' doesn't contain an attribute called '{attrib}'");
			if (el.Attribute(attrib).Value != value)
				throw new AgsXmlParsingException(
					$"In Element '<{el.Name}>', the attribute '{attrib}' doesn't have the value '{value}'");
			return el;
		}

		/// <summary>
		/// Find '.Element(name)'; if not there, throw exception
		/// The contents of this child element must be an integer, otherwise throw exception.
		/// </summary>
		/// <param name="el">Parent element</param>
		/// <param name="name">Name of child element</param>
		/// <returns>The integer in child element</returns>
		/// <exception cref="AgsXmlParsingException"></exception>
		internal static int IntElementOrThrow(this XElement el, XName name)
		{
			var name_el = el.ElementOrThrow(name);
			if (!int.TryParse(name_el.Value, out var int_value))
				throw new AgsXmlParsingException(
					$"Element '<{name_el.Name}>' doesn't contain a legal integer", 
					name_el);
			return int_value;
		}
	}
}
