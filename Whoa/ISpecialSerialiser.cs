using System.IO;

namespace Whoa
{
	public interface ISpecialSerialiser<T>
	{
		void SerialiseObject(Stream fobj, T obj);
		T DeserialiseObject(Stream fobj);
	}
	
	interface ISpecialSerialiser
	{
		void SerialiseObject(Stream fobj, object obj);
		object DeserialiseObject(Stream fobj);
	}
	
	class UngenericSpecialSerialiser<T> : ISpecialSerialiser
	{
		ISpecialSerialiser<T> underlying;
		public UngenericSpecialSerialiser(ISpecialSerialiser<T> underlying)
		{
			this.underlying = underlying;
		}
		public void SerialiseObject(Stream fobj, object obj)
		{
			underlying.SerialiseObject(fobj, (T)obj);
		}
		public object DeserialiseObject(Stream fobj)
		{
			return underlying.DeserialiseObject(fobj);
		}
	}
}
