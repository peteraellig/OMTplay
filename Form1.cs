using libomtnet; // brings OMTDiscovery, OMTReceive, OMTFrameType
using Microsoft.VisualBasic;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;    // for WasapiOut
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OMTplay
{
    public partial class Form1 : Form
    {
        // --- OMT SDK objects (connection) ---
        private OMTDiscovery? _discovery;
        private OMTReceive? _receiver;
        private Thread? _rxThread;
        private CancellationTokenSource? _cts;

        // --- Audio (NAudio) ---
        private IWavePlayer? _waveOut;
        private BufferedWaveProvider? _audioBuffer;
        private bool _audioInitDone = false;
        private bool _audioIsFloat = true;        // determined on first frame
        private int _audioChannels = 2;           // default stereo
        private const int AUDIO_SR = 48000;       // 48 kHz

        // --- Fullscreen state ---
        private bool _isFullscreen = false;
        private FormBorderStyle _oldBorderStyle;
        private Rectangle _oldBounds;

        public Form1()
        {
            InitializeComponent();

            // --- UI event assignments ---
            _btnRefresh.Click += (s, e) => RefreshSources();
            _btnConnect.Click += (s, e) => ConnectSelected();
            _btnDisconnect.Click += (s, e) => Disconnect();
            _btnFullscreen.Click += (s, e) => ToggleFullscreen();
            _btnExit.Click += (s, e) => ExitWithConfirmation();
            KeyPreview = true;
            KeyDown += Form1_KeyDown;
            FormClosing += Form1_FormClosing;
        }

        // --- UI: Fullscreen toggle ---
        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _isFullscreen = true;
                _oldBorderStyle = this.FormBorderStyle;
                _oldBounds = this.Bounds;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                flowLayoutPanel1.Visible = false; // Hide menu panel
                _videoBox.Dock = DockStyle.Fill;
                _videoBox.Focus();
                Cursor.Hide();
            }
            else
            {
                _isFullscreen = false;
                FormBorderStyle = _oldBorderStyle;
                WindowState = FormWindowState.Normal;
                this.Location = _oldBounds.Location;
                this.Size = _oldBounds.Size;
                flowLayoutPanel1.Visible = true; // Show menu panel
                flowLayoutPanel1.BringToFront();
                flowLayoutPanel1.Dock = DockStyle.Top;
                _videoBox.Dock = DockStyle.Fill;
                Cursor.Show();
                this.PerformLayout();
            }
        }

        // --- UI: Keyboard event for fullscreen exit ---
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_isFullscreen && e.KeyCode == Keys.Escape)
            {
                ToggleFullscreen();
            }
        }

        // --- Connection: Form load, OMT discovery initialization ---
        private void Form1_Load(object? sender, EventArgs e)
        {
            try
            {
                _discovery = OMTDiscovery.GetInstance();
                RefreshSources();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to initialize OMT discovery: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                var asm = typeof(OMTDiscovery).Assembly.Location;
                _lblStatus.Text = "OMT loaded from: " + asm;
                _discovery = OMTDiscovery.GetInstance(); // triggers native loading
            }
            catch (DllNotFoundException ex)
            {
                MessageBox.Show("Native DLL missing: " + ex.Message);
            }
            catch (BadImageFormatException ex)
            {
                MessageBox.Show("Platform mismatch (x64 required): " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("OMT init error: " + ex.Message);
            }
            this.Text = "OMTplay 1.0.0.4 - Peter Aellig";
            _btnDisconnect.Visible = false;
        }

        // --- Connection: Discover available sources ---
        private void RefreshSources()
        {
            try
            {
                _cbSources.Items.Clear();
                Application.DoEvents();
                Thread.Sleep(300);
                var addresses = _discovery?.GetAddresses() ?? Array.Empty<string>();
                foreach (var addr in addresses) _cbSources.Items.Add(addr);
                if (_cbSources.Items.Count > 0) _cbSources.SelectedIndex = 0;
                _lblStatus.Text = $"Discovered {_cbSources.Items.Count} source(s)";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Discovery error: " + ex.Message;
            }
        }

        // --- Connection: Connect to selected source ---
        private void ConnectSelected()
        {
            if (_cbSources.SelectedItem is not string address || string.IsNullOrWhiteSpace(address))
            {
                MessageBox.Show(this, "Please select a source.");
                return;
            }

            try
            {
                Disconnect();

                _receiver = new OMTReceive(
                    address,
                    OMTFrameType.Video | OMTFrameType.Audio,
                    OMTPreferredVideoFormat.BGRA,
                    OMTReceiveFlags.None);

                _cts = new CancellationTokenSource();
                _rxThread = new Thread(() => ReceiveLoop(_cts.Token))
                {
                    IsBackground = true,
                    Name = "OMT Receive Thread"
                };
                _rxThread.Start();

                _audioInitDone = false;

                // UI: Update button states after connect
                _btnConnect.Enabled = false;
                _btnConnect.Visible = false;
                _btnDisconnect.Visible = true;
                _btnDisconnect.Enabled = true;

                _lblStatus.Text = "Connected: " + address;
                _lblConnected.Text = $"Connected: {address}";
                _lblResolution.Text = "-";
                _lblTimestamp.Text = "-";
                _lblConnected.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Connect error: " + ex.Message;
                Disconnect();
            }
        }

        // --- Connection: Disconnect from source and cleanup ---
        private void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                if (_rxThread is { IsAlive: true })
                {
                    if (!_rxThread.Join(1000)) _rxThread.Interrupt();
                }
            }
            catch (Exception ex) { Debug.WriteLine("Thread stop error: " + ex); }
            finally
            {
                _rxThread = null;
                _cts?.Dispose();
                _cts = null;
            }

            try { _receiver?.Dispose(); } catch { } finally { _receiver = null; }

            try { _waveOut?.Stop(); } catch { }
            try { _waveOut?.Dispose(); } catch { }
            _waveOut = null;
            _audioBuffer = null;

            // UI: Update button states after disconnect
            _btnConnect.Enabled = true;
            _btnConnect.Visible = true;
            _btnDisconnect.Visible = false;
            _btnDisconnect.Enabled = false;

            // UI: Reset labels
            _lblConnected.Text = "Disconnected";
            _lblConnected.ForeColor = Color.Red;
            _lblResolution.Text = "-";
            _lblTimestamp.Text = "-";
        }

        // --- Audio: Initialize audio output and buffer ---
        private void EnsureAudioInit(int sampleRate, int channels, bool isFloat,
                             int desiredLatencyMs = 180, double bufferSeconds = 0.8)
        {
            if (_audioInitDone) return;

            _waveOut?.Stop();
            _waveOut?.Dispose();

            // WasapiOut is often more stable than WaveOutEvent
            _waveOut = new WasapiOut(AudioClientShareMode.Shared, true, desiredLatencyMs);

            WaveFormat fmt = isFloat
                ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels)
                : new WaveFormat(sampleRate, 16, channels);

            _audioBuffer = new BufferedWaveProvider(fmt)
            {
                BufferDuration = TimeSpan.FromSeconds(bufferSeconds),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            _audioBuffer.BufferLength = (int)(fmt.AverageBytesPerSecond * bufferSeconds);

            _waveOut.Init(_audioBuffer);
            _waveOut.Play();

            _audioIsFloat = isFloat;
            _audioChannels = channels;
            _audioInitDone = true;
        }

        // --- Main receive loop: handles audio and video frames ---
        private void ReceiveLoop(CancellationToken ct)
        {
            var frame = new OMTMediaFrame();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    bool handled = false;

                    // --- AUDIO: receive and buffer all waiting frames ---
                    int drained = 0;
                    while (_receiver != null && _receiver.Receive(OMTFrameType.Audio, 0, ref frame))
                    {
                        if (frame.Data != IntPtr.Zero && frame.DataLength > 0)
                        {
                            // 1) On first audio frame: auto-detect format
                            if (!_audioInitDone)
                            {
                                int channelsGuess = frame.Channels > 0 ? frame.Channels : 2;
                                int sampleRate = frame.SampleRate > 0 ? frame.SampleRate : 48000;
                                bool looksLikeFloat = true; // vMix always delivers Float32

                                EnsureAudioInit(sampleRate, channelsGuess, looksLikeFloat, desiredLatencyMs: 100, bufferSeconds: 2.0);
                            }

                            // 2) Push data in correct format to buffer
                            if (_audioBuffer != null)
                            {
                                if (_audioIsFloat)
                                {
                                    // Convert planar to interleaved
                                    int samplesPerChannel = frame.SamplesPerChannel;
                                    int channels = _audioChannels;
                                    // 1. Copy planar data from unmanaged buffer
                                    float[] planar = new float[samplesPerChannel * channels];
                                    Marshal.Copy(frame.Data, planar, 0, planar.Length);

                                    // 2. Convert planar to interleaved
                                    float[] interleaved = new float[samplesPerChannel * channels];
                                    for (int i = 0; i < samplesPerChannel; i++)
                                        for (int c = 0; c < channels; c++)
                                            interleaved[i * channels + c] = planar[c * samplesPerChannel + i];

                                    // 3. Convert to byte array
                                    byte[] bytes = new byte[interleaved.Length * 4];
                                    Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);

                                    // 4. Add to NAudio buffer
                                    _audioBuffer.AddSamples(bytes, 0, bytes.Length);
                                }
                                else
                                {
                                    byte[] bytes = new byte[frame.DataLength];
                                    Marshal.Copy(frame.Data, bytes, 0, frame.DataLength);
                                    _audioBuffer.AddSamples(bytes, 0, bytes.Length);
                                }
                            }
                        }

                        if (++drained >= 32) break;
                    }

                    // --- VIDEO: receive next frame (with longer timeout) ---
                    if (_receiver != null && _receiver.Receive(OMTFrameType.Video, 20, ref frame))
                    {
                        var bmp = CreateBitmapFromBGRA(frame);
                        if (bmp != null)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                var old = _videoBox.Image;
                                _videoBox.Image = bmp;
                                _lblResolution.Text = FormatResolution(frame);
                                _lblTimestamp.Text = FormatTimestamp(frame.Timestamp, frame.FrameRateN, frame.FrameRateD);
                                old?.Dispose();
                            }));
                        }
                        handled = true;
                    }

                    if (!handled)
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (ThreadInterruptedException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _lblStatus.Text = "Receive error: " + ex.Message;
                        MessageBox.Show(this, "Error in receive loop:\n" + ex.Message, "OMT Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        // --- Video: Convert BGRA frame to Bitmap ---
        private static Bitmap? CreateBitmapFromBGRA(OMTMediaFrame frame)
        {
            // Additional validation
            if (frame.Data == IntPtr.Zero || frame.Width <= 0 || frame.Height <= 0 || frame.Stride <= 0)
                return null;

            try
            {
                var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, frame.Width, frame.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

                int srcStride = frame.Stride;
                int dstStride = bmpData.Stride;
                int rows = frame.Height;
                int bytesPerRow = Math.Min(Math.Abs(dstStride), Math.Abs(srcStride));

                for (int y = 0; y < rows; y++)
                {
                    IntPtr srcRow = frame.Data + y * srcStride;
                    IntPtr dstRow = bmpData.Scan0 + y * dstStride;

                    byte[] row = new byte[bytesPerRow];
                    Marshal.Copy(srcRow, row, 0, bytesPerRow);
                    Marshal.Copy(row, 0, dstRow, bytesPerRow);
                }
                bmp.UnlockBits(bmpData);
                return bmp;
            }
            catch (ArgumentException ex)
            {
                // Log or ignore error
                Debug.WriteLine("Bitmap error: " + ex.Message);
                return null;
            }
        }

        // --- Connection: Cleanup on form closing ---
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e) => Disconnect();

        // --- UI: Exit confirmation dialog ---
        private void ExitWithConfirmation()
        {
            var result = MessageBox.Show(this, "Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Close();
            }
        }

        // --- Video: Format timestamp for display ---
        private static string FormatTimestamp(long timestamp, int frameRateN, int frameRateD)
        {
            // 1 second = 10,000,000 ticks
            long totalSeconds = timestamp / 10_000_000;
            long remainder = timestamp % 10_000_000;

            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)(totalSeconds % 60);

            // Calculate frame from remainder and framerate
            double frameRate = frameRateD != 0 ? (double)frameRateN / frameRateD : 25.0; // fallback 25fps
            int frame = (int)(remainder * frameRate / 10_000_000);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frame:D2}";
        }

        // --- Video: Format resolution and framerate for display ---
        private static string FormatResolution(OMTMediaFrame frame)
        {
            // Calculate framerate
            double fps = frame.FrameRateD != 0 ? (double)frame.FrameRateN / frame.FrameRateD : 0;
            // Height as "p" (e.g. 1080p)
            string res = $"{frame.Height}p";
            // Framerate as integer if possible
            string fpsStr = fps > 0 ? $"{Math.Round(fps)}" : "?";
            return $"{res}{fpsStr}";
        }
    }
}
