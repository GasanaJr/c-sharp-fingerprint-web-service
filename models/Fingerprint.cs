using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Fingerprint
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get; set;}

    [BsonElement("userId")]
    public required string UserId {get; set;}

    [BsonElement("template")]
    public required byte[] Template {get; set;}

    [BsonElement("createdAt")]
    public DateTime CreatedAt {get; set;}


}