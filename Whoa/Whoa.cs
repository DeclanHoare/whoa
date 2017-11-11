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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Numerics;
using System.Runtime.Serialization;
using System.Drawing;
using System.Drawing.Imaging;

namespace Whoa
{
	public static class Whoa
	{
		private enum SpecialSizes
		{
			Null = -1,
			ReferenceEqual = -2 // not used yet...
		}
		
		private static Dictionary<Type, dynamic> SpecialSerialisers = new Dictionary<Type, dynamic>();
		
		static Whoa()
		{
			foreach (Type t in typeof(Whoa).GetNestedTypes(BindingFlags.NonPublic))
			{
				if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISpecialSerialiser<>)))
				{
					RegisterSpecialSerialiser((dynamic)Activator.CreateInstance(t));
				}
			}
		}
		
		public static void RegisterSpecialSerialiser<T>(ISpecialSerialiser<T> serialiser)
		{
			SpecialSerialisers.Add(typeof(T), serialiser);
		}
		
		private class GuidSerialiser: ISpecialSerialiser<Guid>
		{
			public void SerialiseObject(Stream fobj, Guid obj)
			{
				fobj.Write(obj.ToByteArray(), 0, 16);
			}
			
			public Guid DeserialiseObject(Stream fobj)
			{
				var guid = new byte[16];
				fobj.Read(guid, 0, 16);
				return new Guid(guid);
			}
		}
		
		private class BigIntegerSerialiser: ISpecialSerialiser<BigInteger>
		{
			public void SerialiseObject(Stream fobj, BigInteger obj)
			{
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
				{
					byte[] bytes = obj.ToByteArray();
					write.Write(bytes.Length);
					write.Write(bytes);
				}
			}
			
			public BigInteger DeserialiseObject(Stream fobj)
			{
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return new BigInteger(read.ReadBytes(read.ReadInt32()));
			}
		}
		
		private class StringSerialiser: ISpecialSerialiser<string>
		{
			private class FriendlyBinaryWriter: BinaryWriter
			{
				public FriendlyBinaryWriter(Stream fobj) : base(fobj, Encoding.UTF8, true)
				{
				}
				public void Write7BitEncodedIntPublic(int value)
				{
					Write7BitEncodedInt(value);
				}
			}
			
			private class FriendlyBinaryReader: BinaryReader
			{
				public FriendlyBinaryReader(Stream fobj) : base(fobj, Encoding.UTF8, true)
				{
				}
				public int Read7BitEncodedIntPublic()
				{
					return Read7BitEncodedInt();
				}
			}
			public void SerialiseObject(Stream fobj, string obj)
			{
				using (var write = new FriendlyBinaryWriter(fobj))
				{
					byte[] bytes = null;
					int len;
					if (obj == null)
						len = (int)SpecialSizes.Null;
					else
					{
						bytes = Encoding.UTF8.GetBytes(obj);
						len = bytes.Length;
					}
					write.Write7BitEncodedIntPublic(len);
					if (bytes != null)
						write.Write(bytes);
				}
			}
			
			public string DeserialiseObject(Stream fobj)
			{
				using (var read = new FriendlyBinaryReader(fobj))
				{
					int len = read.Read7BitEncodedIntPublic();
					if (len == (int)SpecialSizes.Null)
						return null;
					return Encoding.UTF8.GetString(read.ReadBytes(len));
				}
			}
		}
		
		private class DateTimeSerialiser: ISpecialSerialiser<DateTime>
		{
			public void SerialiseObject(Stream fobj, DateTime obj)
			{
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
					write.Write(obj.ToBinary());
			}
			
			public DateTime DeserialiseObject(Stream fobj)
			{
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return DateTime.FromBinary(read.ReadInt64());
			}
		}
		
		private class ColorSerialiser: ISpecialSerialiser<Color>
		{
			public void SerialiseObject(Stream fobj, Color obj)
			{
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
					write.Write(obj.ToArgb());
			}
			
			public Color DeserialiseObject(Stream fobj)
			{
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return Color.FromArgb(read.ReadInt32());
			}
		}
		
		private class FontSerialiser: ISpecialSerialiser<Font>
		{
			public void SerialiseObject(Stream fobj, Font obj)
			{
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
				{
					write.Write(obj.FontFamily.Name);
					write.Write(obj.Size);
					write.Write((int)obj.Style);
					write.Write((int)obj.Unit);
					write.Write(obj.GdiCharSet);
					write.Write(obj.GdiVerticalFont);
				}
			}
			
			public Font DeserialiseObject(Stream fobj)
			{
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return new Font(read.ReadString(), read.ReadSingle(), (FontStyle)read.ReadInt32(), (GraphicsUnit)read.ReadInt32(), read.ReadByte(), read.ReadBoolean());
			}
		}
		
		private class ImageSerialiser: ISpecialSerialiser<Image>
		{
			public void SerialiseObject(Stream fobj, Image obj)
			{
				// Images can be saved to Streams but the image data will
				// be "invalid" if the Stream it is written to starts at a
				// non-zero position, so we need to buffer.
				using (var mstr = new MemoryStream())
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
				{
					obj.Save(mstr, ImageFormat.Png);
					mstr.Position = 0;
					write.Write((int)mstr.Length);
					mstr.CopyTo(fobj);
				}
			}
			
			public Image DeserialiseObject(Stream fobj)
			{
				// Images can be loaded from Streams but they must remain
				// open for the lifetime of the Image, so, to avoid needing
				// a handle on the file being loaded from, we buffer on read
				// too.
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return Image.FromStream(new MemoryStream(read.ReadBytes(read.ReadInt32())));
			}
		}
		
		private class StreamSerialiser: ISpecialSerialiser<Stream>
		{
			public void SerialiseObject(Stream fobj, Stream obj)
			{
				using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
				{
					obj.Position = 0;
					write.Write((int)obj.Length);
					obj.CopyTo(fobj);
				}
			}
			
			public Stream DeserialiseObject(Stream fobj)
			{
				using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
					return new MemoryStream(read.ReadBytes(read.ReadInt32()));
			}
		}
		
		private static IOrderedEnumerable<MemberInfo> Members(Type t, SerialisationOptions options)
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
			if (options.HasFlag(SerialisationOptions.NonPublic))
				flags |= BindingFlags.NonPublic;
			if (options.HasFlag(SerialisationOptions.FlattenHierarchy))
				flags |= BindingFlags.FlattenHierarchy;
			var all = t.GetMembers(flags).Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property);
			int i = 0;
			if (options.HasFlag(SerialisationOptions.NonSerialized))
				return all.Where(m => (m.GetCustomAttributes(typeof(NonSerializedAttribute), false).SingleOrDefault() == null)).OrderBy(m => i++);
			else
				return all.Where(m => (m.GetCustomAttributes(typeof(OrderAttribute), false).SingleOrDefault() != null)).OrderBy(m => (m.GetCustomAttributes(typeof(OrderAttribute), false).SingleOrDefault() as OrderAttribute).Order);
		}
		
		private static List<bool> ReadBitfield(Stream fobj, int count)
		{
			if (count == (int)SpecialSizes.Null)
				return null;
			sbyte bit = 7;
			int cur = 0;
			var bitfields = new byte[count / 8 + (count % 8 == 0 ? 0 : 1)];
			var bools = new List<bool>(count);
			fobj.Read(bitfields, 0, bitfields.Length);
			for (int i = 0; i < count; i++)
			{
				bools.Add(((bitfields[cur] >> bit) & 1) == 1);
				bit--;
				if (bit < 0)
				{
					bit = 7;
					cur++;
				}
			}
			return bools;
		}
		
		private static void WriteBitfield(Stream fobj, IEnumerable<bool> bools)
		{
			if (bools == null)
				return;
			sbyte bit = 7;
			int cur = 0;
			int count = bools.Count();
			var bitfields = new byte[count / 8 + (count % 8 == 0 ? 0 : 1)];
			foreach (bool mybool in bools)
			{
				if (mybool)
					bitfields[cur] |= (byte) (1 << bit);
				bit--;
				if (bit < 0)
				{
					bit = 7;
					cur++;
				}
			}
			fobj.Write(bitfields, 0, bitfields.Length);
		}
		
		public static T DeserialiseObject<T>(Stream fobj, SerialisationOptions options = SerialisationOptions.None)
		{
			return (T)DeserialiseObject(typeof(T), fobj, options);
		}
		
		private static object DeserialiseObjectWorker(Type t, Stream fobj, SerialisationOptions options)
		{
#if DEBUG
			Console.WriteLine("Deserialising object of type: " + t.ToString());
#endif
			using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
			{
				// Look for a special serialiser for this type.
				dynamic special = null;
				if (SpecialSerialisers.TryGetValue(t, out special))
				{
					return special.DeserialiseObject(fobj);
				}
				
				if (t.IsEnum)
				{
					return Enum.ToObject(t, DeserialiseObject(Enum.GetUnderlyingType(t), fobj, options));
				}
				
				if (t.IsArray)
				{
					int numelems = read.ReadInt32();
					if (numelems == (int)SpecialSizes.Null)
						return null;
					dynamic reta = Activator.CreateInstance(t, new object[] { numelems });
					Type elemtype = t.GetElementType();
					for (int i = 0; i < numelems; i++)
						reta[i] = (dynamic)DeserialiseObject(elemtype, fobj, options);
					return reta;
				}
				
				if (t.IsGenericType)
				{
					var gent = t.GetGenericTypeDefinition();
					
					if (gent == typeof(Nullable<>))
					{
						bool extant = read.ReadBoolean();
						return extant ? DeserialiseObject(t.GetGenericArguments()[0], fobj, options) : null;
					}
					
					if (gent == typeof(List<>))
					{
						int numelems = read.ReadInt32();
						if (numelems == (int)SpecialSizes.Null)
							return null;
						dynamic retl = Activator.CreateInstance(t, new object[] { numelems });
						Type elemtype = t.GetGenericArguments()[0];
						for (int i = 0; i < numelems; i++)
							retl.Add((dynamic)DeserialiseObject(elemtype, fobj, options));
						return retl;
					}
					
					if (gent == typeof(Dictionary<,>))
					{
						int numpairs = read.ReadInt32();
						if (numpairs == (int)SpecialSizes.Null)
							return null;
						dynamic retd = Activator.CreateInstance(t, new object[] { numpairs });
						Type[] arguments = t.GetGenericArguments();
						for (int i = 0; i < numpairs; i++)
						{
							dynamic key = DeserialiseObject(arguments[0], fobj, options);
							dynamic val = DeserialiseObject(arguments[1], fobj, options);
							retd.Add(key, val);
						}
						return retd;
					}
				}
				
				var readermethod = typeof(BinaryReader).GetMethods().FirstOrDefault(m => m.Name.Length > 4 && m.Name.StartsWith("Read") && m.ReturnType == t);
				if (readermethod != null)
					return readermethod.Invoke(read, new object[] { });
				
				int nummembers = read.ReadInt32();
				if (nummembers == (int)SpecialSizes.Null)
					return null;
				object ret = t.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
				var bools = new List<dynamic>();
				
				foreach (dynamic member in Members(t, options).Take(nummembers))
				{
					Type memt;
					if (member.MemberType == MemberTypes.Field)
						memt = member.FieldType;
					else
						memt = member.PropertyType;
					
					if (memt == typeof(bool))
						bools.Add(member);
					else if (memt == typeof(List<bool>))
						member.SetValue(ret, ReadBitfield(fobj, read.ReadInt32()));
					else if (memt == typeof(bool[]))
						member.SetValue(ret, ReadBitfield(fobj, read.ReadInt32()).ToArray());
					else
						member.SetValue(ret, DeserialiseObject(memt, fobj, options));
				}
				
				if (bools.Count > 0)
				{
					var loaded = ReadBitfield(fobj, bools.Count);
					foreach (var item in bools.Zip(loaded, (m, b) => new { Member = m, Value = b }))
						item.Member.SetValue(ret, item.Value);
				}
				return ret;
			}
		}
		
		public static void SerialiseObject<T>(Stream fobj, T obj, SerialisationOptions options = SerialisationOptions.None)
		{
			Type t = typeof(T);
			if ((t == typeof(object)) && obj != null)
				t = obj.GetType();
			SerialiseObject(t, fobj, obj, options);
		}
		
		private static void SerialiseObjectWorker(Stream fobj, dynamic obj, Type t, SerialisationOptions options)
		{
#if DEBUG
			Console.WriteLine("Serialising object of type: " + t.ToString());
#endif
			using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
			{
				// Look for a special serialiser for this type.
				dynamic special = null;
				if (SpecialSerialisers.TryGetValue(t, out special))
				{
					special.SerialiseObject(fobj, obj);
					return;
				}
				if (t.IsEnum)
				{
					Type realt = Enum.GetUnderlyingType(t);
					SerialiseObject(realt, fobj, Convert.ChangeType(obj, realt), options);
					return;
				}
				
				if (t.IsArray)
				{
					if (obj == null)
					{
						write.Write((int)SpecialSizes.Null);
						return;
					}
					write.Write(obj.Length);
					foreach (dynamic item in obj)
						SerialiseObject(item.GetType(), fobj, item, options);
					return;
				}
				
				if (t.IsGenericType)
				{
					var gent = t.GetGenericTypeDefinition();
					
					if (gent == typeof(Nullable<>))
					{
						bool extant = obj != null;
						write.Write(extant);
						if (extant)
							SerialiseObject(t.GetGenericArguments()[0], fobj, obj, options);
						return;
					}
					
					if (gent == typeof(List<>))
					{
						if (obj == null)
						{
							write.Write((int)SpecialSizes.Null);
							return;
						}
						write.Write(obj.Count);
						foreach (dynamic item in obj)
							SerialiseObject(item.GetType(), fobj, item, options);
						return;
					}
					
					if (gent == typeof(Dictionary<,>))
					{
						if (obj == null)
						{
							write.Write((int)SpecialSizes.Null);
							return;
						}
						write.Write(obj.Count);
						foreach (dynamic pair in obj)
						{
							SerialiseObject(pair.Key.GetType(), fobj, pair.Key, options);
							SerialiseObject(pair.Value.GetType(), fobj, pair.Value, options);
						}
						return;
					}
				}
				
				try
				{
					write.Write(obj); // Will fail if not a primitive type
					return;
				}
				catch
				{
				}
				
				if (obj == null)
				{
					write.Write((int)SpecialSizes.Null);
					return;
				}
				
				if (options.HasFlag(SerialisationOptions.RequireSerializable) && t.GetCustomAttributes(typeof(SerializableAttribute), false).SingleOrDefault() == null)
					throw new SerializationException($"{t} is not serialisable.");
				
				var bools = new List<bool>();
				var members = Members(t, options);
				write.Write(members.Count());
				
				foreach (dynamic member in members)
				{
					Type memt;
					if (member.MemberType == MemberTypes.Field)
						memt = member.FieldType;
					else
						memt = member.PropertyType;
					
					if (memt == typeof(bool))
						bools.Add(member.GetValue(obj));
					else if (memt == typeof(List<bool>) || memt == typeof(bool[]))
					{
						var val = member.GetValue(obj) as IEnumerable<bool>;
						if (val == null)
							write.Write((int)SpecialSizes.Null);
						else
						{
							write.Write(val.Count());
							WriteBitfield(fobj, val.ToList());
						}
					}
					else
					{
						dynamic val = member.GetValue(obj);
						SerialiseObject(memt, fobj, val, options);
					}
					
				}
				
				WriteBitfield(fobj, bools);
			}
		}
		
		public static void SerialiseObject(Type t, Stream fobj, dynamic obj, SerialisationOptions options = SerialisationOptions.None)
		{
			try
			{
				SerialiseObjectWorker(fobj, obj, t, options);
			}
			catch (Exception ex)
			{
				try
				{
					ex.Data.Add("Whoa: Type", t);
					ex.Data.Add("Whoa: Object", obj);
				}
				catch // Stifle this exception and throw the important one
				{
				}
				throw;
			}
		}
		
		[Obsolete("This argument order is weird.")]
		public static void SerialiseObject(Stream fobj, dynamic obj, Type t)
		{
			SerialiseObject(t, fobj, obj);
		}
		
		public static object DeserialiseObject(Type t, Stream fobj, SerialisationOptions options = SerialisationOptions.None)
		{
			try
			{
				return DeserialiseObjectWorker(t, fobj, options);
			}
			catch (Exception ex)
			{
				try
				{
					ex.Data.Add("Whoa: Type", t);
				}
				catch
				{
				}
				throw;
			}
		}
	}
}
