using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Thrift;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Transport;
using Thrift.Transport.Client;

namespace UniversalThriftClientConsoleApp;

public class UniversalThriftClient
{
    private readonly TTransport _transport;
    private readonly TBinaryProtocol _protocol;

    public UniversalThriftClient(string url)
    {
        _transport = new THttpTransport(new Uri(url), new TConfiguration
        {
            MaxMessageSize = int.MaxValue
        });
        _protocol = new TBinaryProtocol(_transport);
    }

    public async Task<T> CallMethodAsync<T>(string methodName, params object[] args)
    {
        await _transport.OpenAsync();

        await _protocol.WriteMessageBeginAsync(new TMessage(methodName, TMessageType.Call, 0), default);

        if (args.Length > 0)
        {
            await _protocol.WriteStructBeginAsync(new TStruct($"{methodName}_args"), default);
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var fieldType = GetType(arg);
                await _protocol.WriteFieldBeginAsync(new TField($"arg{i + 1}", fieldType, (short)(i + 1)), default);
                await WriteValueAsync(_protocol, arg);
                await _protocol.WriteFieldEndAsync(default);
            }
            await _protocol.WriteFieldStopAsync(default);
            await _protocol.WriteStructEndAsync(default);
        }

        await _protocol.WriteMessageEndAsync(default);

        try
        {
            await _protocol.Transport.FlushAsync(default);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }

        var response = await ThriftDeserializer.DeserializeAsync(_protocol);
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(response.field_0));
    }

    private TType GetType(object value)
    {
        if (value == null) return TType.Void;

        switch (value)
        {
            case int _: return TType.I32;
            case long _: return TType.I64;
            case string _: return TType.String;
            case bool _: return TType.Bool;
            case double _: return TType.Double;
            case byte _: return TType.Byte;
            case short _: return TType.I16;
            case IDictionary _: return TType.Map;
            case HashSet<object> _: return TType.Set;
            case IEnumerable _ when !(value is string): return TType.List;
            default: return TType.Struct;
        }
    }

    private async Task WriteValueAsync(TProtocol protocol, object value)
    {
        if (value == null)
        {
            return;
        }

        switch (value)
        {
            case int i: await protocol.WriteI32Async(i); break;
            case long l: await protocol.WriteI64Async(l); break;
            case string s: await protocol.WriteStringAsync(s); break;
            case bool b: await protocol.WriteBoolAsync(b); break;
            case double d: await protocol.WriteDoubleAsync(d); break;
            case byte b: await protocol.WriteByteAsync((sbyte)b); break;
            case short s: await protocol.WriteI16Async(s); break;
            case IDictionary map: await WriteMapAsync(protocol, map); break;
            case IEnumerable list when value is HashSet<object>: await WriteSetAsync(protocol, (IEnumerable)value); break;
            case IEnumerable list: await WriteListAsync(protocol, list); break;
            default: await WriteStructAsync(protocol, value); break;
        }
    }

    private async Task WriteStructAsync(TProtocol protocol, object obj)
    {
        await protocol.WriteStructBeginAsync(new TStruct(obj.GetType().Name));
        var properties = obj.GetType().GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttributes<JsonPropertyNameAttribute>().FirstOrDefault()
            })
            .Where(x => x.Attribute != null && x.Attribute.Name.StartsWith("field_"))
            .OrderBy(x => int.Parse(x.Attribute.Name.Replace("field_", "")))
            .ToList();

        foreach (var prop in properties)
        {
            var fieldId = short.Parse(prop.Attribute.Name.Replace("field_", ""));
            var value = prop.Property.GetValue(obj);
            var fieldType = GetType(value);
            await protocol.WriteFieldBeginAsync(new TField(prop.Property.Name, fieldType, fieldId));
            await WriteValueAsync(protocol, value);
            await protocol.WriteFieldEndAsync();
        }

        await protocol.WriteFieldStopAsync();
        await protocol.WriteStructEndAsync();
    }

    private async Task WriteMapAsync(TProtocol protocol, IDictionary map)
    {
        if (map.Count == 0)
        {
            await protocol.WriteMapBeginAsync(new TMap(TType.Void, TType.Void, 0));
            await protocol.WriteMapEndAsync();
            return;
        }

        var firstKey = map.Keys.Cast<object>().First();
        var firstValue = map.Values.Cast<object>().First();
        var keyType = GetType(firstKey);
        var valueType = GetType(firstValue);

        await protocol.WriteMapBeginAsync(new TMap(keyType, valueType, map.Count));
        foreach (DictionaryEntry entry in map)
        {
            await WriteValueAsync(protocol, entry.Key);
            await WriteValueAsync(protocol, entry.Value);
        }
        await protocol.WriteMapEndAsync();
    }

    private async Task WriteListAsync(TProtocol protocol, IEnumerable list)
    {
        var elementType = TType.Void;
        var count = 0;
        foreach (var item in list)
        {
            elementType = GetType(item);
            count++;
        }

        await protocol.WriteListBeginAsync(new TList(elementType, count));
        foreach (var item in list)
        {
            await WriteValueAsync(protocol, item);
        }
        await protocol.WriteListEndAsync();
    }

    private async Task WriteSetAsync(TProtocol protocol, IEnumerable set)
    {
        var elementType = TType.Void;
        var count = 0;
        foreach (var item in set)
        {
            elementType = GetType(item);
            count++;
        }

        await protocol.WriteSetBeginAsync(new TSet(elementType, count));
        foreach (var item in set)
        {
            await WriteValueAsync(protocol, item);
        }
        await protocol.WriteSetEndAsync();
    }
}
