using N3O.Umbraco.Extensions;
using System;
using System.Security.Cryptography;
using System.Text;
using Umbraco.Extensions;

namespace N3O.Umbraco.Utilities;

public enum IdScope {
    Block,
    BlockArea,
    BlockCategory,
    BlockDataType,
    BlockLayout,
    ContentType,
    ContentTypeContainer,
    ContentTypeFolder,
    DataType,
    DataTypeContainer,
    DataTypeFolder,
    PropertyType
}

public static class UmbracoId {
    // Derived GUIDs are persisted, so the seed format, casing and algorithm must never change
    public static Guid Deterministic(IdScope scope, params object[] seeds) {
        var key = $"{scope}_{string.Join("_", seeds)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var bytes = new byte[16];

        Array.Copy(hash, bytes, 16);

        bytes[6] = (byte) ((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte) ((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }

    public static Guid Generate(IdScope scope, params object[] seeds) {
        var id = $"{scope}_{string.Join("_", seeds)}".GetDeterministicHashCode(true).ToGuid();

        return id;
    }
}
