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

namespace Whoa
{
	public static class Whoa
	{		
		private class SpecialSerialiserAttribute: Attribute
		{
			public Type t { get; private set; }
			public SpecialSerialiserAttribute(Type in_t)
			{
				t = in_t;
			}
		}
		
		private class SpecialDeserialiserAttribute: Attribute
		{
			public Type t { get; private set; }
			public SpecialDeserialiserAttribute(Type in_t)
			{
				t = in_t;
			}
		}
		
		[SpecialSerialiser(typeof(Guid))]
		private static void SerialiseGuid(Stream fobj, dynamic obj)
		{
			fobj.Write(obj.ToByteArray(), 0, 16);
		}
		
		[SpecialDeserialiser(typeof(Guid))]
		private static object DeserialiseGuid(Stream fobj)
		{
			var guid = new byte[16];
			fobj.Read(guid, 0, 16);
			return new Guid(guid);
		}
		
		[SpecialSerialiser(typeof(string))]
		private static void SerialiseString(Stream fobj, dynamic obj)
		{
			using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
			{
				byte[] bytes = null;
				int len;
				if (obj == null)
					len = -1;
				else
				{
					bytes = Encoding.UTF8.GetBytes(obj);
					len = bytes.Length;
				}
				typeof(BinaryWriter).GetMethod("Write7BitEncodedInt", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(write, new object[] { len });
				if (bytes != null)
					fobj.Write(bytes, 0, len);
			}
		}
		
		[SpecialDeserialiser(typeof(string))]
		private static object DeserialiseString(Stream fobj)
		{
			using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
			{
				int len = (int)typeof(BinaryReader).GetMethod("Read7BitEncodedInt", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(read, new object[] { });
				if (len < 0)
					return null;
				else
				{
					var bytes = new byte[len];
					fobj.Read(bytes, 0, len);
					return Encoding.UTF8.GetString(bytes);
				}
			}
		}
		
		private static IOrderedEnumerable<MemberInfo> Members(Type t)
		{
			return t.GetProperties().Select(m => m as MemberInfo).Concat(t.GetFields().Select(m => m as MemberInfo)).Where(m => (m.GetCustomAttributes(typeof(OrderAttribute), false).SingleOrDefault() as OrderAttribute) != null).OrderBy(m => (m.GetCustomAttributes(typeof(OrderAttribute), false).SingleOrDefault() as OrderAttribute).Order);
		}
		
		private static List<bool> ReadBitfield(Stream fobj, int count)
		{
			if (count < 0)
				return null;
			sbyte bit = 7;
			int cur = 0;
			var bitfields = new byte[count / 8 + 1];
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
			var bitfields = new byte[bools.Count() / 8 + 1];
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
		
		public static T DeserialiseObject<T>(Stream fobj)
		{
			return (T)DeserialiseObject(typeof(T), fobj);
		}
		
		public static object DeserialiseObject(Type t, Stream fobj)
		{
			using (var read = new BinaryReader(fobj, Encoding.UTF8, true))
			{
				if (t.IsGenericType)
				{
					var gent = t.GetGenericTypeDefinition();
					
					if (gent == typeof(Nullable<>))
					{
						bool extant = read.ReadBoolean();
						return extant ? DeserialiseObject(t.GetGenericArguments()[0], fobj) : null;
					}
					
					if (gent == typeof(List<>))
					{
						int numelems = read.ReadInt32();
						if (numelems < 0)
							return null;
						dynamic retl = Activator.CreateInstance(t, new object[] { numelems });
						Type elemtype = t.GetGenericArguments()[0];
						for (int i = 0; i < numelems; i++)
							retl.Add((dynamic)DeserialiseObject(elemtype, fobj));
						return retl;
					}
					
					if (gent == typeof(Dictionary<,>))
					{
						int numpairs = read.ReadInt32();
						if (numpairs < 0)
							return null;
						dynamic retd = Activator.CreateInstance(t, new object[] { numpairs });
						Type[] arguments = t.GetGenericArguments();
						for (int i = 0; i < numpairs; i++)
						{
							dynamic key = DeserialiseObject(arguments[0], fobj);
							dynamic val = DeserialiseObject(arguments[1], fobj);
							retd.Add(key, val);
						}
						return retd;
					}
				}
				
				// A little self reflection.
				var specialmethod = typeof(Whoa).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(m =>
				{
					var attr = m.GetCustomAttributes(typeof(SpecialDeserialiserAttribute), false).SingleOrDefault() as SpecialDeserialiserAttribute;
					if (attr == null)
						return false;
					return attr.t == t;
				});
				
				if (specialmethod != null)
					return specialmethod.Invoke(null, new object[] { fobj });
				
				var readermethod = typeof(BinaryReader).GetMethods().FirstOrDefault(m => m.Name.Length > 4 && m.Name.StartsWith("Read") && m.ReturnType == t);
				if (readermethod != null)
					return readermethod.Invoke(read, new object[] { });
				
				int nummembers = read.ReadInt32();
				if (nummembers < 0)
					return null;
				object ret = t.GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
				var bools = new List<dynamic>();
				
				foreach (dynamic member in Members(t).Take(nummembers))
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
						member.SetValue(ret, DeserialiseObject(memt, fobj));
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
		
		public static void SerialiseObject(Stream fobj, dynamic obj, Type t)
		{
			using (var write = new BinaryWriter(fobj, Encoding.UTF8, true))
			{
				if (t.IsGenericType)
				{
					var gent = t.GetGenericTypeDefinition();
					
					if (gent == typeof(Nullable<>))
					{
						bool extant = obj != null;
						write.Write(extant);
						if (extant)
							SerialiseObject(fobj, obj, t.GetGenericArguments()[0]);
						return;
					}
					
					if (gent == typeof(List<>))
					{
						if (obj == null)
						{
							write.Write(-1);
							return;
						}
						write.Write(obj.Count);
						foreach (dynamic item in obj)
							SerialiseObject(fobj, item);
						return;
					}
					
					if (gent == typeof(Dictionary<,>))
					{
						if (obj == null)
						{
							write.Write(-1);
							return;
						}
						write.Write(obj.Count);
						foreach (dynamic pair in obj)
						{
							SerialiseObject(fobj, pair.Key);
							SerialiseObject(fobj, pair.Value);
						}
						return;
					}
				}
				
				var specialmethod = typeof(Whoa).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(m =>
				{
					var attr = m.GetCustomAttributes(typeof(SpecialSerialiserAttribute), false).SingleOrDefault() as SpecialSerialiserAttribute;
					if (attr == null)
						return false;
					return attr.t == t;
				});
				
				if (specialmethod != null)
				{
					specialmethod.Invoke(null, new object[] { fobj, obj });
					return;
				}
				
				try
				{
					write.Write(obj); // Will fail if not an integral type
					return;
				}
				catch
				{
				}
				
				if (obj == null)
				{
					write.Write(-1);
					return;
				}
				
				var bools = new List<bool>();
				var members = Members(t);
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
							write.Write(-1);
						else
						{
							write.Write(val.Count());
							WriteBitfield(fobj, val.ToList());
						}
					}
					else
					{
						dynamic val = member.GetValue(obj);
						SerialiseObject(fobj, val, memt);
					}
					
				}
				
				WriteBitfield(fobj, bools);
				
			}
		}
		
		public static void SerialiseObject(Stream fobj, dynamic obj)
		{
			SerialiseObject(fobj, obj, obj.GetType());
		}
	}
}
