using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DoubleHeadphones3D.Audio;

/// <summary>
/// Captures the Windows default render endpoint with WASAPI loopback and fans
/// the same processed signal out to two selected render endpoints.
/// </summary>
public sealed class DualOutputRouter : IDisposable
{
    private readonly MMDevice _firstDevice;
    private readonly MMDevice _secondDevice;
    private WasapiLoopbackCapture? _capture;
    private WasapiOut? _firstOut;
    private WasapiOut? _secondOut;
    private BufferedWaveProvider? _firstBuffer;
    private BufferedWaveProvider? _secondBuffer;
    private readonly object _gate = new();
    private bool _started;
    private bool _disposed;

    public bool SpatialEnabled { get; set; } = true;

    public DualOutputRouter(MMDevice firstDevice, MMDevice secondDevice)
    {
        _firstDevice = firstDevice;
        _secondDevice = secondDevice;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) return;

        _capture = new WasapiLoopbackCapture();
        var format = _capture.WaveFormat;

        _firstBuffer = CreateBuffer(format);
        _secondBuffer = CreateBuffer(format);

        // Shared-mode WASAPI lets Windows perform endpoint format conversion when needed.
        _firstOut = new WasapiOut(_firstDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _secondOut = new WasapiOut(_secondDevice, AudioClientShareMode.Shared, useEventSync: true, latency: 100);
        _firstOut.Init(_firstBuffer);
        _secondOut.Init(_secondBuffer);

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;

        _firstOut.Play();
        _secondOut.Play();
        _capture.StartRecording();
        _started = true;

        Console.WriteLine($"Capture format: {format}");
        if (format.Encoding != WaveFormatEncoding.IeeeFloat || format.BitsPerSample != 32 || format.Channels != 2)
            Console.WriteLine("DSP note: capture is not 32-bit float stereo; spatial effect will bypass processing.");
    }

    private static BufferedWaveProvider CreateBuffer(WaveFormat format) => new(format)
    {
        DiscardOnBufferOverflow = true,
        BufferDuration = TimeSpan.FromMilliseconds(500),
        ReadFully = true
    };

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_firstBuffer is null || _secondBuffer is null || e.BytesRecorded <= 0)
            return;

        byte[] processed = new byte[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, processed, 0, e.BytesRecorded);

        if (SpatialEnabled && _capture is not null)
            StereoSpatializer.ProcessInPlace(processed, e.BytesRecorded, _capture.WaveFormat);

        lock (_gate)
        {
            _firstBuffer.AddSamples(processed, 0, e.BytesRecorded);
            _secondBuffer.AddSamples(processed, 0, e.BytesRecorded);
        }
    }

    private static void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            Console.WriteLine($"Capture stopped: {e.Exception.Message}");
    }

    public void Stop()
    {
        if (!_started) return;

        try { _capture?.StopRecording(); } catch { }
        try { _firstOut?.Stop(); } catch { }
        try { _secondOut?.Stop(); } catch { }

        _started = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();

        if (_capture is not null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
            _capture.Dispose();
        }

        _firstOut?.Dispose();
        _secondOut?.Dispose();
        _firstDevice.Dispose();
        _secondDevice.Dispose();

        _disposed = true;
    }
}
