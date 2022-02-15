```
const string Csv = "123, \"456,\"\"789\"\"\" ,ABC";
var parser = new CsvParser(new StringReader(Csv));
 
var expectedRecord = new[]
{
    "123",
    "456,\"789\"",
    "ABC"
};
 
Assert.IsTrue(parser.TryReadRecord(out string[] record));
CollectionAssert.AreEqual(expectedRecord, record);
Assert.IsFalse(parser.TryReadRecord(out _));
```

```
const string Csv = "123<Foo <FooB456<Foo789<FooB <FooABC<FooBar";
var parser = new CsvParser(new StringReader(Csv), new("<Foo", "<FooBar", "<FooB"));
 
var expectedRecord = new[]
{
    "123",
    "456<Foo789",
    "ABC"
};
 
Assert.IsTrue(parser.TryReadRecord(out string[] record));
CollectionAssert.AreEqual(expectedRecord, record);
Assert.IsFalse(parser.TryReadRecord(out _));
```