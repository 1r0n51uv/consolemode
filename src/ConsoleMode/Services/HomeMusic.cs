using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace ConsoleMode.Services;

/// <summary>
/// Plays <see cref="AmbientMusic"/> in a loop under Home, fading in and out, on its own player so the interface sounds
/// (winmm, <see cref="UiSounds"/>) never cut it. Anything that fails (no audio device) just stays silent.
/// </summary>
public static class HomeMusic
{
    private const double Volume = 0.22;
    private static MediaPlayer? _player;
    private static Microsoft.UI.Dispatching.DispatcherQueueTimer? _fade;
    private static bool _wanted;
    private static bool _failed;

    /// <summary>Plays (fading in) or stops (fading out). Safe to call again with the same value.</summary>
    public static void Set(bool play, Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        if (_failed || play == _wanted) return;
        _wanted = play;
        try
        {
            if (play && _player is null) _player = Create();
            if (_player is null) return;
            if (play && _player.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                _player.Volume = 0;
                _player.Play();
            }
            Fade(dispatcher, play ? Volume : 0, pauseAtEnd: !play);
        }
        catch (Exception ex)
        {
            _failed = true;
            AppLog.Write($"Música do Início: desativada: {ex.Message}");
        }
    }

    private static MediaPlayer Create()
    {
        var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(AmbientMusic.Wav());
            writer.StoreAsync().AsTask().GetAwaiter().GetResult();
            writer.DetachStream();
        }
        stream.Seek(0);
        return new MediaPlayer
        {
            Source = MediaSource.CreateFromStream(stream, "audio/wav"),
            IsLoopingEnabled = true,
            Volume = 0,
            AudioCategory = MediaPlayerAudioCategory.GameMedia
        };
    }

    /// <summary>About a second and a half to the target volume, in small steps.</summary>
    private static void Fade(Microsoft.UI.Dispatching.DispatcherQueue dispatcher, double target, bool pauseAtEnd)
    {
        _fade?.Stop();
        _fade = dispatcher.CreateTimer();
        _fade.Interval = TimeSpan.FromMilliseconds(50);
        _fade.Tick += (timer, _) =>
        {
            if (_player is null) { timer.Stop(); return; }
            var step = Volume / 30;
            var volume = _player.Volume;
            volume = target > volume ? Math.Min(target, volume + step) : Math.Max(target, volume - step);
            _player.Volume = volume;
            if (Math.Abs(volume - target) > 0.0001) return;
            timer.Stop();
            if (pauseAtEnd) _player.Pause();
        };
        _fade.Start();
    }
}
