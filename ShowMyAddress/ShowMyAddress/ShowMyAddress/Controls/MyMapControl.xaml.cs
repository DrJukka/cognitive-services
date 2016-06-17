
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Services.Maps;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ShowMyAddress.Controls
{
    public sealed partial class MyMapControl : UserControl
    {  
        //maps data
        private const String _mapServiceToken = "ADD_YOUR_TOKEN_IN_HERE";


        public Geopoint Center
        {
            get
            {
                return myMap.Center;
            }
            set
            {
                myMap.Center = value;
            }
        }

        public Double ZoomLevel
        {
            get
            {
                return myMap.ZoomLevel;
            }
            set
            {
                myMap.ZoomLevel = value;
            }
        }

        public System.Object ItemsSource
        {
            get
            {
                return MapItems.ItemsSource;
            }
            set
            {
                MapItems.ItemsSource = value;
            }
        }

        public MyMapControl()
        {
            this.InitializeComponent();
        }
        public void InitControl()
        {
            myMap.MapServiceToken = _mapServiceToken;
            myMap.Loaded += MyMap_Loaded;
        }

        public void DeInitControl()
        {
            myMap.Loaded -= MyMap_Loaded;
        }

        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            //
        }

        public async Task<Geopoint> FindLocation(string address)
        {

            if (address.Length > 0 && (Center != null))
            {
                MapLocationFinderResult result = await MapLocationFinder.FindLocationsAsync(address, Center);

                System.Diagnostics.Debug.WriteLine("found " + result.Locations.Count + " results.");
                if (result.Locations.Count > 0 && result.Status == MapLocationFinderStatus.Success)
                {
                    return result.Locations[0].Point;
                }
            }

            return null;
        }
    }
}
