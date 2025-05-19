using AgsEventAdder.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AgsEventAdder
{
	/// <summary>
	/// Stellt Routinen zum Lesen des nächsten Zeichens zur Verfügung,
	/// wobei Kommentare ignoriert werden.
	/// </summary>
	public class Preprocessor : IAgsReader
	{
		public const int EOF = -1;

		private readonly IAgsReader _reader;
		private readonly List<int> _buffer = [];

		private bool _at_line_start = true;

		public Preprocessor(IAgsReader reader)
		{
			_reader = reader;
		}

		public Preprocessor(String s)
		{
			// Can't elegantly daisy-chain c'tors because that would need to
			// happen right in the defining signature, turning the whole routine
			// into one big blob statement which isn't easy to understand at a
			// glance any more.
			MemoryStream stream = new(Encoding.UTF8.GetBytes(s))
			{
				Position = 0
			};
			var rd = new StreamReader(stream, Encoding.UTF8);
			_reader = new StreamReaderShim(rd);
		}

		public int Peek()
		{
			FillBuffer();

			return (_buffer.Count > 0) ? _buffer[0] : EOF;
		}

		public int Read()
		{
			FillBuffer();
			if (_buffer.Count == 0)
				return EOF;
			int ret = _buffer[0];
			_buffer.RemoveAt(0);
			return ret;
		}

		private void FillBuffer()
		{
			while (true)
			{
				// Make sure the buffer contains 1 char
				if (_buffer.Count == 0)
					_buffer.Add(Get());

				switch (_buffer[0])
				{
					case EOF:
						return; // source has dried up

					case '/':
						if (_buffer.Count < 2)
							_buffer.Add(Get());
						if (_buffer[1] == '/')
						{
							// Process '//'
							_buffer.RemoveRange(0, 1);
							_buffer[0] = '\n';
							SkipToEol(process_backslash_eol: false);
							break;
						}
						else if (_buffer[1] == '*')
						{
							// Process '/*'
							_buffer.RemoveRange(0, 1);
							_buffer[0] = ' ';
							SkipToCommentClose();
							break;
						}
						break;

					case '\\':
						if (_buffer.Count < 2)
							_buffer.Add(Get());
						if (_buffer[1] == '\n')
						{
							_buffer.RemoveRange(0, 2);
							continue;
						}
						else if (_buffer[1] == '\r')
						{
							if (_buffer.Count < 3)
								_buffer.Add(Get());
							if (_buffer[2] == '\n')
							{
								_buffer.RemoveRange(0, 3);
								continue;
							}
						}
						break;

					case '\r':
						// Delete whenever '\n' follows immediately
						if (_buffer.Count < 2)
							_buffer.Add(Get());
						if (_buffer[1] == '\n')
							_buffer.RemoveRange(0, 1);
						break;

					case '#':
						// Kludge: When the very first read character is a '#'
						// then the line is a preprocessor command
						if (_at_line_start)
						{
							_buffer[0] = '\n';
							SkipToEol(process_backslash_eol: true);
						}
						return;
				}
				// With this statement, '_at_line_start' will be true 
				// at the time the NEXT character is read.
				_at_line_start = (_buffer[0] == '\n');
				return;
			}
		}

		private int Get()
		{
			// Kludge to detect when the 1st character is read
			try
			{
				return _reader.Read();
			}
			catch
			{
				return EOF;
			}
		}

		private void SkipToCommentClose()
		{
			int nx_char;
			do
			{
				do
				{
					nx_char = Get();
					if (nx_char == EOF)
						return;
					else if (nx_char == '\n')
						_buffer.Add(nx_char);
				} while (nx_char != '*');
				nx_char = Get();
				if (nx_char == EOF)
					return;
				else if (nx_char == '\n')
					_buffer.Add(nx_char);
			} while (nx_char != '/');
		}

		private void SkipToEol(bool process_backslash_eol)
		{
			int nx_char;
			do
			{
				nx_char = Get();
				if (process_backslash_eol && nx_char == '\\')
				{
					nx_char = Get();
					if (nx_char == '\r')
						nx_char = Get();
					if (nx_char == '\n')
					{
						_buffer.Add(nx_char);
						nx_char++; // make nx_char NOT be '\n' or EOF
					}
				}
			} while (nx_char != '\n' && nx_char != EOF);
		}
	}
}
