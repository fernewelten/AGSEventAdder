using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AgsEventAdder;
using static AgsEventAdder.XmlExtensions;

namespace AgsEventAdder.Tests
{
	public class XDocumentTests
	{
		[Fact]
		public void ElementOrThrow_ThrowAndGiveLineNumber()
		{
			const String _ags1 = 
				"""
				<?xml version="1.0" encoding="utf-8"?>
				<!--Comment-->
				<AED Version="3.0.3.2" VersionIndex="3060020" EditorVersion="3.6.0.48">
					<Game/>
				</AED>
				""";
			var root = XDocument.Parse(_ags1, LoadOptions.SetLineInfo)?.Root;
			Assert.NotNull(root);

			AgsXmlParsingException ex = 
				Assert.Throws<AgsXmlParsingException>(
					() => root.ElementOrThrow("Holzschuh"));
			var msg = ex.Message;
			Assert.Equal(3, ex.GetLineNumber());
			Assert.Contains("<AED", msg);
			Assert.Contains("<Holzschuh", msg);
		}
			
		[Fact]
		public void IntElementOrThrow_AcceptInt()
		{
			const String _ags1 = 
				"""
				<?xml version="1.0" encoding="utf-8"?>
				<AED>
					<Game>
						<Settings>
							<AlReAsRe>False</AlReAsRe>
							<AndrAppVerCo>112</AndrAppVerCo>
						</Settings>
					</Game>
				</AED>
				""";
			var root = XDocument.Parse(_ags1, LoadOptions.SetLineInfo)?.Root;
			Assert.NotNull(root);

			var game = root.ElementOrThrow("Game");
			var settings = game.ElementOrThrow("Settings");
			var integer = settings.IntElementOrThrow("AndrAppVerCo");
			Assert.Equal(112, integer);
		}

		[Fact]
		public void IntElementOrThrow_ThrowOnNonInt()
		{
			const String _ags1 = 
				"""
				<?xml version="1.0" encoding="utf-8"?>
				<!--Comment-->
				<AED Version="3.0.3.2" VersionIndex="3060020" EditorVersion="3.6.0.48">
					<AndrAppVerCo>112H</AndrAppVerCo>
				</AED>
				""";
			var root = XDocument.Parse(_ags1, LoadOptions.SetLineInfo)?.Root;
			Assert.NotNull(root);

			AgsXmlParsingException ex =
				Assert.Throws<AgsXmlParsingException>(
					() => root.IntElementOrThrow("AndrAppVerCo"));
		}
	}
}
