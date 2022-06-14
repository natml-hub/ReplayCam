/* 
*   NatCorder
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatML.Examples {

    using System.Collections;
    using UnityEngine;
    using Recorders;
    using Recorders.Clocks;
    using Recorders.Inputs;

    [RequireComponent(typeof(AudioSource))]
    public class ReplayCam : MonoBehaviour {

        [Header(@"Recording")]
        public int videoWidth = 1280;
        public int videoHeight = 720;
        public bool recordMicrophone;

        private MP4Recorder recorder;
        private CameraInput cameraInput;
        private AudioInput audioInput;
        private AudioSource microphoneSource;

        private IEnumerator Start () {
            microphoneSource = gameObject.GetComponent<AudioSource>();
            #if !UNITY_WEBGL
            microphoneSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
            yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
            microphoneSource.Play();
            #else
            yield break;
            #endif
        }

        public void StartRecording () {
            // Start recording
            var frameRate = 30;
            var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            var clock = new RealtimeClock();
            recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount, audioBitRate: 96_000);
            // Create recording inputs
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;
        }

        public async void StopRecording () {
            // Mute microphone
            microphoneSource.mute = true;
            // Stop recording
            audioInput?.Dispose();
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