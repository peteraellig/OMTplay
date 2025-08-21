using libomtnet; // OMTDiscovery, OMTReceive, OMTFrameType
using NAudio.CoreAudioApi; // <-- required for AudioClientShareMode (added from Nicosman)
using NAudio.Wasapi;       // WasapiOut
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

//DONT visible  _lblsStatus !
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

        // Incoming (from OMT/vMix)
        private bool _inputIsFloat = true;     //  vMix gives Float32
        private int _inputChannels = 2;       // actual input channels
        private int _inputSampleRate = 48000; // 48kHz

        // Output device capabilities
        private bool _deviceAcceptsFloat = true;
        private bool _audioPermanentlyDisabled = false;

        private const int AUDIO_SR = 48000;       // 48 kHz
        private bool _downmixToStereo = true;     // >2ch → stereo

        // --- Fullscreen state ---
        private bool _isFullscreen = false;
        private FormBorderStyle _oldBorderStyle;
        private Rectangle _oldBounds;
       
        private int _selectedStereoPair = 0; // Add this line

       

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

            // If not connected by Designer: //(added from Nicosman)
            // this.Load += Form1_Load; //(added from Nicosman)
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
                flowLayoutPanel1.Visible = true; // Menüleiste anzeigen
                flowLayoutPanel1.BringToFront();
                flowLayoutPanel1.Dock = DockStyle.Top;

                // Setze die VideoBox auf absolute Werte
                _videoBox.Dock = DockStyle.None; // Entferne das Andocken
                _videoBox.Location = new Point(0, 106); // Setze die Position
                _videoBox.Size = new Size(1030, 565); // Setze die Größe

                Cursor.Show();
                this.PerformLayout();
            }
        }

        // --- UI: Keyboard event for fullscreen exit ---
        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_isFullscreen && e.KeyCode == Keys.Escape) ToggleFullscreen();
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
            catch (DllNotFoundException ex) { MessageBox.Show("Native DLL missing: " + ex.Message); }
            catch (BadImageFormatException ex) { MessageBox.Show("Platform mismatch (x64 required): " + ex.Message); }
            catch (Exception ex) { MessageBox.Show("OMT init error: " + ex.Message); }

            this.Text = "OMTplay 1.0.0.7 - Peter Aellig / Nicos Manadis";
            _btnDisconnect.Visible = false;
            radioButton1.Checked = true; // Set default stereo pair selection    

            // Lade die gespeicherte Einstellung
            _chkStartFullscreen.Checked = Properties.Settings.Default.StartFullscreen;
            _cbSourceSelection.SelectedItem = Properties.Settings.Default.SelectedSource.ToString();

            // Prüfe, ob die Checkbox aktiviert ist
            if (_chkStartFullscreen.Checked)
            {
                // Versuche, die gespeicherte Quelle zu verbinden
                int selectedSource = Properties.Settings.Default.SelectedSource;

                // Prüfe, ob die gewünschte Quelle existiert
                if (selectedSource > 0 && selectedSource <= _cbSources.Items.Count)
                {
                    _cbSources.SelectedIndex = selectedSource - 1; // Wähle die gewünschte Quelle aus (0-basiert)
                    ConnectSelected(); // Stelle die Verbindung her
                    ToggleFullscreen(); // Starte im Vollbildmodus
                }
                else
                {
                    // Keine Verbindung herstellen, wenn die gewünschte Quelle nicht existiert
                    MessageBox.Show(this, $"The desired Autostart source ({selectedSource}) is not available.",
                        "Autostart connection failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
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
                _audioPermanentlyDisabled = false;

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
            _audioInitDone = false;

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
            _lblAudiochannels.Text = "Audio Channels (max. 8)";
        }

        // --- Audio device factory (WASAPI + fallback WaveOutEvent) ---
        private bool TryCreateAudioDevice(int desiredLatencyMs, bool wantFloat, out IWavePlayer? player, out bool deviceIsFloat)
        {
            player = null;
            deviceIsFloat = wantFloat;

            // 1) WASAPI (timer-driven) – float
            try
            {
                player = new WasapiOut(AudioClientShareMode.Shared, false, desiredLatencyMs);
                deviceIsFloat = true;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WASAPI(float) init failed: " + ex.Message);
            }

            // 2) WASAPI (timer-driven) – PCM16
            try
            {
                player = new WasapiOut(AudioClientShareMode.Shared, false, Math.Max(150, desiredLatencyMs));
                deviceIsFloat = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WASAPI(PCM16) init failed: " + ex.Message);
            }

            // 3) WaveOutEvent (WinMM) – PCM16
            try
            {
                var w = new WaveOutEvent { DesiredLatency = Math.Max(150, desiredLatencyMs) };
                player = w;
                deviceIsFloat = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WaveOutEvent init failed: " + ex.Message);
            }

            return false;
        }

        // --- Audio: Initialize audio output and buffer ---
        private void EnsureAudioInit(int sampleRate, int channels, bool incomingIsFloat,
                                     int desiredLatencyMs = 120, double bufferSeconds = 1.5)
        {
            if (_audioInitDone || _audioPermanentlyDisabled) return;

            // Input validation
            if (channels <= 0 || channels > 8) channels = 2;
            if (sampleRate < 8000 || sampleRate > 192000) sampleRate = AUDIO_SR;

            int outChannels = (_downmixToStereo && channels > 2) ? 2 : channels;

            _inputIsFloat = incomingIsFloat;
            _inputChannels = channels;
            _inputSampleRate = sampleRate;

            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _audioBuffer = null;

            if (!TryCreateAudioDevice(desiredLatencyMs, wantFloat: true, out _waveOut, out _deviceAcceptsFloat))
            {
                _audioPermanentlyDisabled = true;
                _audioInitDone = true;
                return;
            }

            var fmt = _deviceAcceptsFloat
                ? WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, outChannels)
                : new WaveFormat(sampleRate, 16, outChannels);

            _audioBuffer = new BufferedWaveProvider(fmt)
            {
                BufferDuration = TimeSpan.FromSeconds(bufferSeconds),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            _audioBuffer.BufferLength = (int)(fmt.AverageBytesPerSecond * bufferSeconds);

            // Null-Prüfung vor Init
            if (_waveOut == null || _audioBuffer == null)
            {
                Debug.WriteLine("Audio initialization failed: waveOut or audioBuffer is null.");
                _audioPermanentlyDisabled = true;
                return;
            }

            _waveOut.Init(_audioBuffer);
            _waveOut.Play();

            if (_downmixToStereo && channels > 2)
                _audioInitDone = true;
        }

        // Convert float [-1..1] -> 16-bit PCM
        private static void FloatInterleavedToPcm16Bytes(float[] interleaved, byte[] dest)
        {
            int di = 0;
            for (int i = 0; i < interleaved.Length; i++)
            {
                float f = interleaved[i];
                if (f > 1f) f = 1f; else if (f < -1f) f = -1f;
                short s = (short)(f * 32767f);
                dest[di++] = (byte)(s & 0xFF);
                dest[di++] = (byte)((s >> 8) & 0xFF);
            }
        }

        // --- select Audio Stereo Pair

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                _selectedStereoPair = 1; // Channels 1 & 2
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                _selectedStereoPair = 2; // Channels 3 & 4
            }
        }


        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                _selectedStereoPair = 3; // Channels 5 & 6
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                _selectedStereoPair = 4; // Channels 7 & 8
            }
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
                    while (!_audioPermanentlyDisabled &&
                           _receiver != null &&
                           _receiver.Receive(OMTFrameType.Audio, 0, ref frame))
                    {
                        if (frame.Data != IntPtr.Zero && frame.DataLength > 0)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                // Begrenzung der Kanäle auf maximal 8
                                int limitedChannels = Math.Min(frame.Channels, 8);
                                _lblAudiochannels.Text = $"Received audio channels: {limitedChannels}";
                            }));

                            // 1) On first audio frame: auto-detect format
                            if (!_audioInitDone)
                            {
                                int channelsGuess = Math.Min(frame.Channels > 0 ? frame.Channels : 2, 8);
                                int sampleRate = frame.SampleRate > 0 ? frame.SampleRate : 48000;
                                bool looksLikeFloat = true; // vMix always delivers Float32

                                EnsureAudioInit(sampleRate, channelsGuess, looksLikeFloat,
                                                desiredLatencyMs: 100, bufferSeconds: 2.0);
                            }

                            // 2) Push data to buffer (planar float32 → interleaved, with optional downmix)
                            if (_audioBuffer != null)
                            {
                                try
                                {
                                    int samplesPerChannel = Math.Max(0, frame.SamplesPerChannel);
                                    int inCh = Math.Max(1, _inputChannels);
                                    int totalSamples = samplesPerChannel * inCh;

                                    if (samplesPerChannel > 0 && totalSamples > 0)
                                    {
                                        // Copy planar float32 από unmanaged σε managed
                                        int maxFloats = frame.DataLength / sizeof(float);
                                        int copyFloats = Math.Min(totalSamples, maxFloats);

                                        float[] planar = new float[totalSamples];
                                        byte[] tmp = new byte[copyFloats * sizeof(float)];
                                        Marshal.Copy(frame.Data, tmp, 0, tmp.Length);
                                        Buffer.BlockCopy(tmp, 0, planar, 0, tmp.Length);

                                        if (_downmixToStereo && inCh > 2)
                                        {
                                            // Downmix zu 2 Kanälen (Stereopaar auswählen)
                                            float[] stereo = new float[samplesPerChannel * 2];

                                            for (int i = 0; i < samplesPerChannel; i++)
                                            {
                                                float sumL = 0f; int cntL = 0;
                                                float sumR = 0f; int cntR = 0;

                                                for (int c = 0; c < inCh; c++)
                                                {
                                                    // Wähle das Stereopaar basierend auf _selectedStereoPair
                                                    if (_selectedStereoPair == 1 && (c == 0 || c == 1)) // Channels 1 & 2
                                                    {
                                                        float s = planar[c * samplesPerChannel + i];
                                                        if (c == 0) { sumL += s; cntL++; } else { sumR += s; cntR++; }
                                                    }
                                                    else if (_selectedStereoPair == 2 && (c == 2 || c == 3)) // Channels 3 & 4
                                                    {
                                                        float s = planar[c * samplesPerChannel + i];
                                                        if (c == 2) { sumL += s; cntL++; } else { sumR += s; cntR++; }
                                                    }
                                                    else if (_selectedStereoPair == 3 && (c == 4 || c == 5)) // Channels 5 & 6
                                                    {
                                                        float s = planar[c * samplesPerChannel + i];
                                                        if (c == 4) { sumL += s; cntL++; } else { sumR += s; cntR++; }
                                                    }
                                                    else if (_selectedStereoPair == 4 && (c == 6 || c == 7)) // Channels 7 & 8
                                                    {
                                                        float s = planar[c * samplesPerChannel + i];
                                                        if (c == 6) { sumL += s; cntL++; } else { sumR += s; cntR++; }
                                                    }
                                                }

                                                float L = cntL > 0 ? (sumL / cntL) : 0f;
                                                float R = cntR > 0 ? (sumR / cntR) : 0f;

                                                stereo[i * 2 + 0] = L;
                                                stereo[i * 2 + 1] = R;
                                            }

                                            if (_deviceAcceptsFloat)
                                            {
                                                byte[] bytes = new byte[stereo.Length * sizeof(float)];
                                                Buffer.BlockCopy(stereo, 0, bytes, 0, bytes.Length);
                                                _audioBuffer!.AddSamples(bytes, 0, bytes.Length);
                                            }
                                            else
                                            {
                                                byte[] pcm = new byte[stereo.Length * 2]; // 16-bit
                                                FloatInterleavedToPcm16Bytes(stereo, pcm);
                                                _audioBuffer!.AddSamples(pcm, 0, pcm.Length);
                                            }
                                        }
                                        else
                                        {
                                            // inCh <= 2 : απλή interleave (planar -> interleaved)
                                            float[] interleaved = new float[totalSamples];
                                            for (int i = 0; i < samplesPerChannel; i++)
                                                for (int c = 0; c < inCh; c++)
                                                    interleaved[i * inCh + c] = planar[c * samplesPerChannel + i];

                                            if (_deviceAcceptsFloat)
                                            {
                                                byte[] bytes = new byte[interleaved.Length * sizeof(float)];
                                                Buffer.BlockCopy(interleaved, 0, bytes, 0, bytes.Length);
                                                _audioBuffer!.AddSamples(bytes, 0, bytes.Length);
                                            }
                                            else
                                            {
                                                byte[] pcm = new byte[interleaved.Length * 2]; // 16-bit
                                                FloatInterleavedToPcm16Bytes(interleaved, pcm);
                                                _audioBuffer!.AddSamples(pcm, 0, pcm.Length);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ax)
                                {
                                    Debug.WriteLine("Audio buffer error: " + ax);
                                    _audioPermanentlyDisabled = true;
                                    try { _waveOut?.Stop(); _waveOut?.Dispose(); } catch { }
                                    _waveOut = null; _audioBuffer = null;
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

                    if (!handled) Thread.Sleep(1);
                }
                catch (ThreadInterruptedException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(() =>
                    {
                        _lblStatus.Text = "Receive error: " + ex.Message;
                        MessageBox.Show(this, "Error in receive loop:\n" + ex.ToString(),
                            "OMT Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        // --- Video: Convert BGRA frame to Bitmap (resistant to negative stride) ---
        private static Bitmap? CreateBitmapFromBGRA(OMTMediaFrame frame)
        {
            if (frame.Data == IntPtr.Zero || frame.Width <= 0 || frame.Height <= 0)
                return null;

            try
            {
                int rows = frame.Height;
                int srcStride = frame.Stride;
                bool srcBottomUp = srcStride < 0;
                int absSrcStride = Math.Abs(srcStride);

                if (frame.DataLength > 0)
                {
                    int maxStrideBySize = frame.DataLength / Math.Max(1, rows);
                    absSrcStride = Math.Min(absSrcStride, maxStrideBySize);
                }

                var bmp = new Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, frame.Width, frame.Height);
                var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);

                try
                {
                    int dstStride = bmpData.Stride;
                    int bytesPerRow = Math.Min(absSrcStride, Math.Abs(dstStride));
                    byte[] rowBuf = new byte[bytesPerRow];

                    for (int y = 0; y < rows; y++)
                    {
                        int sy = srcBottomUp ? (rows - 1 - y) : y;

                        IntPtr srcRow = IntPtr.Add(frame.Data, sy * srcStride);
                        IntPtr dstRow = IntPtr.Add(bmpData.Scan0, y * dstStride);

                        Marshal.Copy(srcRow, rowBuf, 0, bytesPerRow);
                        Marshal.Copy(rowBuf, 0, dstRow, bytesPerRow);
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("Bitmap error: " + ex);
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
            if (result == DialogResult.Yes) Close();
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

            double frameRate = frameRateD != 0 ? (double)frameRateN / frameRateD : 25.0; // fallback 25fps
            int frame = (int)(remainder * frameRate / 10_000_000);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}:{frame:D2}";
        }

        // --- Video: Format resolution and framerate for display ---
        private static string FormatResolution(OMTMediaFrame frame)
        {
            double fps = frame.FrameRateD != 0 ? (double)frame.FrameRateN / frame.FrameRateD : 0;
            string res = $"{frame.Height}p";
            string fpsStr = fps > 0 ? $"{Math.Round(fps)}" : "?";
            return $"{res}{fpsStr}";
        }

        private void _chkStartFullscreen_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.StartFullscreen = _chkStartFullscreen.Checked;
            Properties.Settings.Default.Save();
        }

        private void _cbSourceSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cbSourceSelection.SelectedItem != null && int.TryParse(_cbSourceSelection.SelectedItem.ToString(), out int selectedSource))
            {
                Properties.Settings.Default.SelectedSource = selectedSource; // Speichere die ausgewählte Quelle
                Properties.Settings.Default.Save(); // Speichere die Einstellungen
            }
        }
    }
}
