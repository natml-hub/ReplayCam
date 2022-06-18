/* 
*   ReplayCam
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using System.Linq;
    using UnityEngine;
    using UnityEngine.UI;
    using NatML.Devices;
    using NatML.Devices.Outputs;
    using NatML.Recorders;
    using NatML.Recorders.Clocks;
    using NatML.Recorders.Inputs;

    public class ReplayCam : MonoBehaviour {

        [Header(@"Camera Preview")]
        public RawImage previewImage;
        public AspectRatioFitter previewAspectFitter;

        [Header(@"Recording")]
        public int videoWidth = 1280;
        public int videoHeight = 720;
        public bool recordMicrophone;

        private CameraDevice cameraDevice;
        private AudioDevice audioDevice;
        private TextureOutput previewTextureOutput;
        
        private MP4Recorder recorder;
        private CameraInput cameraInput;

        private async void Start () {
            // Request camera permissions
            var cameraPermissionStatus = await MediaDeviceQuery.RequestPermissions<CameraDevice>();
            if (cameraPermissionStatus != PermissionStatus.Authorized) {
                Debug.LogError("User did not grant camera permissions");
                return;
            }
            // Request microphone permissions
            var microphonePermissionStatus  = await MediaDeviceQuery.RequestPermissions<AudioDevice>();
            if (microphonePermissionStatus != PermissionStatus.Authorized) {
                Debug.LogWarning("User did not grant microphone permissions");
                recordMicrophone = false;
            }
            // Get the default camera and microphone
            var query = new MediaDeviceQuery();
            cameraDevice = query.FirstOrDefault(device => device is CameraDevice) as CameraDevice;
            audioDevice = query.FirstOrDefault(device => device is AudioDevice) as AudioDevice;
            // Start the camera preview
            previewTextureOutput = new TextureOutput();
            cameraDevice.StartRunning(previewTextureOutput);
            // Display the preview texture
            var previewTexture = await previewTextureOutput;
            previewImage.texture = previewTexture;
            previewAspectFitter.aspectRatio = (float)previewTexture.width / previewTexture.height;
        }

        public void StartRecording () {
            // Start recording
            recorder = new MP4Recorder(
                videoWidth,
                videoHeight,
                30,
                recordMicrophone ? audioDevice.sampleRate : 0,
                recordMicrophone ? audioDevice.channelCount : 0
            );
            // Create recording inputs
            var clock = new RealtimeClock();
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            if (recordMicrophone)
                audioDevice.StartRunning(audioBuffer => recorder.CommitSamples(audioBuffer.sampleBuffer, clock.timestamp));
        }

        public async void StopRecording () {
            // Stop recording
            if (audioDevice?.running ?? false)
                audioDevice.StopRunning();
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            // Playback recording
            Debug.Log($"Saved recording to: {path}");
            #if UNITY_IOS || UNITY_ANDROID
            Handheld.PlayFullScreenMovie($"file://{path}");
            #endif
        }
    }
}