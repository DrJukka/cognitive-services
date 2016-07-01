# WordCloud

This project was made as minimum viable product to check whether we could use voice input for getting 
words and then whether we could illustrate them in word cloud type of UI component. 

## Using the app

First you need to have Visual Studio capable of handling Universal Windows applications, and open the [solution file](https://github.com/DrJukka/cognitive-services/blob/master/WordCloud/WordCloud.sln) with it.


## General overview

The projects consist of two parts [the WordCloud app](https://github.com/DrJukka/cognitive-services/tree/master/WordCloud/WordCloud)and the [WordCloudControl](https://github.com/DrJukka/cognitive-services/tree/master/WordCloud/WordCloudControl) UserControl.

The WordCloud app is using the voice recognition API for voice input, and feeding the WordCloudControl with the recognized text. Then the WordCloudControl is displaying the words in the UI.

The WordCloudControl was originally designed by George Mamaladze, and you can find his implementation from [http://www.codeproject.com/](http://www.codeproject.com/Articles/224231/Word-Cloud-Tag-Cloud-Generator-Control-for-NET-Win). His version is targeting .NET environment, where as I needed UWP implementation, thus I ported the code.

Main differencies are on drawing, I ended up using Win2D for the drawing, and then simply fixing anything that was handled differently between the platforms to get it fully working.


License
=======

All samples except the WordCloudControl, under this folder are licensed with the MIT License. For more details, see
[LICENSE](https://github.com/DrJukka/cognitive-services/blob/master/LICENSE).

The WordCloudControl is using [The Code Project Open License (CPOL)](http://www.codeproject.com/info/cpol10.aspx)