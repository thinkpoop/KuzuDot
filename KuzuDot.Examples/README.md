# KuzuDot Examples

This project contains examples demonstrating the usage of KuzuDot.

## Prerequisites

- .NET 8.0 or later
- KuzuDB native library (`kuzu_shared.dll` for Windows)

## Running the Examples

### OUsing Visual Studio
1. Set `KuzuDot.Examples` as startup project
2. Press F5 to run


## Sample Data Schemas

### Social Network
```cypher
// Nodes
User(id, name, email)
Post(id, title, content, timestamp)

// Relationships  
User -[:Follows {since}]-> User
User -[:Authored]-> Post
User -[:Likes {timestamp}]-> Post
```

### Corporate Hierarchy
```cypher
// Nodes
Company(id, name, industry)
Employee(id, name, role)

// Relationships
Employee -[:WorksFor {start_date, salary}]-> Company
Employee -[:Manages {since}]-> Employee
```

## API Usage Examples

### Basic Database Setup
```csharp
using var database = new Database(":memory:");
using var connection = database.Connect();
using var result = connection.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name))");
```

### Prepared Statements
```csharp
using var statement = connection.Prepare("CREATE (:Person {name: $name, age: $age})");
statement.BindString("name", "Alice");
statement.BindInt64("age", 30);
using var result = statement.Execute();
```

### Complex Queries
```csharp
using var result = connection.Query(@"
    MATCH (follower:User)-[:Follows]->(author:User)-[:Authored]->(post:Post)
    RETURN follower.name, author.name, post.title
");
```

## Resources

- [KuzuDB Documentation](https://kuzudb.com/)
- [Cypher Query Language Reference](https://docs.kuzudb.com/cypher/)
- [KuzuDot Source Code](../KuzuDot/)
