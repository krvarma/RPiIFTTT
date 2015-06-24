using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPiIFTTT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // I2C Controller name
        private const string I2C_CONTROLLER_NAME = "I2C1";
        // LED Pin
        private const int LED_PIN = 5;
        // IFTTT Channel Event Name
        private const string EVENT_LOW_NAME = "rpievent-low";
        private const string EVENT_HIGH_NAME = "rpievent-high";
        private const string EVENT_NORMAL_NAME = "rpievent-normal";
        // IFTTT Channel Secret Key
        private const string SECRET_KEY = "X6nH0XX6B2zfrMVyTMPLW";

        // I2C Device
        private I2cDevice I2CDev;
        // Timer
        private DispatcherTimer ReadSensorTimer;
        // TSL Sensor
        private TSL2561 TSL2561Sensor;
        // GPIO 
        private static GpioController gpio = GpioController.GetDefault();
        // GPIO Pin
        private GpioPin LEDPin = null;
        // TSL Gain and MS Values
        private Boolean Gain = false;
        private uint MS = 0;
        // Holds current luminosity
        private static double CurrentLux = 0;
        // Did we send IFTTT Event?
        private Boolean isEventSend = false;

        // Luminosity Threshold Limits
        private float LowerLimit = (float)10.0;
        private float UpperLimit = (float)120.0;

        public MainPage()
        {
            this.InitializeComponent();

            /* Register for the unloaded event so we can clean up upon exit */
            Unloaded += MainPage_Unloaded;

            // Initialize I2C Device
            InitializeI2CDevice();

            // Start Timer every 3 seconds
            ReadSensorTimer = new DispatcherTimer();
            ReadSensorTimer.Interval = TimeSpan.FromMilliseconds(3000);
            ReadSensorTimer.Tick += Timer_Tick;
            ReadSensorTimer.Start();

            LEDPin = gpio.OpenPin(LED_PIN);
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            I2CDev.Dispose();
        }

        private async void InitializeI2CDevice()
        {
            try
            {
                // Initialize I2C device
                var settings = new I2cConnectionSettings(TSL2561.TSL2561_ADDR);

                settings.BusSpeed = I2cBusSpeed.FastMode;
                settings.SharingMode = I2cSharingMode.Shared;

                string aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */

                I2CDev = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());

                return;
            }

            initializeSensor();
        }

        private void initializeSensor()
        {
            // Initialize Sensor
            TSL2561Sensor = new TSL2561(ref I2CDev);

            // Set the TSL Timing
            MS = (uint)TSL2561Sensor.SetTiming(false, 2);
            // Powerup the TSL sensor
            TSL2561Sensor.PowerUp();

            Debug.WriteLine("TSL2561 ID: " + TSL2561Sensor.GetId());
        }

        private void Timer_Tick(object sender, object e)
        {
            // Retrive luminosity and update the screen
            uint[] Data = TSL2561Sensor.GetData();

            Debug.WriteLine("Data1: " + Data[0] + ", Data2: " + Data[1]);

            CurrentLux = TSL2561Sensor.GetLux(Gain, MS, Data[0], Data[1]);

            String strLux = String.Format("{0:0.00}", CurrentLux);
            String strInfo = "Luminosity: " + strLux + " lux";

            Debug.WriteLine(strInfo);

            LightValue.Text = strInfo;

            // Check luminosoty is in range
            Boolean isNotInRange = (CurrentLux < 10.0 || CurrentLux > UpperLimit);

            LEDPin.SetDriveMode(GpioPinDriveMode.Output);
            LEDPin.Write((isNotInRange) ? GpioPinValue.High : GpioPinValue.Low);

            if (isNotInRange)
            {
                // Send notification message if not already send
                if (!isEventSend)
                {
                    Debug.WriteLine("Send below threshold event");

                    if(CurrentLux < LowerLimit)
                        SendIFTTTEvent(EVENT_LOW_NAME, strLux);
                    else
                        SendIFTTTEvent(EVENT_HIGH_NAME, strLux);

                    isEventSend = true;
                }
            }
            else
            {
                // Send notification message if not already send
                if (isEventSend)
                {
                    Debug.WriteLine("Luminosoty is in permissible range");

                    SendIFTTTEvent(EVENT_NORMAL_NAME, strLux);

                    isEventSend = false;
                }
            }
        }
        
        // Send IFTTT Event
        private async void SendIFTTTEvent(string eventName, string value1)
        {
            using (var client = new HttpClient())
            {
                // Send current luminosity value
                var values = new Dictionary<string, string>
                {
                   { "value1", value1 }
                };

                var url = "https://maker.ifttt.com/trigger/" + eventName + "/with/key/" + SECRET_KEY;

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();
            }
        }
    }
}