using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LastFmHonorific.Core;

/// <summary>
/// Polls the Last.fm "user.getrecenttracks" endpoint and reports back the track
/// currently marked as "now playing" for the configured user, if any.
///
/// Unlike Spotify, Last.fm's public API uses a single static API key (no OAuth, no
/// refresh tokens, no client secret) - so this class is much simpler than
/// SpotifyPollingService. The username is whoever's profile we're reading, the
/// API key just identifies our app to Last.fm's rate limiter.
/// </summary>
public class LastFmPollingService : IDisposable
{
    private const int API_TIMEOUT_MS = 5000;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int MAX_RESPONSE_TIME_SAMPLES = 100;
    private const string API_BASE_URL = "https://ws.audioscrobbler.com/2.0/";

    private readonly Config _config;
    private readonly IPluginLog _pluginLog;
    private readonly IChatGui _chatGui;
    private readonly HttpClient _httpClient;

    private int _apiCallCount;
    private int _apiErrorCount;
    private readonly Queue<long> _apiResponseTimes = new(MAX_RESPONSE_TIME_SAMPLES);

    public int ApiCallCount => _apiCallCount;
    public int ApiErrorCount => _apiErrorCount;
    public double AverageResponseTime => _apiResponseTimes.Count > 0 ? Average(_apiResponseTimes) : 0;

    public LastFmPollingService(Config config, IPluginLog pluginLog, IChatGui chatGui)
    {
        _config = config;
        _pluginLog = pluginLog;
        _chatGui = chatGui;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(API_TIMEOUT_MS)
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns the track Last.fm currently reports as "now playing" for the configured
    /// user, or null if nothing is playing right now (or the request failed).
    /// </summary>
    public async Task<LastFmTrack?> GetCurrentlyPlayingTrackAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (username, apiKey) = _config.WithLock(() => (_config.LastFmUsername, _config.LastFmApiKey));

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            var url = BuildRequestUrl(username, apiKey);

            var json = await RetryAsync(
                () => FetchJsonAsync(url),
                maxRetries: MAX_RETRY_ATTEMPTS
            ).ConfigureAwait(false);

            stopwatch.Stop();
            _apiCallCount++;
            RecordResponseTime(stopwatch.ElapsedMilliseconds);

            if (json == null)
            {
                return null;
            }

            return ParseNowPlayingTrack(json);
        }
        catch (OperationCanceledException)
        {
            _apiErrorCount++;
            HandleError(null, "Last.fm API request timed out after 5 seconds.");
            return null;
        }
        catch (HttpRequestException e)
        {
            _apiErrorCount++;
            HandleError(e, "Error polling Last.fm. Check your username and API key.");
            return null;
        }
        catch (Exception e)
        {
            _apiErrorCount++;
            HandleError(e, "Unhandled error during Last.fm poll");
            return null;
        }
    }

    private static string BuildRequestUrl(string username, string apiKey)
    {
        var encodedUser = Uri.EscapeDataString(username);
        var encodedKey = Uri.EscapeDataString(apiKey);
        return $"{API_BASE_URL}?method=user.getrecenttracks&user={encodedUser}&api_key={encodedKey}&format=json&limit=1";
    }

    private async Task<JsonDocument?> FetchJsonAsync(string url)
    {
        using var cts = new CancellationTokenSource(API_TIMEOUT_MS);
        using var response = await _httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            throw new HttpRequestException($"Last.fm returned {(int)response.StatusCode}: {Truncate(body, 200)}");
        }

        var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the "user.getrecenttracks" response and returns the track only if Last.fm
    /// is flagging it with the "@attr": {"nowplaying": "true"} marker - i.e. it's actually
    /// playing right now, not just the last historical scrobble.
    /// </summary>
    internal static LastFmTrack? ParseNowPlayingTrack(JsonDocument document)
    {
        using (document)
        {
            var root = document.RootElement;

            if (!root.TryGetProperty("recenttracks", out var recentTracks))
            {
                return null;
            }

            if (!recentTracks.TryGetProperty("track", out var trackArray) || trackArray.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            if (trackArray.GetArrayLength() == 0)
            {
                return null;
            }

            var firstTrack = trackArray[0];

            if (!IsNowPlaying(firstTrack))
            {
                return null;
            }

            return new LastFmTrack
            {
                Name = GetString(firstTrack, "name"),
                Artists = new() { new SimpleArtist { Name = GetArtistName(firstTrack) } },
                Album = new SimpleAlbum { Name = GetAlbumName(firstTrack) },
                DurationMs = 0,
                Popularity = 0
            };
        }
    }

    private static bool IsNowPlaying(JsonElement track)
    {
        if (!track.TryGetProperty("@attr", out var attr))
        {
            return false;
        }

        if (!attr.TryGetProperty("nowplaying", out var nowPlaying))
        {
            return false;
        }

        return nowPlaying.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => string.Equals(nowPlaying.GetString(), "true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string GetArtistName(JsonElement track)
    {
        if (!track.TryGetProperty("artist", out var artist))
        {
            return string.Empty;
        }

        // Last.fm returns artist either as {"#text": "Name", "mbid": "..."} or, in some
        // endpoints, just a plain string. Handle both defensively.
        if (artist.ValueKind == JsonValueKind.Object && artist.TryGetProperty("#text", out var text))
        {
            return text.GetString() ?? string.Empty;
        }

        return artist.ValueKind == JsonValueKind.String ? artist.GetString() ?? string.Empty : string.Empty;
    }

    private static string GetAlbumName(JsonElement track)
    {
        if (!track.TryGetProperty("album", out var album))
        {
            return string.Empty;
        }

        if (album.ValueKind == JsonValueKind.Object && album.TryGetProperty("#text", out var text))
        {
            return text.GetString() ?? string.Empty;
        }

        return album.ValueKind == JsonValueKind.String ? album.GetString() ?? string.Empty : string.Empty;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    private async Task<T?> RetryAsync<T>(Func<Task<T>> operation, int maxRetries = MAX_RETRY_ATTEMPTS) where T : class
    {
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries - 1)
                {
                    throw;
                }

                var delayMs = (int)Math.Pow(2, attempt) * 1000;

                if (_config.EnableDebugLogging)
                {
                    _pluginLog.Debug($"Retry attempt {attempt + 1}/{maxRetries} after {delayMs}ms delay. Error: {ex.Message}");
                }

                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }

        return null;
    }

    private void RecordResponseTime(long milliseconds)
    {
        _apiResponseTimes.Enqueue(milliseconds);

        if (_apiResponseTimes.Count > MAX_RESPONSE_TIME_SAMPLES)
        {
            _apiResponseTimes.Dequeue();
        }
    }

    private static double Average(Queue<long> values)
    {
        long sum = 0;
        foreach (var value in values)
        {
            sum += value;
        }
        return (double)sum / values.Count;
    }

    private void HandleError(Exception? e, string message)
    {
        if (e != null)
        {
            _pluginLog.Warning(e, message);
        }
        else
        {
            _pluginLog.Warning(message);
        }

        if (_config.EnableDebugLogging)
        {
            _chatGui.PrintError($"LastFmHonorific: {message}");
        }
    }
}
