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
			Console.WriteLine ("Current path = " + AppDomain.CurrentDomain.BaseDirectory);
			bool fileOk = false;
			bool dirOk = false;
			string outPath = AppDomain.CurrentDomain.BaseDirectory;
			string f = Console.ReadLine ();

			while (fileOk == false) {
				if (File.Exists (f)) {
					Console.WriteLine ("Good. " + f + " exists");
					fileOk = true;
				} else {
					Console.WriteLine (f + " does not exist, enter file name again: ");
					f = Console.ReadLine ();
				}		
			}

			while (dirOk == false) {
				Console.WriteLine ("Enter destination path for HL7 files or just Enter for current directory");
				outPath = Console.ReadLine ();
				if (outPath == "") {
					outPath = AppDomain.CurrentDomain.BaseDirectory;
				} else {
				}

				if (Directory.Exists(outPath)) {
					Console.WriteLine("Good. " + outPath + " exists");
					dirOk = true;
				} else {
					Console.WriteLine("Bad. " +  outPath + " Directory does not exist!");
				}

			
			}

			// reads file till end
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
			/* Sample ADT message
			 * 
			 * MSH|^~\&|ADM|UCMC|||201511141241||ADT^A08|1464821|D|2.4|||AL|NE|
			 * EVN|A08|201511141241|
			 * PID|1||M700002229^^^^MR^UCMC~E0-B20150812102144516^^^^PI^UCMC~E00003526^^^^HUB^UCMC||TEST^MOBILEPROUSE^^^^^L||19720229|M|||^^^^^^P|||||||MA0000032961|
			 * PV1|1|I|3ET^U349^1|EL|||SPROUSE^Prouse^Stephen^W^^MS^BC MS RN^1500000000^^^^^XX|||MED||||H|||SPROUSE^Prouse^Stephen^W^^MS^BC MS RN^^^^^^XX|IN||U|||||||||||||||||||UCMC||ADM|||201508121020|
			 * PV2||MS/GYN^MED SURG/GYN|TEST FOR V6MOBILE||||||||94|||||||||||||||||||||||||N|
			 * ROL|1|AD|AT|SPROUSE^Prouse^Stephen^W^^MS^BC MS RN^^^^^^XX|
			 * ROL|2|AD|AD|SPROUSE^Prouse^Stephen^W^^MS^BC MS RN^^^^^^XX|
			 * ROL|3|AD|PP|SPROUSE^Prouse^Stephen^W^^MS^BC MS RN^^^^^^XX|
			 * AL1|1|DA|F001000476^Penicillins^^Penicillins^^allergy.id|SV|Anaphylaxis|20151114|
			 * AL1|2|DA|F006016065^walnut^^walnut^^allergy.id|SV|Anaphylaxis|20150812|
			 * DRG|32 ICD10|
			*/

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
