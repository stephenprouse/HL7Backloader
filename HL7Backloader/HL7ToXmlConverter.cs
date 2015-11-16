﻿using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace HL7Backloader
{
	/// This class takes an HL7 message
	/// and transforms it into an XML representation.
	public static class HL7ToXmlConverter
	{
		// This is the XML document we'll be creating
		private static XmlDocument _xmlDoc;

		/// <span class="code-SummaryComment"><summary></span>
		/// Converts an HL7 message into an XML representation of the same message.
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><param name="sHL7">The HL7 to convert</param></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		public static string ConvertToXml(string sHL7)
		{
			// Go and create the base XML
			_xmlDoc = CreateXmlDoc();

			// HL7 message segments are terminated by carriage returns,
			// so to get an array of the message segments, split on carriage return
			string[] sHL7Lines = sHL7.Split('\r');

			// Now we want to replace any other unprintable control
			// characters with whitespace otherwise they'll break the XML
			for (int i = 0; i < sHL7Lines.Length; i++)
			{
				sHL7Lines[i] = Regex.Replace(sHL7Lines[i], @"[^ -~]", "");
			}

			/// Go through each segment in the message
			/// and first get the fields, separated by pipe (|),
			/// then for each of those, get the field components,
			/// separated by carat (^), and check for
			/// repetition (~) and also check each component
			/// for subcomponents, and repetition within them too.
			for (int i = 0; i < sHL7Lines.Length; i++)
			{
				// Don't care about empty lines
				if (sHL7Lines[i] != string.Empty)
				{
					// Get the line and get the line's segments
					string sHL7Line = sHL7Lines[i];
					string[] sFields = HL7ToXmlConverter.GetMessgeFields(sHL7Line);

					// Create a new element in the XML for the line
					XmlElement el = _xmlDoc.CreateElement(sFields[0]);
					_xmlDoc.DocumentElement.AppendChild(el);

					// For each field in the line of HL7
					for (int a = 0; a < sFields.Length; a++)
					{
						// Create a new element
						XmlElement fieldEl = _xmlDoc.CreateElement(sFields[0] + 
							"." + a.ToString());

						/// Part of the HL7 specification is that part
						/// of the message header defines which characters
						/// are going to be used to delimit the message
						/// and since we want to capture the field that
						/// contains those characters we need
						/// to just capture them and stick them in an element.
						if (sFields[a] != @"^~\&")
						{
							/// Get the components within this field, separated by carats (^)
							/// If there are more than one, go through and create an element for
							/// each, then check for subcomponents, and repetition in both.
							string[] sComponents = HL7ToXmlConverter.GetComponents(sFields[a]);
							if (sComponents.Length > 1)
							{
								for (int b = 0; b < sComponents.Length; b++)
								{
									XmlElement componentEl = _xmlDoc.CreateElement(sFields[0] + 
										"." + a.ToString() + 
										"." + b.ToString());

									string[] subComponents = GetSubComponents(sComponents[b]);
									if (subComponents.Length > 1)
										// There were subcomponents
									{
										for (int c = 0; c < subComponents.Length; c++)
										{
											// Check for repetition
											string[] subComponentRepetitions = 
												GetRepetitions(subComponents[c]);
											if (subComponentRepetitions.Length > 1)
											{
												for (int d = 0; 
													d < subComponentRepetitions.Length; 
													d++)
												{
													XmlElement subComponentRepEl = 
														_xmlDoc.CreateElement(sFields[0] + 
															"." + a.ToString() + 
															"." + b.ToString() + 
															"." + c.ToString() + 
															"." + d.ToString());
													subComponentRepEl.InnerText = 
														subComponentRepetitions[d];
													componentEl.AppendChild(subComponentRepEl);
												}
											}
											else
											{
												XmlElement subComponentEl = 
													_xmlDoc.CreateElement(sFields[0] + 
														"." + a.ToString() + "." + 
														b.ToString() + "." + c.ToString());
												subComponentEl.InnerText = subComponents[c];
												componentEl.AppendChild(subComponentEl);

											}
										}
										fieldEl.AppendChild(componentEl);
									}
									else // There were no subcomponents
									{
										string[] sRepetitions = 
											HL7ToXmlConverter.GetRepetitions(sComponents[b]);
										if (sRepetitions.Length > 1)
										{
											XmlElement repetitionEl = null;
											for (int c = 0; c < sRepetitions.Length; c++)
											{
												repetitionEl = 
													_xmlDoc.CreateElement(sFields[0] + "." + 
														a.ToString() + "." + b.ToString() + 
														"." + c.ToString());
												repetitionEl.InnerText = sRepetitions[c];
												componentEl.AppendChild(repetitionEl);
											}
											fieldEl.AppendChild(componentEl);
											el.AppendChild(fieldEl);
										}
										else
										{
											componentEl.InnerText = sComponents[b];
											fieldEl.AppendChild(componentEl);
											el.AppendChild(fieldEl);
										}
									}
								}
								el.AppendChild(fieldEl);
							}
							else
							{
								fieldEl.InnerText = sFields[a];
								el.AppendChild(fieldEl);
							}
						}
						else
						{
							fieldEl.InnerText = sFields[a];
							el.AppendChild(fieldEl);
						}
					}
				}
			}

			return _xmlDoc.OuterXml;
		}

		/// <span class="code-SummaryComment"><summary></span>
		/// Split a line into its component parts based on pipe.
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><param name="s"></param></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		private static string[] GetMessgeFields(string s)
		{
			return s.Split('|');
		}

		/// <span class="code-SummaryComment"><summary></span>
		/// Get the components of a string by splitting based on carat.
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><param name="s"></param></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		private static string[] GetComponents(string s)
		{
			return s.Split('^');
		}

		/// <span class="code-SummaryComment"><summary></span>
		/// Get the subcomponents of a string by splitting on ampersand.
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><param name="s"></param></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		private static string[] GetSubComponents(string s)
		{
			return s.Split('&');
		}

		/// <span class="code-SummaryComment"><summary></span>
		/// Get the repetitions within a string based on tilde.
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><param name="s"></param></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		private static string[] GetRepetitions(string s)
		{
			return s.Split('~');
		}

		/// <span class="code-SummaryComment"><summary></span>
		/// Create the basic XML document that represents the HL7 message
		/// <span class="code-SummaryComment"></summary></span>
		/// <span class="code-SummaryComment"><returns></returns></span>
		private static XmlDocument CreateXmlDoc()
		{
			XmlDocument output = new XmlDocument();
			XmlElement rootNode = output.CreateElement("HL7Message");
			output.AppendChild(rootNode);
			return output;
		}
	}
}

