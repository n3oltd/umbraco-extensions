using Humanizer.Bytes;
using N3O.Umbraco.Attributes;

namespace N3O.Umbraco.Email.Models;

public class EmailAttachmentReq {
    [Name("Name")]
    public string Name { get; set; }

    [Name("Content Type")]
    public string ContentType { get; set; }

    [Name("Size")]
    public ByteSize Size { get; set; }

    [Name("Storage Folder Name")]
    public string StorageFolderName { get; set; }

    [Name("Filename")]
    public string Filename { get; set; }
}
