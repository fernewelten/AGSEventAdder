using System.Linq;
using System.Security.Cryptography;
using AgsEventAdder;


namespace Tests
{
	public class ScannerTests
	{
		const String T1 = "void main(int a)\n{ while (a)\t{ return a; } }";

		[Fact]
		public void ReadToken_ReadCharAndSymTokens1()
		{
			Scanner sc = new(T1);
			Assert.Equal("void", sc.ReadToken());
			Assert.Equal("main", sc.ReadToken());
			Assert.Equal("(", sc.ReadToken());
			Assert.Equal("int", sc.ReadToken());
			Assert.Equal("a", sc.ReadToken());
			Assert.Equal(")", sc.ReadToken());
		}

		[Fact]
		public void ReadToken_ReadNumberTokens()
		{
			Scanner sc = new("1.23 123 1E+12");
			Assert.Equal("1.23", sc.ReadToken());
			Assert.Equal("123", sc.ReadToken());
			Assert.Equal("1E+12", sc.ReadToken());
		}

		[Fact]
		public void ReadToken_HandleBackslash()
		{
			Scanner sc = new("'a' '\\\\' '\\n'");
			Assert.Equal("'a'", sc.ReadToken());
			Assert.Equal("'\\\\'", sc.ReadToken());
			Assert.Equal("'\\n'", sc.ReadToken());
		}

		[Fact]
		public void ReadDelimitedTokens_HandleDelimiters()
		{
			Scanner sc = new(T1);
			Assert.Equal("void",sc.ReadDelimitedTokens());
			Assert.Equal("main",sc.ReadDelimitedTokens());
			Assert.Equal("()",sc.ReadDelimitedTokens());
			Assert.Equal("{(){}}",sc.ReadDelimitedTokens());
			Assert.Null(sc.ReadDelimitedTokens());
		}

		[Fact]
		public void CollectFunctionsWithBody_CollectFunctions_1()
		{
			const String input = 
				"""
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
			Scanner sc = new(input);
			var funcs = sc.CollectFunctionsWithBody();
			Assert.Contains("PrepareDisplay", funcs);
			Assert.Contains("gTitleScr_Start_OnClick", funcs);
			Assert.Contains("gTitleScr_Load_OnClick", funcs);
			Assert.Equal(3, funcs.Count);
		}

		[Fact]
		public void CollectFunctionsWithBody_Test()
		{
			using StreamReader sr = File.OpenText("C:\\Users\\Peter G Bouillon\\Documents\\AGS\\Castle Escape\\TwoClickHandler.asc");
			var input = sr.ReadToEnd();

			Preprocessor pp = new(s: input);
			Scanner sc = new(reader: pp);

			var funcs = sc.CollectFunctionsWithBody();
			string fn = DateTime.Now.ToString("u");
			fn = fn.Replace(':', '.').Replace(' ', '_');
			using StreamWriter sw = new($"C:\\temp\\CollectFunc-{fn}.txt");
			foreach (var func in funcs) 
				sw.WriteLine(func);
		}
	}
}