# Whoa

## About
Whoa is a serialisation library for .NET. Its output is ultra small and
you can add members to your type and still be able to deserialise old
versions. Since it stores the bare minimum of type data, it is a bit
more finicky than other serialisation solutions like BinaryFormatter,
but produces much smaller output.

The format may change in non-backwards-compatible ways sometimes as new
features are added. To make sure your data can be deserialised, use the
same version of Whoa for serialisation and deserialisation. That said,
whenever feasible, backwards compatibility will be preserved, and
eventually the format will be finalised.

## Usage

Whoa is static and stateless. To serialise an object:

`Whoa.Whoa.SerialiseObject(outstream, object[, options]);`

... and to deserialise it:

`Whoa.Whoa.DeserialiseObject<type>(instream[, options]);`

There are also reflection-friendly interfaces to these functions:

`Whoa.Whoa.SerialiseObject(type, outstream, object[, options]);`

`Whoa.Whoa.DeserialiseObject(type, instream[, options]);`

If you'd like to take control of the serialisation of a particular
class, you should define a derivative of ISpecialSerialiser, instantiate
it, and pass it to Whoa with:

`Whoa.Whoa.RegisterSpecialSerialiser(instance);`

ISpecialSerialiser is a generic interface. Your derivative should fill
in the type argument with the type you want to handle. Whoa contains
inbuilt special serialisers for:

* System.Guid
* System.BigInteger
* System.String
* System.DateTime
* System.Drawing.Color
* System.Drawing.Font
* System.Drawing.Image
* System.IO.Stream

It will also handle arrays, Lists, and Dictionaries of types it knows
automatically, and numeric types and enums are passed to BinaryWriter.

By default, Whoa requires the use of an OrderAttribute on each member
of your class that you would like serialised, and it will only touch
public members. It is also perfectly happy to serialise classes that do
not have a SerializableAttribute. These characteristics can be changed
by passing flags from the SerialisationOptions enum.
Of particular note is the NonSerialized mode. This will remove the
requirement for Order attributes and instead (de)serialise the members
in the order they are received from GetMembers(). The .NET documentation
states that this order is arbitrary, but on both Microsoft's framework
and Mono, it seems to always match declaration order, so it's not
currently a practical issue. In this mode, you can exclude members from
serialisation by giving them a NonSerializedAttribute.
Also, remember that the options will need to be the same on
serialisation and deserialisation.

## TODO

* Reference semantics
* Remembering derived type information
