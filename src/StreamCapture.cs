using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class StreamCapture : MonoBehaviour {

    public enum StreamType {
        FILE,
    }

    [SerializeField] private int _width = 1920;
    [SerializeField] private int _height = 1080;
    [SerializeField] private int _fps = 30; 
    [SerializeField] private int _captureTextureDepth = 24; 
    [SerializeField] private RenderTextureFormat _captureInputFormat = RenderTextureFormat.ARGB32; 
    [SerializeField] private Camera _camera;
    [SerializeField] private string _outputFilePath;
    [SerializeField] private StreamType _streamType;

    private RenderTexture _renderTexture;
    private Queue<byte[]> _frameQueue = new Queue<byte[]>();
    private FFmpegEncoder _ffmpegEncoder;
    
    private bool _isStreaming = false;
    private bool _isProcessingFrame = false;

    private void Start() {
        _isStreaming = false;
        _ffmpegEncoder = new FFmpegEncoder();
        _renderTexture = new RenderTexture(_width, _height, _captureTextureDepth, _captureInputFormat);
        _renderTexture.Create();
    }

    public void StartStreaming() {
        Debug.Log("Starting stream...");
        _isStreaming = true;
        StartCoroutine(CaptureFrames());
        StartCoroutine(ProcessFrameQueue());
    }

    public void StopStreaming() {
        Debug.Log("Stopping stream...");
        StopAllCoroutines();
        _isStreaming = false;
        _ffmpegEncoder.StopEncoder();
    }

    private void OnDestroy() {
        if (_renderTexture != null) {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
    }

    private IEnumerator CaptureFrames() {
        _ffmpegEncoder.StartEncoder(_width, _height, _fps, _outputFilePath);

        while (_isStreaming) {
            yield return new WaitForEndOfFrame();

            // Render to our persistent RenderTexture
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = null;

            // Request async GPU readback
            AsyncGPUReadback.Request(_renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
            yield return new WaitForSeconds(1f / _fps);
        }

    }

    private void OnCompleteReadback(AsyncGPUReadbackRequest request) {
        if (request.hasError) {
            Debug.LogError("GPU readback error.");
            return;
        }

        if (request.done) {
            // Get the raw data
            var rawData = request.GetData<byte>();

            // Create a copy since the native array will be disposed
            byte[] frameData = new byte[rawData.Length];
            rawData.CopyTo(frameData);

            // Add to queue for processing 
            lock (_frameQueue) {
                _frameQueue.Enqueue(frameData);
            }
        }
    }

    private IEnumerator ProcessFrameQueue() {
        while (true) {
            if (_frameQueue.Count > 0 && !_isProcessingFrame) {
                byte[] frameData;
                lock (_frameQueue) {
                    frameData = _frameQueue.Dequeue();
                }

                _isProcessingFrame = true;

                // Process frame on a background thread to avoid blocking main thread
                System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                    try {
                        EncodeH264Frame(frameData);
                    } finally {
                        _isProcessingFrame = false;
                    }
                });
            }

            yield return null;
        }
    }

    private async Task EncodeH264Frame(byte[] rawBytes) {
        await _ffmpegEncoder.EncodeFrame(rawBytes);
    }

}