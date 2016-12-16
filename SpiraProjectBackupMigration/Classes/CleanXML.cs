using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration.Classes
{
	/// <summary>Overrides standard XML Writer to filter out bad characters.</summary>
	public class CleanXMLWriter : XmlTextWriter
	{
		/// <summary>Constructor.</summary>
		/// <param name="s">The stream to write to.</param>
		public CleanXMLWriter(Stream s)
			: base(s, Encoding.UTF8)
		{ }

		/// <summary>Writes the string to the stream.</summary>
		/// <param name="text">The text to write.</param>
		public override void WriteString(string text)
		{
			string newText = String.Join("", text.Where(c => !char.IsControl(c)));
			base.WriteString(newText);
		}
	}

	/// <summary>Overrides the standard XMl Reader to filter out bad characters.</summary>
	public class CleanXMLReader : XmlTextReader
	{
		/// <summary>Constructior.</summary>
		/// <param name="s">The stream to read from.</param>
		public CleanXMLReader(Stream s)
			: base(s)
		{ }

		/// <summary>Read the next string from the file.</summary>
		/// <returns>The next (fileterd) string read.</returns>
		public override string ReadString()
		{
			string text = base.ReadString();
			string newText = String.Join("", text.Where(c => !char.IsControl(c)));
			return newText;
		}
	}
}