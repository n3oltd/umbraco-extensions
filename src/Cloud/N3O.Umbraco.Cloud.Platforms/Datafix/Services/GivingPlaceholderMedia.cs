using System;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class GivingPlaceholderMedia {
    public Guid Id { get; set; }
    public string Src { get; set; }
    public string Filename { get; set; }
    public string MediaFileId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Created { get; set; }
}
