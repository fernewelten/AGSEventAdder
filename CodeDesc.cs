using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace AgsEventAdder
{
	internal class CodeDesc
	{
		public string Filepath { get; set; }
		public HashSet<string> Functions { get; set; }

		public void InitFunctions() // throws IOException
		{
			using StreamReader rd = new(path: Filepath);
			Functions = [];

			Preprocessor pp = new(reader: new StreamReaderShim(reader: rd));
			Scanner scanner = new(reader: pp);

			// collect all functions
			// recognise a function as NAME PARENTHESISED_EXPR BRACED_EXPR
			List<string> buffer = [" ", " ", " "];
			while (true)
			{
				String token = scanner.ReadToken();
				if (token is null)
					break;
				if (String.IsNullOrEmpty(token))
					continue;
				if (token == "noloopcheck")
					continue;
				buffer.Add(token);
				buffer.RemoveAt(0);
				if (!buffer[1].StartsWith("("))
					continue;
				if (!buffer[2].StartsWith("{"))
					continue;
				Functions.Add(buffer[0]);
			}
		}

		public void AddStub(string function, string signature) // throws IOException
		{
			Functions.Add(function);

			using (StreamWriter code = new(path: Filepath, append: true))
			{
				string stub =
					"""
					function {func}{signature}
					{
					    // TODO 
					}
					"""
					.Replace("{func}", function)
					.Replace("{signature}", signature);

				code.WriteLine(stub);
			};
		}
	}
}
