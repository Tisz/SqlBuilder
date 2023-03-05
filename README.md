# SqlBuilder
A SQL query builder library for C# to help you ditch your hard coded queries.

```cs
SqlQuery query = new SqlQuery();

query.AddColumn("Name")
    .From("Customers")
    .Where("LastName", "Smith", useQuotes: true);

Debug.Print(query.GetQuery());
```

```sql
SELECT Name 
FROM Customers 
WHERE LastName = 'Smith' 
```

### ✨Extra SQL filter types
```cs
var myGuidList = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() };
query.AddColumn("StockOnHand")
    .From("Items")
    .WhereIn("ID", myGuidList);
```

```sql
SELECT StockOnHand 
FROM Items 
WHERE ID IN ('ea99e4d7-eefe-42c8-9ea5-fca341b8a33e', '675725a9-9703-4b9d-8af1-507427d54a34') 
```

### ✨Easy passing of conditions, and changing the comparison
```cs
query.AddColumn("Name")
    .From("Customers")
    .Where("Created", DateTime.Now.AddYears(-1), Comparisons.Greater);
```

```sql
SELECT Name, LastName 
FROM Customers 
WHERE DOB >= @DOBStart AND DOB <= @DOBEnd 
```

### ✨Query joins

```cs
query.AddColumn("C", "Name")
    .AddColumn("I", "ItemName")
    .From("Customers", "C")
    .Join(JoinTypes.Inner, "SaleItem", "SI", "CustomerId", "C", "Id")
    .Join(JoinTypes.Inner, "Item", "I", "Id", "SI", "ItemId");
```

```sql
SELECT C.Name, I.ItemName
FROM Customers C
INNER JOIN SaleItem SI ON SI.CustomerId = C.Id
INNER JOIN Item I ON I.Id = SI.ItemId 
```

### ✨Sub query support
```cs
SqlQuery subQuery = new SqlQuery();
subQuery.AddColumn("ID")
    .From("Items")
    .Where("Created", DateTime.Now.AddYears(-1), Comparisons.Lesser);

SqlQuery mainQuery = new SqlQuery();
mainQuery.AddColumn("*")
    .From("SaleItem")
    .WhereIn("ItemId", subQuery);
```

```sql
SELECT * 
FROM SaleItem 
WHERE ItemId IN (
  SELECT ID 
  FROM Items 
  WHERE Created < '2022-03-05 11:52:05.283'
) 
```

### ✨Access to quick methods for commonly filters

Date range filter for looking between two dates.
And supports SQL Parameters.

```cs
query.AddColumn("Name")
    .AddColumn("LastName")
    .From("Customers")
    .WhereBetween("DOB", "@DOBStart", "@DOBEnd");
query.AddParameter("@DOBStart", DateTime.Now.AddYears(-2));
query.AddParameter("@DOBEnd", DateTime.Now.AddYears(-1));
```

```sql
SELECT Name, LastName
FROM Customers
WHERE DOB >= @DOBStart AND DOB <= @DOBEnd
```

### ✨Insert Queries

```cs
query.InsertInto("ID", "FirstName", "LastName", "DOB")
    .InsertIntoTable("Customers")
    .InsertIntoValues(Guid.NewGuid(), "Joe", "Doe", DateTime.Now);
```

```sql
INSERT INTO Customers (ID, FirstName, LastName, DOB) 
VALUES ('d98aa670-ac9f-4e17-9f56-3a4963cff7cf', 'Joe', 'Doe', CONVERT(datetime, '05/03/2023 11:42:42', 103))
```

### ✨Update Queries

```cs
query.UpdateColumn("FirstName", "Harry", true)
    .UpdateTable("Customers")
    .Where("ID", Guid.NewGuid());
```

```sql
UPDATE Customers 
SET FirstName = 'Harry' 
WHERE ID = 'cbafef14-d31d-4512-97b2-d8b90405a039' 
```
