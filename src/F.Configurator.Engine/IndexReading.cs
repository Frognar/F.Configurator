using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

// One way of reading the start of an index: the values read so far and the text not read yet.
internal sealed record IndexReading(ImmutableList<(string Feature, Value Value)> Values, string Unread)
{
    public IndexReading Read(string feature, Value value, int length) =>
        new(Values.Add((feature, value)), Unread[length..]);

    public IndexReading Skip(int length) => this with { Unread = Unread[length..] };
}
