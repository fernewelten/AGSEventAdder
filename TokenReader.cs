using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AGSEventAdder
{ 
	public class TokenReader
	{
		public StreamReader Reader { get; private set; }

		public TokenReader(Stream stream)
		{
			Reader = new StreamReader(stream);
		}

		public TokenReader(String s)
		{
			MemoryStream stream = new(Encoding.UTF8.GetBytes(s))
			{
				Position = 0
			};
			Reader = new StreamReader(stream, Encoding.UTF8);
		}

		/// <summary>
		/// Read the next token from the stream; 
		/// return null when the stream is exhausted
		/// </summary>
		/// <returns>token as a String</returns>
		public String ReadToken()
		{
			const String digits = "0123456789";
			const String number_chars = digits + ".+-Ee";
			const String letters =
				"abcdefghijklmnopqrstuvwxyz"
			  + "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const String ident_chars = digits + letters;

			char first_char;
			do
			{
				char? ch = Read();
				if (ch == null)
					return null;
				first_char = (char)ch;
			}
			while (Char.IsWhiteSpace(first_char));

			if (digits.IndexOf(first_char) >= 0)
				return ReadCharSeq(first_char, number_chars);
			if (letters.IndexOf(first_char) >= 0)
				return ReadCharSeq(first_char, ident_chars);
			if (first_char == '\'' || first_char == '"')
				return ReadQuoted(first_char);
			return first_char.ToString();
		}

		/// <summary>
		/// Read all conssecutive chars that are in seq
		/// </summary>
		/// <param name="first_ch">The first character of the sequence</param>
		/// <param name="seq"></param>
		/// <returns>The characters read, as a string</returns>
		private String ReadCharSeq(char first_ch, string seq)
		{
			String ret = first_ch.ToString();
			while (true)
			{
				char? peek = Peek();
				if (seq.IndexOf(peek??' ') < 0)
					return ret;
				ret += Read()?.ToString();
			}
		}

		/// <summary>
		/// Read a quoted string.
		/// A ‘\\’ escapes the next character, 
		/// making it become part of the string even when it's the opener
		/// </summary>
		/// <param name="opener">The starting and ending delimeter of the string</param>
		/// <returns>The string including quote marks and delimeters</returns>
		private String ReadQuoted(char opener)
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
		/// Read the next character from 'Reader'
		/// </summary>
		/// <returns>The character read, or null when 'Reader' is exhausted</returns>
		private char? Read()
		{
			try
			{
				int ch = Reader.Read();
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
		/// Peek at the next character from 'Reader', don't consume that character
		/// </summary>
		/// <returns>The peeked character, or null when 'Reader' is exhausted</returns>
		private char? Peek()
		{
			int ch;
			try
			{
				ch = Reader.Peek();
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
	}
}