using AGSEventAdder;

namespace Tests
{
	public class TokenReaderTests
	{
		const String T1 = "void main(int a)\n{ while (a)\t{ return a; } }";

		[Fact]
		public void ReadToken_1()
		{
			TokenReader tr = new(T1);
			Assert.Equal("void", tr.ReadToken());
			Assert.Equal("main", tr.ReadToken());
			Assert.Equal("(", tr.ReadToken());
			Assert.Equal("int", tr.ReadToken());
			Assert.Equal("a", tr.ReadToken());
			Assert.Equal(")", tr.ReadToken());
		}

		[Fact]
		public void ReadToken_2()
		{
			TokenReader tr = new("1.23 123 1E+12");
			Assert.Equal("1.23", tr.ReadToken());
			Assert.Equal("123", tr.ReadToken());
			Assert.Equal("1E+12", tr.ReadToken());
		}

		[Fact]
		public void ReadToken_3()
		{
			TokenReader tr = new("'a' '\\\\' '\\n'");
			Assert.Equal("'a'", tr.ReadToken());
			Assert.Equal("'\\\\'", tr.ReadToken());
			Assert.Equal("'\\n'", tr.ReadToken());
		}

		[Fact]
		public void ReadDelimitedTokens()
		{
			TokenReader tr = new(T1);
			Assert.Equal("void",tr.ReadDelimitedTokens());
			Assert.Equal("main",tr.ReadDelimitedTokens());
			Assert.Equal("()",tr.ReadDelimitedTokens());
			Assert.Equal("{(){}}",tr.ReadDelimitedTokens());
			Assert.Null(tr.ReadDelimitedTokens());
		}
	}
}