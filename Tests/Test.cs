// Copyright 2017 Declan Hoare
// This file is part of Whoa.
//
// Whoa is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Whoa is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with Whoa.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Whoa;

namespace Whoa.Tests
{
	public static class Test
	{
		private enum RecordType
		{
			Single,
			EP,
			LP
		}
		private class Record
		{
			[Order]
			public string title;
			
			[Order]
			public string artist;
			
			[Order]
			public int rpm;
			
			[Order]
			public double price;
			
			[Order]
			public bool inLibrary;
			
			[Order]
			public bool otherBool;
			
			[Order]
			public List<string> songs;
			
			[Order]
			public Guid guidForSomeReason;
			
			[Order]
			public List<bool> moreBools;
			
			[Order]
			public string awards;
			
			[Order]
			public int? profits;
			
			[Order]
			public int? losses;
			
			[Order]
			public List<int> somethingElse;
			
			[Order]
			public Dictionary<string, string> staff;
			
			[Order]
			public Dictionary<string, string> plausibleSampleData;
			
			[Order]
			public List<bool> notActuallyMoreBools;
			
			[Order]
			public bool boolThree;
			
			[Order]
			public bool boolFour;
			
			[Order]
			public bool boolFive;
			
			[Order]
			public bool boolSix;
			
			[Order]
			public bool boolSeven;
			
			[Order]
			public bool boolEight;
			
			[Order]
			public BigInteger reallyReallyReallyReallyReallyReallyBigNumber;
			
			[Order]
			public DateTime releaseDate;
			
			[Order]
			public RecordType kind;
			
			public override string ToString()
			{
				string ret = $@"{title}
Artist: {artist}
RPM: {rpm}
Price: {price}
Released: {releaseDate}
";
				ret += (inLibrary ? "In" : "Not in") + " library" + Environment.NewLine;
				ret += "Something else about it: " + (otherBool ? "Yep" : "Nope") + Environment.NewLine;
				ret += "Songs:" + Environment.NewLine;
				int track = 0;
				foreach (string song in songs)
					ret += $"{++track}. {song}" + Environment.NewLine;
				ret += $"Guid (?!): {guidForSomeReason}" + Environment.NewLine;
				ret += "Some more information:" + Environment.NewLine;
				foreach (bool info in moreBools)
					ret += (info ? "Yes" : "No") + Environment.NewLine;
				ret += "Awards: " + (awards == null ? "N/A" : awards) + Environment.NewLine;
				ret += "Profits: " + (profits == null ? "N/A" : profits.ToString()) + Environment.NewLine;
				ret += "Losses: " + (losses == null ? "N/A" : losses.ToString()) + Environment.NewLine;
				ret += "Null lists work: " + (somethingElse == null ? "Yes" : "No") + Environment.NewLine;
				ret += "Staff:" + Environment.NewLine;
				foreach (var pair in staff)
					ret += $"{pair.Key} - {pair.Value}" + Environment.NewLine;
				ret += "Null dictionaries work: " + (plausibleSampleData == null ? "Yes" : "No") + Environment.NewLine;
				ret += "Null bool lists work: " + (notActuallyMoreBools == null ? "Yes" : "No") + Environment.NewLine;
				ret += $@"boolThree = {boolThree}
boolFour = {boolFour}
boolFive = {boolFive}
boolSix = {boolSix}
boolSeven = {boolSeven}
boolEight = {boolEight}
A number that is so shockingly large that you won't believe how large it is, even though it doesn't mean anything: {reallyReallyReallyReallyReallyReallyBigNumber}
This album is a: {kind}.
";
				return ret;
			}
		}
		public static void Main(string[] args)
		{
			using (var str = new MemoryStream())
			{
				var rec = new Record()
				{
					title = "Cool Songs For Cool People",
					artist = "Ethan Klein",
					rpm = 78,
					price = 99.99,
					inLibrary = true,
					otherBool = true,
					songs = new string[]
					{
						"International Tiles",
						"Test Data"
					}.ToList(),
					guidForSomeReason = Guid.NewGuid(),
					moreBools = new bool[] {true, false, false, true, true, true, false, false, true, true}.ToList(),
					awards = null,
					profits = 5000,
					losses = null,
					somethingElse = null,
					staff = new Dictionary<string, string>
					{
						{"Ethan Klein", "Artist"},
						{"Post Malone", "Featured Artist"},
						{"Frank Walker", "Tile Provider"},
						{"This is some", "test data"}
					},
					plausibleSampleData = null,
					notActuallyMoreBools = null,
					boolThree = false,
					boolFour = true,
					boolFive = false,
					boolSix = true,
					boolSeven = false,
					boolEight = true,
					reallyReallyReallyReallyReallyReallyBigNumber = BigInteger.Parse("41290871590318501381209471092481204"),
					releaseDate = new DateTime(2015, 04, 20),
					kind = RecordType.EP
				};
				string expected = rec.ToString();
				Whoa.SerialiseObject(str, rec);
				if (args.Length > 0) // Save output for debugging.
				{
					str.Position = 0;
					using (var fobj = File.OpenWrite(args[0]))
						str.CopyTo(fobj);
				}
				str.Position = 0;
				var res = Whoa.DeserialiseObject<Record>(str);
				string actual = res.ToString();
				Console.Write("Test ");
				if (expected == actual)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("passed");
					Console.ResetColor();
					Console.Write(actual);
                    Console.ReadKey(true); //Us Visual Studio users don't get to press a key before the app closes. This must be added directly in the code.
                    Environment.Exit(0);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("FAILED");
					Console.ResetColor();
					Console.WriteLine("Expected:");
					Console.Write(expected);
					Console.WriteLine("");
					Console.WriteLine("Actual:");
					Console.Write(actual);
                    Console.ReadKey(true); //Us Visual Studio users don't get to press a key before the app closes. This must be added directly in the code.
					Environment.Exit(1);
				}
			}
		}
	}
}
