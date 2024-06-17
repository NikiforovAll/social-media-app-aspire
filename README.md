# Learning .NET Aspire by example: Social Media App using Postgres, MongoDb, Elasticsearch

The goal of this repository is to show how to build applications using .NET Aspire.

Components:

* `Aspire.Hosting.PostgreSQL` - users & followers
* `Aspire.Hosting.MongoDB` - posts
* `Aspire.Hosting.Elasticsearch` - post searching & likes analytics
* `Aspire.Hosting.Redis` - output caching
* `Aspire.Hosting.RabbitMQ` - message bus, used to denormalize data to Elastic

## Domain

For more information about implemented functionality see [REST API document](./docs/domain.md).

## Databases

The reasoning for each type of data storage:

1. **Relational Databases (PostgreSQL):**

   **Motivation & Reasoning:** Relational databases are designed to handle structured data and relationships between data entities effectively. They are based on a relational model where data is stored in tables and the relationship between these data is also stored in tables. For a social media application, user profiles and the relationships between users (like who follows whom) are well-suited to a relational model.

   **Pros:**
   - Strong consistency and ACID (Atomicity, Consistency, Isolation, Durability) compliance which ensures reliable processing of transactions.
   - Excellent support for complex queries and joins due to SQL (Structured Query Language).
   - Mature, with plenty of tools, libraries, and resources available.

   **Cons:**
    - Can become slower as the volume of data increases.
    - Scaling horizontally (across multiple servers) can be challenging.
    - They can be overkill for simple, non-relational data.

2. **NoSQL Databases (MongoDB):**

   **Motivation & Reasoning:** NoSQL databases are designed to handle unstructured data, and they excel at dealing with large volumes of data and high write loads. They don't require a fixed schema and are easy to scale. For a social media application, posts and likes can be considered as document-like data and can be stored effectively in a NoSQL database.

   **Pros:**
   - Schema-less, which offers flexibility as data requirements evolve.
   - Generally provide easy horizontal scaling.
   - Good performance with large amounts of data.

   **Cons:**
   - Lack of standardization as compared to SQL databases.
   - Not all NoSQL databases support ACID transactions.
   - Joins and complex queries can be more difficult or not natively supported.

3. **Search and Analytics Engines (Elasticsearch):**

   **Motivation & Reasoning:** Elasticsearch is a real-time distributed search and analytics engine. It's designed for horizontal scalability, maximum reliability, and easy management. It excels at searching complex data types. For a social media application, Elasticsearch can be used to index posts and provide powerful search capabilities.

   **Pros:**
   - Excellent full-text search capabilities with a powerful query language.
   - Real-time analytics.
   - Can handle large amounts of data and scale horizontally easily.

   **Cons:**
   - Not designed to be a primary database, more suited for secondary read-heavy workloads.
   - Managing and maintaining an Elasticsearch cluster can be complex.
   - No built-in multi-document ACID transactions.

In summary, the choice of database depends on the specific needs of your application. It's common to use a combination of different types of databases (polyglot persistence) to leverage the strengths of each.
