using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.Cropper.DataTypes;

public class CropperSource {
    [JsonProperty("src")]
    public string Src { get; set; }

    [JsonProperty("mediaId")]
    public string MediaId { get; set; }

    [JsonProperty("filename")]
    public string Filename { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("altText")]
    public string AltText { get; set; }

    [JsonProperty("crops")]
    public IEnumerable<Crop> Crops { get; set; }

    public class Crop {
        [JsonProperty("x")]
        public int X { get; set; }
    
        [JsonProperty("y")]
        public int Y { get; set; }
    
        [JsonProperty("width")]
        public int Width { get; set; }
    
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("rotate", NullValueHandling = NullValueHandling.Ignore)]
        public int? Rotate { get; set; }
    }
}
