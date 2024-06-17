using System.Linq;
using System.Security.Cryptography;
using AgsEventAdder;


namespace Tests
{
	public class TokenReaderTests
	{
		const String T1 = "void main(int a)\n{ while (a)\t{ return a; } }";

		[Fact]
		public void ReadToken_1()
		{
			Scanner tr = new(T1);
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
			Scanner tr = new("1.23 123 1E+12");
			Assert.Equal("1.23", tr.ReadToken());
			Assert.Equal("123", tr.ReadToken());
			Assert.Equal("1E+12", tr.ReadToken());
		}

		[Fact]
		public void ReadToken_3()
		{
			Scanner tr = new("'a' '\\\\' '\\n'");
			Assert.Equal("'a'", tr.ReadToken());
			Assert.Equal("'\\\\'", tr.ReadToken());
			Assert.Equal("'\\n'", tr.ReadToken());
		}

		[Fact]
		public void ReadDelimitedTokens_1()
		{
			Scanner tr = new(T1);
			Assert.Equal("void",tr.ReadDelimitedTokens());
			Assert.Equal("main",tr.ReadDelimitedTokens());
			Assert.Equal("()",tr.ReadDelimitedTokens());
			Assert.Equal("{(){}}",tr.ReadDelimitedTokens());
			Assert.Null(tr.ReadDelimitedTokens());
		}

		[Fact]
		public void CollectFunctions_1()
		{
			const String input = """
				int DialogOption;
				String a, b, c = 9;
				function PrepareDisplay()
				{
				    cDis.on = false;
				}
				readonly int ML_BlindsOpenLocY = 130;
				function gTitleScr_Start_OnClick(GUIControl *control, MouseButton button)
				{

				}
				function gTitleScr_Quit_OnClick(GUIControl, MouseButton);
				int gTitleScr_Load_OnClick noloopcheck(GUIControl *control, MouseButton button)
				{

				}
				""";
			Scanner tr = new(input);
			var funcs = tr.CollectFunctionsWithBody();
			Assert.Contains("PrepareDisplay", funcs);
			Assert.Contains("gTitleScr_Start_OnClick", funcs);
			Assert.Contains("gTitleScr_Load_OnClick", funcs);
			Assert.Equal(3, funcs.Count);
		}
	}
}