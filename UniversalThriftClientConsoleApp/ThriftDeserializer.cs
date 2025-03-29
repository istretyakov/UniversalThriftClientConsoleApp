using System.Dynamic;
using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Protocol.Utilities;

namespace UniversalThriftClientConsoleApp;

public class ThriftDeserializer
{
    public static async Task<dynamic> DeserializeAsync(TProtocol protocol)
    {
        return await ParseMessageAsync(protocol);
    }

    private static async Task<dynamic> ParseMessageAsync(TProtocol protocol)
    {
        var message = await protocol.ReadMessageBeginAsync();
        if (message.Type == TMessageType.Exception)
        {
            var ex = new TApplicationException();
            throw new Exception($"Thrift error: {ex.Message}");
        }

        var result = await ParseStructAsync(protocol);
        await protocol.ReadMessageEndAsync();
        return result;
    }

    private static async Task<IDictionary<string, object>> ParseStructAsync(TProtocol protocol)
    {
        var obj = new ExpandoObject() as IDictionary<string, object>;
        await protocol.ReadStructBeginAsync();
        while (true)
        {
            var field = await protocol.ReadFieldBeginAsync();
            if (field.Type == TType.Stop)
                break;

            obj[$"field_{field.ID}"] = await ReadValueAsync(protocol, field.Type);
            await protocol.ReadFieldEndAsync();
        }
        await protocol.ReadStructEndAsync();
        return obj;
    }

    private static async Task<object> ReadValueAsync(TProtocol protocol, TType type)
    {
        switch (type)
        {
            case TType.Bool: return await protocol.ReadBoolAsync();
            case TType.Byte: return await protocol.ReadByteAsync();
            case TType.I16: return await protocol.ReadI16Async();
            case TType.I32: return await protocol.ReadI32Async();
            case TType.I64: return await protocol.ReadI64Async();
            case TType.Double: return await protocol.ReadDoubleAsync();
            case TType.String: return await protocol.ReadStringAsync();
            case TType.Struct: return await ParseStructAsync(protocol);
            case TType.Map: return await ParseMapAsync(protocol);
            case TType.Set: return await ParseSetAsync(protocol);
            case TType.List: return await ParseListAsync(protocol);
            default: await TProtocolUtil.SkipAsync(protocol, type, default); return null;
        }
    }

    private static async Task<IDictionary<object, object>> ParseMapAsync(TProtocol protocol)
    {
        var map = new Dictionary<object, object>();
        var mapHeader = await protocol.ReadMapBeginAsync();
        for (int i = 0; i < mapHeader.Count; i++)
        {
            var key = await ReadValueAsync(protocol, mapHeader.KeyType);
            var value = await ReadValueAsync(protocol, mapHeader.ValueType);
            map[key] = value;
        }
        await protocol.ReadMapEndAsync();
        return map;
    }

    private static async Task<List<object>> ParseListAsync(TProtocol protocol)
    {
        var list = new List<object>();
        var listHeader = await protocol.ReadListBeginAsync();
        for (int i = 0; i < listHeader.Count; i++)
        {
            list.Add(await ReadValueAsync(protocol, listHeader.ElementType));
        }
        await protocol.ReadListEndAsync();
        return list;
    }

    private static async Task<HashSet<object>> ParseSetAsync(TProtocol protocol)
    {
        var set = new HashSet<object>();
        var setHeader = await protocol.ReadSetBeginAsync();
        for (int i = 0; i < setHeader.Count; i++)
        {
            set.Add(await ReadValueAsync(protocol, setHeader.ElementType));
        }
        await protocol.ReadSetEndAsync();
        return set;
    }
}
