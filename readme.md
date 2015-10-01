# Data Helper

A library of methods for assisting in managing data and object collections

##### Take a list and return a datatable
```
var dataTable = listOfObjects.ToDataTable();
```

##### Convert a list of items to CSV
```
var csvString = listOfObjects.ToCsv(); 
```

##### Serialize an object to binary
```
var binaryString = object.SerializeToBinary();
```

##### Deserialize an object from binary
```
var object = binaryString.DeSerializeFromBinary();
```

##### Serialize an object to XML
```
var xmlString = object.SerializeToXML();
```

##### Deserialize an object from XML
```
var object = xmlString.DeserializeFromXML();
```

##### SQL Bulk Copy helper
```
//Using SQLConnection
sqlConnection.SqlBulkCopy(listOfObjects);
//Using Entity Context
((SqlConnection)context.Database.Connection).SqlBulkCopy(listOfObjects);
//Override optional tableName for the name of the database table
```

##### Shuffle a collection
```
var shuffledCollection = listOfObjects.Shuffle(new Random);
```

##### Traverse a collection (Recursion)
```
var hierarchyCollection = listOfObjectsWithParentId.Where(x => x.id == idOfParentNode).Traverse(x => x.object);
```

##### Split a collection into smaller collections
```
var newCollectionOfCollections = listOfObjects.Split(10);
```

##### Clone an object
```
var clonedObject = object.Clone();
```

##### Logical sorting of items
```
var sortedCollection = collection.OrderBy(o => o.PropertyToOrderBy, new OrderComparer());
```
