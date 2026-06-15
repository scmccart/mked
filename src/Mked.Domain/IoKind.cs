namespace Mked.Domain;

/// <summary>Distinguishes the cause of an <see cref="MkedError.IoError"/>.</summary>
public enum IoKind
{
    /// <summary>The file was not found at the specified path.</summary>
    ReadNotFound,

    /// <summary>Read access was denied by the operating system.</summary>
    ReadAccessDenied,

    /// <summary>Write access was denied by the operating system.</summary>
    WriteAccessDenied,

    /// <summary>A write failed for a reason other than an access-denied error.</summary>
    WriteGeneric,
}
