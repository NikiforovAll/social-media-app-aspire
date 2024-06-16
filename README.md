# Learning .NET Aspire by example: Social Media App using Postgres, MongoDb, Elasticsearch

The goal of this repository is to show how to build Cloud Native applications using .NET Aspire.

## Domain

A social media application is a platform where users can create and share content or participate in social networking. The main entities in such an application typically include:

**User**: This is an individual who uses the social media application. They have attributes such as a unique identifier (UserID), a name, an email address, etc.

**Post**: This is a piece of content created by a user. It has attributes such as a unique identifier (PostID), a title, a body, and a reference to the user who created it (Author).

**Follow**: This represents the action of one user (the follower) choosing to see the posts of another user (the followed) in their feed. It is a relationship between two users.

**Like**: This represents the action of a user showing appreciation for a post. It is a relationship between a user and a post.

### REST API

RESTful API specification:

1. **Users**
   - **GET /users:** Returns a list of all users.
   - **GET /users/{id}:** Returns the user with the specified ID.
   - **POST /users:** Creates a new user. Expects a JSON body with the user details.
   - **PUT /users/{id}:** Updates the user with the specified ID. Expects a JSON body with the new user details.
   - **DELETE /users/{id}:** Deletes the user with the specified ID.
2. **Posts**
   - **GET users/{id}/posts:** Returns a list of all posts.
   - **GET /posts/{id}:** Returns the post with the specified ID.
   - **POST /posts:** Creates a new post. Expects a JSON body with the post details.
   - **DELETE /posts/{id}:** Deletes the post with the specified ID.
3. **Follows**
    - **GET /users/{id}/follows:** Returns a list of all users that the user with the specified ID is following.
    - **PUT /users/{id}/follows/{followId}:** Makes the user with the specified ID follow another user. Expects a JSON body with the ID of the user to follow.
    - **DELETE /users/{id}/follows/{followId}:** Makes the user with the specified ID unfollow the user with the specified followId.
4. **Likes**
   - **GET /posts/{id}/likes:** Returns a list of all users who have liked the post with the specified ID.
   - **POST /posts/{id}/likes:** Makes a user like the post with the specified ID. Expects a JSON body with the ID of the user.
   - **DELETE /posts/{id}/likes/{userId}:** Removes the like from the user with the specified userId on the post with the specified ID.
5. **Search**
   - **GET /search/users?query={query}:** Searches for users based on the query string.
   - **GET /search/posts?query={query}:** Searches for posts based on the query string.
6. **Analytics**
   - **GET /analytics/users:** Returns analytics data about users, such as the total number of users, the average number of followers per user, etc.
   - **GET /analytics/posts:** Returns analytics data about posts, such as the total number of posts, the average number of likes per post, etc.

This API allows for full CRUD operations on users and posts, as well as following/unfollowing users and liking/unliking posts. It also provides search functionality for users and posts, and analytics data about users and posts.

```mermaid
graph 
    API[API]
    Users[Users]
    Posts[Posts]
    Follows[Follows]
    Likes[Likes]
    Search[Search]
    Analytics[Analytics]

    API --> Users
    API --> Posts
    API --> Follows
    API --> Likes
    API --> Search
    API --> Analytics

    subgraph Users
    GET_users[GET /users]
    GET_users_id[GET /users/id]
    POST_users[POST /users]
    PUT_users_id[PUT /users/id]
    DELETE_users_id[DELETE /users/id]
    end

    subgraph Posts
    GET_posts[GET users/id/posts]
    GET_posts_id[GET /posts/id]
    POST_posts[POST /posts]
    DELETE_posts_id[DELETE /posts/id]
    end

    subgraph Follows
    GET_users_id_follows[GET /users/id/follows]
    POST_users_id_follows[POST /users/id/follows]
    DELETE_users_id_follows_followId[DELETE /users/id/follows/followId]
    end

    subgraph Likes
    GET_posts_id_likes[GET /posts/id/likes]
    POST_posts_id_likes[POST /posts/id/likes]
    DELETE_posts_id_likes_userId[DELETE /posts/id/likes/userId]
    end

    subgraph Search
    GET_search_users[GET /search/users?query=query]
    GET_search_posts[GET /search/posts?query=query]
    end

    subgraph Analytics
    GET_analytics_users[GET /analytics/users]
    GET_analytics_posts[GET /analytics/posts]
    end
```

## Database selection process

Sure, let's dive into the reasoning for each type of data storage:

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
