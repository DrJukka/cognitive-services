using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// This control is purely for debugging purpose, it will be used to show latest results in the UI
namespace FaceDetection.Controls
{
    public sealed partial class FaceDetailControl : UserControl
    {
        public FaceDetailControl()
        {
            this.InitializeComponent();
        }
    }
}
