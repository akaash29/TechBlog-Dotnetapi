namespace UserRegistration.Application.Common.Constants;

/// <summary>
/// The fixed set of Azure Blob Storage containers this app uploads into, and the
/// virtual-folder prefixes used inside each one. Containers are created on first
/// use (see AzureBlobStorageService) — nothing needs provisioning up front.
/// </summary>
public static class BlobContainers
{
    /// <summary>Cover and inline body images uploaded from the compose editor, stored
    /// under the "images/" virtual folder inside this container.</summary>
    public const string BlogPost = "blogpost";
    public const string BlogPostImagesFolder = "images";

    /// <summary>Profile photos, stored one virtual folder per user ("{userId:N}/")
    /// inside this container so each user's uploads are grouped together.</summary>
    public const string UserImage = "userimage";

    /// <summary>Message attachments and voice notes, stored one virtual folder
    /// per conversation ("{lowerUserId:N}_{higherUserId:N}/") — the two
    /// participants' ids sorted so both directions of a conversation land in
    /// the same folder — then split into "files/" and "voice/" underneath.</summary>
    public const string Messages = "messages";
    public const string MessageFilesFolder = "files";
    public const string MessageVoiceFolder = "voice";
}
