using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class GivingMigrationRepointRes {
    public string Message { get; set; }
    public bool Preview { get; set; }
    public int Attempted { get; set; }
    public int Repointed { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public IEnumerable<GivingMigrationRepointItemRes> Items { get; set; } = [];
}
