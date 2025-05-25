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
		internal static XElement ElementOrThrow(this XElement el, XName name) =>
			el.Element(name) ??
				throw new AgsXmlParsingException(
					$"Sub-element '<{name}>' not found within <{el.Name}>", 
					el);

		/// <summary>
		/// Check that the element 'el' has an attribute 'attrib' that has the expected 'expected'
		/// </summary>
		/// <param name="el">The element</param>
		/// <param name="attrib">The attribute of the element</param>
		/// <param name="expected">The expected of the attribute</param>
		/// <returns>The element</returns>
		/// <exception cref="AgsXmlParsingException"></exception>
		internal static XElement CheckAttributeOrThrow(this XElement el, XName attrib, string expected)
		{
			var actual = el.AttributeValueOrThrow(attrib);
			return actual == expected ? 
				el : throw new AgsXmlParsingException(
					$"In Element '<{el.Name}>', the attribute '{attrib}' has the value '{actual}' but should have '{expected}'", 
					el);
		}

		internal static string AttributeValueOrThrow(this XElement el, in XName attrib)
		{
			var att = el.Attribute(attrib) ?? throw new AgsXmlParsingException(
					$"Element '<{el.Name}>' doesn't have an attribute called '{attrib}'", 
					el);
			return att.Value;
		}

		internal static int IntAttributeValueOrThrow(this XElement el, in XName attrib)
		{
			var value = el.AttributeValueOrThrow(attrib);
			if (!int.TryParse(value, out var int_value))
				throw new AgsXmlParsingException(
					$"Element 'Attribute '{attrib}' of <{el}>' contains '{value}', this isn't a legal integer",
					el);
			return int_value;
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
					$"Element '<{name_el.Name}>' contains '{name_el.Value}', this isn't a legal integer", 
					name_el);
			return int_value;
		}
	}
}
