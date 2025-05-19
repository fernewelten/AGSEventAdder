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
			String ret = first_ch.ToString();
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
			const String lit_starters = "'\"";
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

			String ret = opener.ToString();
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
			const String openers = "{[(";
			const String closers = "}])";

			String ret = "";
			int nesting = 0;

			do
			{
				String token = ReadToken();
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
		/// Collect all the names functions that are declared with body 
		/// </summary>
		/// <returns> (Unsorted) list of the functions </returns>
		public HashSet<String> CollectFunctionsWithBody()
		{
			const String before_func = "{;";

			HashSet<String> ret = [];

			while (true)
			{
				// The first char of the last token read
				char last_t_start;
				do // exactly 1 time
				{
					// Try to read a type
					String type_s = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(type_s))
						return ret; // End-of-stream; let's return what we've got
					last_t_start = type_s[0];
					if (!StartsIdent(last_t_start))
						break; // can't be type of function decl

					// Try to read a name
					String name_s = ReadDelimitedTokens();
					if (name_s == "*") 
						name_s = ReadDelimitedTokens();
					if (String.IsNullOrEmpty (name_s))
						return ret;	
					last_t_start = name_s[0];
					if (!StartsIdent(last_t_start))
						break; // can't be name of function decl

					// Try to read 'noloopcheck' or an expression in parentheses
					String paren_s = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(paren_s))
						return ret;
					if (paren_s == "noloopcheck")
					{
						paren_s = ReadDelimitedTokens();
						if (String.IsNullOrEmpty(paren_s))
							return ret;
					}
					last_t_start = paren_s[0];
					if (last_t_start != '(')
						break;

					// Try to read an expression in braces
					String brace_s = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(brace_s))
						return ret;
					last_t_start = brace_s[0];
					if (last_t_start != '{')
						break;

					// This is a header of a function declaration with body.
					ret.Add(name_s);
				} while (false);

				// if we aren't behind ';' or '{…}', wait for that
				while (before_func.IndexOf(last_t_start) < 0)
				{
					String token_s = ReadDelimitedTokens();
					if (String.IsNullOrEmpty(token_s))
						return ret;
					last_t_start = token_s[0];
				}
			}
		}
	}
}