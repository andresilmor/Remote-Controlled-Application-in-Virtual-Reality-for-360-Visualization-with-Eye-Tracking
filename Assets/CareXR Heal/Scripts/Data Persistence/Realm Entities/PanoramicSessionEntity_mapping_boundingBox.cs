using System;
using System.Collections.Generic;
using Realms;
using MongoDB.Bson;

public partial class PanoramicSessionEntity_mapping_boundingBox : IEmbeddedObject {
    [MapTo("height")]
    public int? Height { get; set; }

    [MapTo("width")]
    public int? Width { get; set; }

    [MapTo("x")]
    public int? X { get; set; }

    [MapTo("y")]
    public int? Y { get; set; }
}
