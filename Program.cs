using System;
using System.Collections.Generic;
using System.IO;

namespace SMD_Move_tool
{
	class Program
	{
		static int GetMaterialIndex(string[] tNames, string sName, int iMatCount)
		{
			for (int i = 0; i <= iMatCount; i++)
			{
				if (tNames[i] == sName)
				{
					return i;
				}
			}
			return -1;
		}
		static bool CheckRangeIndex(int iIndex, int iCount)
		{
			return (iIndex >= 0 && iIndex <= iCount);
		}
		static void Main(string[] args)
		{
			Console.Title = "SMD Move tool | Waiting file";
			Console.WriteLine("Created by JarosLucky in 2021 year.\n");

		Request_File:
			Console.WriteLine("Drag the smd file you want to edit here.\n");

			string sSMD_Path = Console.ReadLine().Replace("\"", "");

			Console.Clear();
			Console.WriteLine(Path.GetFullPath(sSMD_Path));
			if (!sSMD_Path.ToLower().EndsWith(".smd"))
			{
				Console.WriteLine("ERROR: The previous file was not in smd format.");
				goto Request_File;
			}

			if (!System.IO.File.Exists(sSMD_Path))
			{
				Console.WriteLine("ERROR: The previous file was not exists.");
				goto Request_File;
			}

			Console.Title = "SMD Move tool | Processing the SMD file";
			Console.WriteLine("Reading the smd file...\n");

			try
			{
				string sSMD_Data = File.ReadAllText(sSMD_Path);
				int iSMD_Length = sSMD_Data.Length;
				Console.WriteLine("Detecting line transfer type...");

				// We have t three of endings...
				string  sEnding;
				int iEnding_Size;
				if (sSMD_Data.IndexOf("\r\n") > -1)
				{
					sEnding = "\r\n";
					Console.WriteLine("Used line transfer type: CRLF.");
				}
				else if (sSMD_Data.IndexOf("\n") > -1)
				{
					sEnding = "\n";
					Console.WriteLine("Used line transfer type: LF.");
				}
				else if (sSMD_Data.IndexOf("\r") > -1)
				{
					sEnding = "\r";
					Console.WriteLine("Used line transfer type: CR.");
				}
				else
				{
					Console.WriteLine("ERROR: We cant found line ending... Most likely the file is empty...");
					goto Request_File;
				}

				iEnding_Size = sEnding.Length;

				Console.WriteLine("\nStart of processing...");
				int iPoint = sSMD_Data.IndexOf("triangles" + sEnding);
				if (iPoint == -1)
				{
					Console.WriteLine("ERROR: The SMD file does not contain triangles!");
					goto Request_File;
				}

				iPoint = iPoint + 9 + iEnding_Size;
				int iStart_Point = iPoint;
				int iLast_Point = iPoint;

				string sSMD_File_Begin = sSMD_Data.Substring(0, iLast_Point);
				string sSMD_File_Repeats = "";
				string sSMD_File_End;

				int iMaterials_Count = -1;
				List<int> tMapping = new List<int>(64);
				string[] tNames = new string[64];
				string[] tData = new string[64];

				Console.Write("Starting triangles...");

				string sLast_Added_Material = "";  
				bool bEnd_Found = false;
				int iCount = 0;
				while (!bEnd_Found)
				{
					iCount++;
					iPoint = sSMD_Data.IndexOf(sEnding, iPoint);
					if (iPoint == -1)
						return;

					string sHead_Line = sSMD_Data.Substring(iLast_Point, iPoint - iLast_Point);
				
					if (sLast_Added_Material != sHead_Line)
					{
						if (iMaterials_Count >= 0)
						{
							if (tData[iMaterials_Count] == null)
								tData[iMaterials_Count] = sSMD_Data.Substring(iStart_Point, iLast_Point - iStart_Point);
							else
								sSMD_File_Repeats += sSMD_Data.Substring(iStart_Point, iLast_Point - iStart_Point);
						}

						if (sHead_Line == "end")
						{
							Console.WriteLine("\nFinished reading the triangles.");
							bEnd_Found = true;
							continue;
						}

						Console.Write("\nReading " + sHead_Line + "...");
						iStart_Point = iLast_Point;

						if (GetMaterialIndex(tNames, sHead_Line, iMaterials_Count) == -1)
						{
							Console.Write("\tNew!");
							iMaterials_Count += 1;

							tNames[iMaterials_Count] = sHead_Line;
							tMapping.Add(iMaterials_Count);
						}

						sLast_Added_Material = sHead_Line;
					}

					iPoint = sSMD_Data.IndexOf(sEnding, iPoint + iEnding_Size);
					iPoint = sSMD_Data.IndexOf(sEnding, iPoint + iEnding_Size);
					iPoint = sSMD_Data.IndexOf(sEnding, iPoint + iEnding_Size) + iEnding_Size;

					iLast_Point = iPoint;
				}

				Console.WriteLine("Finishing...");
				sSMD_File_End = sSMD_Data.Substring(iLast_Point, iSMD_Length - iLast_Point);
				Console.WriteLine("Done!");

				sSMD_Data = null;
				tMapping.TrimExcess();

				Console.Title = "SMD Move tool | Working with the file: " + sSMD_Path;
			Display_Mapping:
				Console.Clear();

				Console.WriteLine("Segments grouped by material:");
				for (int i = 0; i <= iMaterials_Count; i++)
				{
					Console.Write(i + "\t");
					Console.WriteLine(tNames[tMapping[i]]);
				}

				Console.WriteLine("\nWhat is the position of the element that is being shifted? Use \"save\"(\"-\") to exit.\nHint: You can also specify a second position to swap elements.");

			Wait_Command:
				string sInput = Console.ReadLine();

				if (sInput.ToLower() == "save" || sInput == "-")
					goto Save_File;

				int iArg_Elem_Pos = 0;
				int iArg_End_Pos = 0;

				string[] tArgs = sInput.Split();
				if (tArgs.Length == 0 || !int.TryParse(tArgs[0], out iArg_Elem_Pos) || tArgs.Length > 1 && !int.TryParse(tArgs[1], out iArg_End_Pos))
				{
					Console.WriteLine("ERROR: Incorrect command.");
					goto Wait_Command;
				}

				if (!CheckRangeIndex(iArg_Elem_Pos, iMaterials_Count) || !CheckRangeIndex(iArg_End_Pos, iMaterials_Count))
				{
					Console.WriteLine("ERROR: Incorrect positions.");
					goto Wait_Command;
				}

				int iElementIndex = tMapping[iArg_Elem_Pos];
				if (iArg_End_Pos == 0)
				{
					tMapping.RemoveAt(iArg_Elem_Pos);
					tMapping.Insert(iArg_End_Pos, iElementIndex);
				}
				else
				{
					tMapping[iArg_Elem_Pos] = tMapping[iArg_End_Pos];
					tMapping[iArg_End_Pos] = iElementIndex;
				}

				goto Display_Mapping;

			Save_File:
				Console.Title = "SMD Move tool | Saving";

				Console.Clear();
				Console.WriteLine("Saving file...");

				string sSMD_Out = sSMD_File_Begin;
				for (int i = 0; i <= iMaterials_Count; i++)
					sSMD_Out += tData[tMapping[i]];
				sSMD_Out += sSMD_File_Repeats + sSMD_File_End;

				File.WriteAllText(sSMD_Path, sSMD_Out);

				Console.WriteLine("Saved!");
			}
			catch (IOException e)
			{
				Console.Clear();
				Console.WriteLine("ERROR: Reading error!!!");
				Console.WriteLine(e.Message);
				Console.WriteLine("");

				goto Request_File;
			}

			Console.Title = "SMD Move tool | IDLE";

			//Console.WriteLine("Press any button to close...");
			//Console.ReadKey();

			Environment.Exit(0);
		}
	}
}
