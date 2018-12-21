﻿//------------------------------------------------------------------------------
// <copyright file="GestureDetector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    using System.Net;
    using System.IO;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Kinect.VisualGestureBuilder;

    /// <summary>
    /// Gesture Detector class which listens for VisualGestureBuilderFrame events from the service
    /// and updates the associated GestureResultView object with the latest results for the 'Seated' gesture
    /// </summary>
    public class GestureDetector : IDisposable
    {
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase1 = @"Database\Final2_LaunchTrain.gbd";
        private readonly string gestureDatabase2 = @"Database\ActivateAndLand2.gbd";


        /// <summary> Name of the discrete gesture in the database that we want to track </summary>
        private readonly string LaunchGestureName = "Launch_Right";
        private readonly string LandGestureName = "Land_Left";
        private readonly string SwipeLeftGestureName = "SwipeLeft_Right";
        private readonly string SwipeRightGestureName = "SwipeRight_Left";
        private readonly string LiftGestureName = "Lift";
        private readonly string LowerGestureName = "Lower";
        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

        /// <summary>
        /// Initializes a new instance of the GestureDetector class along with the gesture frame source and reader
        /// </summary>
        /// <param name="kinectSensor">Active sensor to initialize the VisualGestureBuilderFrameSource object with</param>
        /// <param name="gestureResultView">GestureResultView object to store gesture results of a single body to</param>
        public string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        async Task PauseAsync(VisualGestureBuilderFrameReader vgb) {
            vgb.IsPaused = true;
            Console.WriteLine("Paused");
            await Task.Delay(5000);
            vgb.IsPaused = false;
            Console.WriteLine("Unpaused");
        }
        public GestureDetector(KinectSensor kinectSensor, GestureResultView gestureResultView)
        {
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            if (gestureResultView == null)
            {
                throw new ArgumentNullException("gestureResultView");
            }
            
            this.GestureResultView = gestureResultView;
            
            // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

            // open the reader for the vgb frames
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.IsPaused = true;
                this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
            }

            // load the 'Seated' gesture from the gesture database
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase1))
            {
                // we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
                // but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
                foreach (Gesture gesture in database.AvailableGestures)
                {
                    this.vgbFrameSource.AddGesture(gesture);
                    Console.WriteLine(gesture.Name);
                    //if (gesture.Name.Equals(this.LaunchGestureName) || gesture.Name.Equals(this.LandGestureName))
                    //{
                      //  this.vgbFrameSource.AddGesture(gesture);
                    //}
                }
            }
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase2))
            {
                // we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
                // but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
                foreach (Gesture gesture in database.AvailableGestures)
                {
                    if(gesture.Name == "TakeOff") {
                        this.vgbFrameSource.AddGesture(gesture);
                    }  
                    
                    Console.WriteLine(gesture.Name);
                    //if (gesture.Name.Equals(this.LaunchGestureName) || gesture.Name.Equals(this.LandGestureName))
                    //{
                    //  this.vgbFrameSource.AddGesture(gesture);
                    //}
                }
            }
        }

        /// <summary> Gets the GestureResultView object which stores the detector results for display in the UI </summary>
        public GestureResultView GestureResultView { get; private set; }

        /// <summary>
        /// Gets or sets the body tracking ID associated with the current detector
        /// The tracking ID can change whenever a body comes in/out of scope
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
            }
        }

        /// <summary>
        /// Handles gesture detection results arriving from the sensor for the associated body tracking Id
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                    if (discreteResults != null)
                    {
                        // we only have one gesture in this source object, but you can get multiple gestures
                        foreach (Gesture gesture in this.vgbFrameSource.Gestures)
                        {
                            //Console.WriteLine(gesture.Name);
                            if (gesture.Name.Equals("TakeOff") && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {
                                    // update the GestureResultView object with new gesture result values

                                    //this.GestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);
                                    if (result.Detected)
                                    {
                                        Console.WriteLine("Launched");
                                        Console.WriteLine(Get("http://10.2.10.14:5000/"));
                                        Console.WriteLine("Request sent");
                                        PauseAsync(this.vgbFrameReader);                                                                           
                                    }

                                  
                                }
                             }
                            if (gesture.Name.Equals(this.LandGestureName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {
                                    // update the GestureResultView object with new gesture result values

                                    //this.GestureResultView.UpdateGestureResult(true, result.Detected, result.Confidence);
                                    if (result.Detected)
                                    {

                                        Console.WriteLine(Get("http://10.2.10.14:5000/land"));
                                        Console.WriteLine("Land request sent");
                                        PauseAsync(this.vgbFrameReader);

                                    }
             
                                }
                            }
                            if (gesture.Name.Equals(this.SwipeLeftGestureName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {
                  
                                    if (result.Detected) 
                                    {

                                        Console.WriteLine(Get("http://10.2.10.14:5000/left"));
                                        Console.WriteLine("Left request sent");
                                        PauseAsync(this.vgbFrameReader);

                                    }

                                }
                            }
                            if (gesture.Name.Equals(this.SwipeRightGestureName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {

                                    if (result.Detected)
                                    {

                                        Console.WriteLine(Get("http://10.2.10.14:5000/right"));
                                        Console.WriteLine("Right request sent");
                                        PauseAsync(this.vgbFrameReader);

                                    }

                                }
                            }
                            if (gesture.Name.Equals(this.LiftGestureName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {

                                    if (result.Detected)
                                    {

                                        Console.WriteLine(Get("http://10.2.10.14:5000/lift"));
                                        Console.WriteLine("Lift request sent");
                                        PauseAsync(this.vgbFrameReader);

                                    }

                                }
                            }
                            if (gesture.Name.Equals(this.LowerGestureName) && gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {

                                    if (result.Detected)
                                    {

                                        Console.WriteLine(Get("http://10.2.10.14:5000/lower"));
                                        Console.WriteLine("Lower request sent");
                                        PauseAsync(this.vgbFrameReader);

                                    }

                                }
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the TrackingIdLost event for the VisualGestureBuilderSource object
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // update the GestureResultView object to show the 'Not Tracked' image in the UI
            this.GestureResultView.UpdateGestureResult(false, false, 0.0f);
        }
    }
}
