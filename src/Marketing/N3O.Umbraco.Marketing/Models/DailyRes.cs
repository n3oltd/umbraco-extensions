using System.Collections.Generic;

namespace N3O.Umbraco.Marketing.Models;

public class DailyRes {
    public IEnumerable<GoalRow> Goals { get; set; }
    public IEnumerable<TrafficRow> Traffic { get; set; }
    public IEnumerable<UserRow> Users { get; set; }
}
