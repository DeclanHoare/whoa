# Whoa

## About
Whoa is a serialisation library for C#. Its output is ultra small and
you can add members to your type and still be able to deserialise old
versions. Since it stores the bare minimum of type data, it is a bit
more finicky than other serialisation solutions like BinaryFormatter,
but produces much smaller output.

## Usage

For Whoa to produce meaningful output, your type will need to have a
public constructor with no arguments, and every field or property that
you would like to save needs to be public (properties must have a public
getter AND a public setter) and have a Whoa.OrderAttribute. Also, if you
want to preserve backwards compatibility, don't rearrange or remove
any of the members with OrderAttributes. You can, however, add new ones
to the end of your class without issue.

To serialise an object:
`Whoa.Whoa.SerialiseObject(outstream, obj);`

To deserialise an object:
`Whoa.Whoa.DeserialiseObject<T>(instream);`


