namespace N3O.Umbraco.Marketing.Models;

public class TrafficRow {
    public string Campaign { get; set; }
    public string Date { get; set; }
    public string Medium { get; set; }
    public int NewUsers { get; set; }
    public int Pageviews { get; set; }
    public string Referrer { get; set; }
    public int Sessions { get; set; }
    public string Source { get; set; }
}
