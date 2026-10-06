using N3O.Umbraco.Attributes;

namespace N3O.Umbraco.Email.Models;

public class EmailAttachmentReq {
    [Name("Name")]
    public string Name { get; set; }

    [Name("Content Type")]
    public string ContentType { get; set; }

    [Name("Bytes")]
    public byte[] Bytes { get; set; }
}
