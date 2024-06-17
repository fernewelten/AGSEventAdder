using AgsEventAdder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
	
	public class PreProcessorTests
	{
		private String GetLine(Preprocessor pr)
		{
			String preprocessed = "";
			do
			{
				int ch = pr.Read();
				if (ch == Preprocessor.EOF)
					break;
				if (ch != '\n' && Char.IsWhiteSpace((char)ch))
					ch = ' ';
				preprocessed += (char)ch;
			}
			while (true);
			return preprocessed;
		}

		[Fact]
		public void SkipEolComment()
		{
			const String input = """
				int  // To B or not to B
				a;
				""";
			Preprocessor pr = new(input);

			String preprocessed = GetLine(pr);
			Assert.Equal("int  \na;", preprocessed);
		}

		[Fact]
		public void SkipLongComment()
		{
			const String input = """
				int	a /* To B or not /*
				to B */;
				""";
			Preprocessor pr = new(input);

			String preprocessed = GetLine(pr);
			Assert.Equal("int a  \n;", preprocessed);
		}

		[Fact]
		public void SkipFirstPpDirective1()
		{
			const String input = """
				#define
				t
				""";
			Preprocessor pr = new(input);
			String preprocessed = GetLine(pr);
			Assert.Equal("\nt", preprocessed);
		}

		[Fact]
		public void SkipFirstPpDirective2()
		{
			const String input = """
				t#
				""";
			Preprocessor pr = new(input);
			Assert.Equal('t', pr.Read());
			Assert.Equal('#', pr.Read());
		}

		[Fact]
		public void SkipPpDirective1()
		{
			const String input = 
				"""
				a
				#line
				;
				""";
			Preprocessor pr = new(input);
			String preprocessed = GetLine(pr);
			Assert.Equal("a\n\n;", preprocessed);
		}

		[Fact]
		public void SkipPpDirective2()
		{
			const String input = """
				a
				#line	\
				Extra line
				;
				""";
			Preprocessor pr = new(input);
			String preprocessed = GetLine(pr);
			Assert.Equal("a\n\n\n;", preprocessed);
		}
	}
}
