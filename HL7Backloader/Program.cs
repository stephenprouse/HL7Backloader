using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HL7Backloader
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			List<string> lines = new List<string>();
			Console.WriteLine ("Enter path and file name");
			string f = Console.ReadLine ();

			Console.WriteLine ("Enter destination path for HL7 files");
			string outPath = Console.ReadKey ();

			using (StreamReader r = new StreamReader(f))
			{
				int lineCount = 0;
				string line;
				while ((line = r.ReadLine()) != null)
				{
					lines.Add(line);
					lineCount++;
				}
				Console.WriteLine("Total line/record count = " + lineCount);
			}

			Console.WriteLine("\nPress [Enter] to proceed with file creation");
			Console.ReadLine();

			int fileCount = 0;
			foreach (string row in lines)
			{
				string[] values = row.Split('|');
				CreateHL7Message(values[0], values[1], outPath);
				fileCount++;
			}
			Console.WriteLine(fileCount + " HL7 File Created");
			Console.ReadLine();
		}

		static void CreateHL7Message(string accountNumber, string queryValue, string outPath)
		{
			string dt = DateTime.Now.ToString("yyyyMMddHHmmss");
			string outFilePath = outPath + dt + "_" + accountNumber + ".ecj";
			HL72Message outMessage = new HL72Message();
			string mshSeg = @"MSH|^~\&|BOOST|BOOST|HIS|BOOST|" + dt + "||ORU^R01|" + dt + "|P|2.5";
			string pidSeg = @"PID|1|||||||||||||||||" + accountNumber + "||";
			string obrSeg = @"OBR|||||||" + dt;
			string obxSeg1 = @"OBX|1|NM|ADMPL3||" + queryValue + "||||||F";
			outMessage.AppendSegment(mshSeg);
			outMessage.AppendSegment(pidSeg);
			outMessage.AppendSegment(obrSeg);
			outMessage.AppendSegment(obxSeg1);
			File.WriteAllBytes(outFilePath, Encoding.ASCII.GetBytes(outMessage.ToString()));
		}
	}
}
