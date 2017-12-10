using System.IO;

namespace Whoa
{
	public interface ISpecialSerialiser<T>
	{
		void SerialiseObject(Stream fobj, T obj);
		T DeserialiseObject(Stream fobj);
	}
}
