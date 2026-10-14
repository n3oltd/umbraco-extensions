using Humanizer.Bytes;

namespace N3O.Umbraco.Email;

public static class EmailConstants {
    public static readonly ByteSize MaximumAttachmentsSize = ByteSize.FromMegabytes(10);

    public static class StorageFolders {
        public static readonly string Attachments = "EmailAttachments";
    }
}
