using System.Collections.Generic;

namespace LastFmHonorific.Core;

/// <summary>
/// A single artist credited on a track. Mirrors the shape of Spotify's SimpleArtist
/// so existing Scriban templates (e.g. "Activity.Artists[0].Name") keep working unchanged.
/// </summary>
public sealed class SimpleArtist
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// The album a track belongs to. Mirrors Spotify's SimpleAlbum shape (just the Name,
/// which is all the existing templates use: "Activity.Album.Name").
/// </summary>
public sealed class SimpleAlbum
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents the track Last.fm currently reports as "now playing" for the configured user.
/// Field names intentionally match SpotifyAPI.Web's FullTrack (Name, Artists, Album.Name,
/// DurationMs, Popularity) so templates written for SpotifyHonorific can be pasted in as-is.
///
/// Last.fm does not provide DurationMs or Popularity for now-playing scrobbles, so those are
/// always 0 - they exist only so templates referencing them don't throw a parse/render error.
/// </summary>
public sealed class LastFmTrack
{
    public string Name { get; set; } = string.Empty;
    public List<SimpleArtist> Artists { get; set; } = new();
    public SimpleAlbum Album { get; set; } = new();
    public int DurationMs { get; set; }
    public int Popularity { get; set; }

    /// <summary>
    /// A stable-ish identity for "is this still the same track" comparisons.
    /// Last.fm doesn't give us a track ID, so we build one from artist+name.
    /// </summary>
    public string Id => $"{(Artists.Count > 0 ? Artists[0].Name : string.Empty)}::{Name}";
}
