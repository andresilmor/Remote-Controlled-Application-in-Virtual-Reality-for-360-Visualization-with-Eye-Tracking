using System;
using System.Collections.Generic;
using Realms;
using MongoDB.Bson;

public partial class PanoramicSessionEntity : IRealmObject {
    [MapTo("_id")]
    [PrimaryKey]
    public ObjectId? Id { get; set; }

    [MapTo("imageHeight")]
    public int? ImageHeight { get; set; }

    [MapTo("imageWidth")]
    public int? ImageWidth { get; set; }

    [MapTo("mapping")]
    public IList<PanoramicSessionEntity_mapping> Mapping { get; }

    [MapTo("uuid")]
    public string? Uuid { get; set; }
}
