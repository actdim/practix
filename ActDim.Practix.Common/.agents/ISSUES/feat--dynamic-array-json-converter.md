---
slug: dynamic-array-json-converter
type: feat
status: open
priority: medium
created: 2026-08-17
updated: 2026-08-17
---

# Feature Issue: Evaluate DynamicArray wrapper for JSON array deserialization in ObjectJsonConverter

## Description
In `ObjectJsonConverter`, JSON arrays are currently deserialized to a standard `List<object>`.
An alternative implementation using a `DynamicArray` wrapper (`DynamicObject`) was proposed to enable DLR indexer (`[0]`) and property access (`.Count`, `.Length`) on dynamic JSON array targets.

## Extracted Alternative Code Snippet
```csharp
// Alternative array deserialization in ObjectJsonConverter:
private static DynamicArray ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
{
    var list = new List<object>();
    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        list.Add(ReadValue(ref reader, options));
    return new DynamicArray(list);
}

// Alternative DLR wrapper for dynamic array indexing and member resolution:
public class DynamicArray : DynamicObject, IEnumerable<object>
{
    private readonly List<object> _items;

    public DynamicArray(List<object> items) => _items = items;

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        result = _items[(int)indexes[0]];
        return true;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = binder.Name switch
        {
            "Count" => _items.Count,
            "Length" => _items.Count,
            _ => null
        };
        return result != null;
    }

    public int Count => _items.Count;
    public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
```

## Objectives & Acceptance Criteria
- [ ] Evaluate performance overhead of `DynamicArray` (`DynamicObject`) vs `List<object>`.
- [ ] Compare API usability with DLR dynamic calls in consumer code.
- [ ] Add unit tests verifying indexer and property access behavior if adopted.
