using System;

namespace Whoa
{
	/// <summary>
	/// Options for Whoa. These need to match on serialisation and
	/// deserialisation.
	/// </summary>
	[Flags]
	public enum SerialisationOptions
	{
		/// <summary>
		/// Use the default options: serialise all public class members
		/// that have an OrderAttribute.
		/// </summary>
		None = 0,
		
		/// <summary>
		/// Serialise all class members that don't have a
		/// NonSerializedAttribute. This is experimental. The .NET
		/// specification does not guarantee the order of members
		/// obtained using reflection but both Microsoft .NET Framework
		/// and Mono seem to return them in declaration order.
		/// If this flag is not set, only class members with an
		/// OrderAttribute will be serialised.
		/// </summary>
		NonSerialized = 1,
		
		/// <summary>
		/// Require a SerializableAttribute for unrecognised types.
		/// By default, all objects can be serialised.
		/// </summary>
		RequireSerializable = 2,
		
		/// <summary>
		/// Serialise non-public members.
		/// By default, only public members will be serialised.
		/// </summary>
		NonPublic = 4,
		
		/// <summary>
		/// Serialise protected and public members of parent classes.
		/// By default, only members of the exact class of the object
		/// will be serialised.
		/// </summary>
		FlattenHierarchy = 8,
	}
}
