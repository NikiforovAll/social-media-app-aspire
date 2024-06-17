GET /posts/_search
{
    "query": {
        "bool": {
            "should": [
                {
                    "match": {
                        "title": "voluptas"
                    }
                },
                {
                    "match": {
                        "content": "voluptas"
                    }
                }
            ]
        }
    }
}

GET /likes/_search
{
    "query": {
        "match": {
            "authorId": 17
        }
    }
}

GET /likes/_search
{
    "size": 0,
    "query": {
        "range": {
            "createdAt": {
                "gte": "2020-01-01",
                "lte": "2025-01-01"
            }
        }
    },
    "aggs": {
        "likes_by_user": {
            "terms": {
                "field": "likedBy",
                "size": 5,
                "order": {
                    "_count": "desc"
                }
            }
        },
        "likes_by_post":{
            "terms": {
                "field": "postId.keyword",
                "size": 5,
                "order": {
                    "_count": "desc"
                }
            }
        }
    }
}
