using System;
using System.Collections.Generic;
using Realms;
using MongoDB.Bson;

public partial class PanoramicSessionEntity_mapping_data : IEmbeddedObject {
    [MapTo("alias")]
    public string? Alias { get; set; }
}
