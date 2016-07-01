# FaceDetection

This project was made for demo purposes only, and mainly coded during one week in June 2016. The purse of this application is to detect faces and face details from people entering a room.


## Using the app

First you need to have Visual Studio capable of handling Universal Windows applications, and open the [solution file](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection.sln) with it.

Then get your keys from [Cognitive Service](https://www.microsoft.com/cognitive-services/en-us/sign-up) and add them to [MainPage.xaml.cs](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/MainPage.xaml.cs), which after you can run the app in your machine.

## General overview

The [MainPage.xaml.cs](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/MainPage.xaml.cs) simply holds the usercontrol and does initialization and cleaning for it. All UserControls are located under the [Controls](https://github.com/DrJukka/cognitive-services/tree/master/FaceDetection/FaceDetection/Controls) folder.

The main UserControl is called  [FaceDetector](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/Controls/FaceDetector.xaml.cs) and its the only enterance point into the app logic. Its implements off-line face detection, which is used to figure out that we currently see a face.

Once we see a face we pass the image with face to [FaceMetaData](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/Controls/FaceMetaData.cs), which then uses on-line detection APIs to get more details for the detected faces.

FaceMetaData then passes array of [FaceWithEmotions](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/Controls/FaceWithEmotions.cs) objects back to the FaceDetector, which then prints the data into the UI, and forwards the array to [DataSender](https://github.com/DrJukka/cognitive-services/blob/master/FaceDetection/FaceDetection/Controls/DataSender.cs) for the delivery to the final destination. In this case its our HTTP proxy server running in local host.

## Notable issues

I was using public free key for the Face API, thus there is a limit of 20 queries / minute. Thus, there is timeout timer which causes the FaceMetaData to have 3 second delay after each successfull processing, thus if you need faster detection than what is currenrly provided, all you need to do, is to get better key and remove the timer usage.

License
=======

All samples under this folder are licensed with the MIT License. For more details, see
[LICENSE](<../LICENSE.md>).
