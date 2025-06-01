using AgsEventAdder.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace AgsEventAdder
{
	public class Scanner
	{
		private readonly IAgsReader _reader;

		public Scanner(IAgsReader reader)
		{
			_reader = reader;
		}

		public Scanner(String s)
		{
			MemoryStream stream = new(Encoding.UTF8.GetBytes(s))
			{
				Position = 0
			};
			var rd = new StreamReader(stream, Encoding.UTF8);
			_reader = new StreamReaderShim(rd);
		}

		/// <summary>
		/// Read the next token from the stream; 
		/// return null when the stream is exhausted
		/// </summary>
		/// <returns>token as a String</returns>
		public String ReadToken()
		{
			
			char first_char;
			do
			{
				char? ch = Read();
				if (ch == null)
					return null;
				first_char = (char)ch;
			}
			while (Char.IsWhiteSpace(first_char));

			if (StartsNumber(first_char))
				return ReadCharSeq(first_char, IsInNumber);
			if (StartsIdent(first_char))
				return ReadCharSeq(first_char, IsInIdent);
			if (StartsQuotedLiteral(first_char))
				return ReadQuotedLiteral(first_char);
			return first_char.ToString();
		}

		/// <summary>
		/// Read all consecutive chars that are in seq
		/// </summary>
		/// <param name="first_ch">The first character of the sequence</param>
		/// <param name="seq"></param>
		/// <returns>The characters read, as a string</returns>
		private String ReadCharSeq(char first_ch, CharTest test)
		{
			string ret = first_ch.ToString();
			while (true)
			{
				char? peek = Peek();
				if (peek == null || !test((char) peek))
					return ret;
				ret += Read()?.ToString();
			}
		}

		delegate bool CharTest (char ch);

		private bool StartsNumber(char ch)
		{
			return Char.IsDigit(ch);
		}

		private bool IsInNumber(char ch)
		{
			return Char.IsDigit(ch) || ".E+-".IndexOf(ch) >= 0;
		}

		private bool StartsIdent(char ch) 
		{ 
			return ch < 128 && (Char.IsLetter(ch) || ch == '_');
		}

		private bool IsInIdent(char ch)
		{
			return Char.IsLetterOrDigit(ch) || ch == '_';
		}

		private bool StartsQuotedLiteral(char ch)
		{
			const string lit_starters = "'\"";
			return lit_starters.IndexOf(ch) >= 0;
		}

		/// <summary>
		/// Read a quoted string.
		/// A ‘\\’ escapes the next character, 
		/// making it become part of the string even when it's the opener
		/// </summary>
		/// <param name="opener">The starting and ending delimeter of the string</param>
		/// <returns>The string including quote marks and delimeters</returns>
		private String ReadQuotedLiteral(char opener)
		{

			string ret = opener.ToString();
			while (true)
			{
				char? rd = Read();
				if (rd == null)
					return null;

				ret += rd;

				if (rd == '\\')
				{
					rd = Read();
					if (rd == null)
						return null;
					ret += rd;
					continue;
				}

				if (rd == opener)
					return ret;
			}
		}

		/// <summary>
		/// Read the next character from '_reader'
		/// </summary>
		/// <returns>The character read, or null when '_reader' is exhausted</returns>
		private char? Read()
		{
			try
			{
				int ch = _reader.Read();
				if (ch == -1)
					return null;
				return (char)ch;
			}
			catch 
			{
				return null;
			}
		}

		/// <summary>
		/// Peek at the next character from '_reader', don't consume that character
		/// </summary>
		/// <returns>The peeked character, or null when '_reader' is exhausted</returns>
		private char? Peek()
		{
			int ch;
			try
			{
				ch = _reader.Peek();
			}
			catch 
			{
				return null;
			}
		    
			return (ch == -1)? null : (char)ch;
		}

		
		/// <summary>
		/// Read the next token from the stream
		/// if it is an opening delimiter, 
		/// continue reading to and including the corresponding closer
		/// and return this sequence of delimiters.
		/// Otherwise, return this non-delimeter token
		/// </summary>
		/// <returns>The token or string of tokens, as a String</returns>
		public String ReadDelimitedTokens()
		{
			const string openers = "{[(";
			const string closers = "}])";

			string ret = "";
			int nesting = 0;

			do
			{
				string token = ReadToken();
				if (String.IsNullOrEmpty(token))
					return null;
				if (openers.IndexOf(token[0]) >= 0)
				{
					ret += token;
					nesting++;
					continue;
				}
				if (closers.IndexOf(token[0]) >= 0)
				{
					ret += token;
					nesting--;
					continue;
				}
				if (nesting == 0)
					ret = token;
			} while (nesting > 0);
			return ret;
		}

		/// <summary>
		/// Collect all the names functions that are declared 
		/// </summary>
		/// <returns> (Unsorted) list of the functions </returns>
		public void CollectDeclaredFunctions(HashSet<string> funcs)
		{
			// Find:
			// 'import TYPE FUNC (…);'
			// 'TYPE noloopcheck FUNC (…) {' 

			// Loop through the symbols, wait for a func declaration
			while (true)
			{
				string last_read;
				do // exactly 1 time
				{
					// Try to read a type
					string type_str = last_read = ReadDelimitedTokens();
					bool import_found = (type_str == "import");
					if (import_found)
						type_str = last_read = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(type_str))
						return; // End-of-stream; let's return what we've got					
					if (!StartsIdent(type_str[0]))
						break; // can't be type of function decl

					// Try to read a name
					string name_str = last_read = ReadDelimitedTokens();
					if (name_str == "*") 
						name_str = last_read = ReadDelimitedTokens();
					if (name_str == "noloopcheck")
						name_str = last_read = ReadDelimitedTokens();
					if (String.IsNullOrEmpty (name_str))
						return;	
					if (!StartsIdent(name_str[0]))
						break; // can't be name of function decl

					// Try to read an expression in parentheses
					string paren_str = last_read = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(paren_str))
						return;
					if (paren_str[0] != '(')
						break;
					// Try to read an expression in braces
					string brace_str = last_read = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(brace_str))
						return;
					if (brace_str == ";" && import_found)
						// This is an import stmt. of a function
						funcs.Add(name_str);
					else if (brace_str[0] == '{')
						// This is a header of a function declaration with body.
						funcs.Add(name_str);
				} while (false);

				if (last_read == null || last_read[0] == '{')
					continue;

				while (last_read != ";")
				{
					last_read = ReadDelimitedTokens();
					if (last_read is null)
						return;
				}
			}
		}
	}
}