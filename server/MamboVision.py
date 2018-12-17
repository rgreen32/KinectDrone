"""
MamboVision is separated from the main Mambo class to enable the use of the drone without the FPV camera.
If you want to do vision processing, you will need to create a MamboVision object to capture the
video stream.

This module relies on the opencv module, which can a bit challenging to compile on the Raspberry Pi.
The instructions here are very helpful:

https://www.pyimagesearch.com/2016/04/18/install-guide-raspberry-pi-3-raspbian-jessie-opencv-3/

I did not use a virtual environment and just installed it in my regular python 2.7 environment.
That webpage said it takes only a few hours but it only compiled in single threaded mode for my RPI 3 and
that took overnight to finish.

Also had to compile ffmpeg and use a MAX_SLICES of 8192.  Directions for that are here.

https://github.com/tgogos/rpi_ffmpeg

Author: Amy McGovern, dramymcgovern@gmail.com
"""
import cv2
import threading
import time

class MamboVision:
    def __init__(self, fps=10, buffer_size=10):
        """
        Setup your vision object and initialize your buffers.  You won't start seeing pictures
        until you call open_video.

        :param fps: frames per second (don't set this very high on a Raspberry Pi!).  Defaults to 10 which is a number
        that should keep a Raspberry Pi busy but not overheated.

        :param buffer_size: number of frames to buffer in memory.  Defaults to 10.
        """

        self.fps = fps
        self.buffer_size = buffer_size

        # initialize a buffer (will contain the last buffer_size vision objects)
        self.buffer = [None] * buffer_size
        self.buffer_index = 0

        # setup the thread for monitoring the vision (but don't start it until we connect in open_video)
        self.vision_thread = threading.Thread(target=self._buffer_vision, args=(fps, buffer_size))

        self.vision_running = True


    def open_video(self, max_retries=3):
        """
        Open the video stream in opencv for capturing and processing.  The address for the stream
        is the same for all Mambos and is documented here:

        http://forum.developer.parrot.com/t/streaming-address-of-mambo-fpv-for-videoprojection/6442/6

        Remember that this will only work if you have connected to the wifi for your mambo!

        :param max_retries: Maximum number of retries in opening the camera (remember to connect to camera wifi!).
        Defaults to 3.

        :param fps: frames per second (don't set this very high on a Raspberry Pi!).  Defaults to 10 which is a number
        that should keep a Raspberry Pi busy but not overheated.

        :param buffer_size: number of frames to buffer in memory.  Defaults to 10.

        :return True if the vision opened correctly and False otherwise
        """
        print "opening the camera"
        self.capture = cv2.VideoCapture("rtsp://192.168.99.1/media/stream2")

        #print self.capture.get(cv2.CV_CAP_PROPS_FPS)

        # if it didn't open the first time, try again a maximum number of times
        try_num = 1
        while (not self.capture.isOpened() and try_num < max_retries):
            print "re-trying to open the capture"
            self.capture = cv2.VideoCapture("rtsp://192.168.99.1/media/stream2")
            try_num += 1

        # return whether the vision opened
        return self.capture.isOpened()

    def start_video_buffering(self):
        """
        If the video capture was successfully opened, then start the thread to buffer the stream

        :return:
        """
        if (self.capture.isOpened()):
            print "starting vision thread"
            self.vision_thread.start()

    def _buffer_vision(self, fps, buffer_size):
        """
        Internal method to save valid video captures from the camera fps times a second

        :param fps: frames per second (set in init)
        :param buffer_size: number of images to buffer (set in init)
        :return:
        """

        while (self.vision_running):
            # grab the latest image
            print "grabbing frame"
            capture_correct, video_frame = self.capture.read()
            print capture_correct
            if (capture_correct):
                self.buffer_index += 1
                self.buffer_index %= buffer_size
                print "saving frame to buffer"
                #print video_frame
                self.buffer[self.buffer_index] = video_frame
                
            # put the thread back to sleep for fps
            print "sleeping for %f" % (1.0 / fps)
            time.sleep(1.0 / fps)
        

    def get_latest_valid_picture(self):
        """
        Return the latest valid image (from the buffer)

        :return: last valid image received from the Mambo
        """
        return self.buffer[self.buffer_index]

    def stop_vision_buffering(self):
        """
        Should stop the vision thread
        """
        self.vision_running = False
    
