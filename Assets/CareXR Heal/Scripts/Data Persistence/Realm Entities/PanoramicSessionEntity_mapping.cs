using System;
using System.Collections.Generic;
using Realms;
using MongoDB.Bson;

public partial class PanoramicSessionEntity_mapping : IEmbeddedObject {
    [MapTo("boundingBox")]
    public PanoramicSessionEntity_mapping_boundingBox? BoundingBox { get; set; }

    [MapTo("data")]
    public PanoramicSessionEntity_mapping_data? Data { get; set; }
}
